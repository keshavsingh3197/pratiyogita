using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class ZeroLogVerifyException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// Zero-Log Google OAuth (<c>youtube.readonly</c>) viewer verification:
/// <list type="bullet">
///   <item>Exchanges authorization code strictly in RAM (<c>access_type=online</c>, no refresh token requested).</item>
///   <item>Calls <c>channels?part=snippet&amp;mine=true</c> to obtain ONLY public channel metadata (ID, title, @handle, avatar).</item>
///   <item>Revokes &amp; discards the ephemeral <c>access_token</c> immediately in <c>finally</c> — NEVER stored in DB, cookies, or logs.</item>
///   <item>NEVER logs OAuth codes, verifiers, tokens, emails, or personal identifiers.</item>
/// </list>
/// </summary>
public sealed class GoogleYouTubeVerifyService : IDisposable
{
    public const string ReadOnlyScope = "https://www.googleapis.com/auth/youtube.readonly";
    public const string ZeroLogStatement =
        "Zero-Log Policy: Google access tokens and account emails are never stored in databases, cookies, or logs. Only your public YouTube channel title/handle is verified in memory.";

    private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private const string ChannelsEndpoint = "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";
    private static readonly TimeSpan PendingStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ProofLifetime = TimeSpan.FromMinutes(25);
    private const int MaxPendingStates = 2000;

    private sealed record PendingVerification(string CodeVerifier, string RedirectUri, DateTime ExpiresAtUtc);

    private readonly IConfiguration _configuration;
    private readonly CentralAdminConfigReader _centralConfig;
    private readonly TimeProvider _clock;
    private readonly ILogger<GoogleYouTubeVerifyService> _logger;
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, PendingVerification> _pending = new(StringComparer.Ordinal);
    private readonly byte[] _hmacKey;
    private readonly string[] _extraAllowedOrigins;

    public GoogleYouTubeVerifyService(
        IConfiguration configuration,
        CentralAdminConfigReader centralConfig,
        ILogger<GoogleYouTubeVerifyService> logger,
        TimeProvider? clock = null,
        HttpMessageHandler? handler = null)
    {
        _configuration = configuration;
        _centralConfig = centralConfig;
        _clock = clock ?? TimeProvider.System;
        _logger = logger;

        var rawSigningSecret = FirstNonEmpty(
            configuration["YouTubeVerify:ProofSigningKey"],
            configuration["Jwt:SigningKey"],
            Environment.GetEnvironmentVariable("Jwt__SigningKey"));

        _hmacKey = string.IsNullOrWhiteSpace(rawSigningSecret)
            ? RandomNumberGenerator.GetBytes(32)
            : SHA256.HashData(Encoding.UTF8.GetBytes("pratiyogita-yt-verify:" + rawSigningSecret));

        _extraAllowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        _http = new HttpClient(handler ?? new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = DecompressionMethods.All,
            SslOptions = { EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13 },
            AllowAutoRedirect = false,
        })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
    {
        var (clientId, clientSecret) = await ResolveOAuthCredentialsAsync(ct).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    public bool IsAllowedRedirectUri(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)) return false;
        if (!Uri.TryCreate(redirectUri.Trim(), UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.AbsolutePath, "/winners", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.AbsolutePath, "/winners/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1"))
            return true;

        if (uri.Scheme != Uri.UriSchemeHttps) return false;

        if (uri.Host.Equals("pratiyogita.keshavsingh.in", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("keshavsingh.in", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".keshavsingh.in", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("keshavsingh3197.github.io", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var origin = uri.GetLeftPart(UriPartial.Authority);
        return _extraAllowedOrigins.Any(o => string.Equals(o?.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<YouTubeVerifyAuthUrlResponse> CreateAuthorizationUrlAsync(string redirectUri, CancellationToken ct = default)
    {
        if (!IsAllowedRedirectUri(redirectUri))
            throw new ZeroLogVerifyException("redirect-uri-invalid", "Redirect URI is not on an allowed Pratiyogita origin.");

        var (clientId, clientSecret) = await ResolveOAuthCredentialsAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ZeroLogVerifyException(
                "youtube-verify-not-configured",
                "Google OAuth credentials are not configured yet on this server.",
                503);
        }

        PruneExpiredStates();
        if (_pending.Count >= MaxPendingStates)
            throw new ZeroLogVerifyException("verify-busy", "Too many channel verifications are in progress. Try again shortly.", 429);

        var normalizedRedirect = redirectUri.Trim();
        var state = "pyw_" + Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        _pending[state] = new PendingVerification(verifier, normalizedRedirect, _clock.GetUtcNow().UtcDateTime + PendingStateLifetime);

        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = normalizedRedirect,
            ["response_type"] = "code",
            ["scope"] = ReadOnlyScope,
            ["access_type"] = "online",
            ["prompt"] = "select_account consent",
            ["include_granted_scopes"] = "false",
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        };

        var url = AuthorizeEndpoint + "?" + string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return new YouTubeVerifyAuthUrlResponse(url, ReadOnlyScope, ZeroLogStatement);
    }

    public async Task<YouTubeVerifyCompleteResponse> VerifyChannelZeroLogAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state) || !_pending.TryRemove(state.Trim(), out var pending))
            throw new ZeroLogVerifyException("verify-state-invalid", "Verification session expired or already completed. Start again.");

        var now = _clock.GetUtcNow().UtcDateTime;
        if (pending.ExpiresAtUtc < now)
            throw new ZeroLogVerifyException("verify-state-expired", "Verification link expired. Please verify again.");

        if (!IsAllowedRedirectUri(redirectUri)
            || !string.Equals(pending.RedirectUri, redirectUri.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ZeroLogVerifyException("verify-redirect-mismatch", "Verification redirect URI did not match the initiated request.");
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length > 2048)
            throw new ZeroLogVerifyException("verify-code-missing", "Missing authorization code from Google.");

        var (clientId, clientSecret) = await ResolveOAuthCredentialsAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new ZeroLogVerifyException("youtube-verify-not-configured", "Google OAuth credentials are unavailable.", 503);

        string? ephemeralAccessToken = null;
        try
        {
            ephemeralAccessToken = await ExchangeCodeInMemoryAsync(
                code.Trim(),
                pending.CodeVerifier,
                pending.RedirectUri,
                clientId,
                clientSecret,
                ct).ConfigureAwait(false);

            var channel = await ReadPublicChannelAsync(ephemeralAccessToken, ct).ConfigureAwait(false);
            var expiresAt = now + ProofLifetime;
            var proof = MintVerificationToken(channel, expiresAt);

            // Strictly no tokens, codes, emails, or personal channel titles written to logs.
            _logger.LogInformation("Zero-log YouTube channel verification succeeded (code=200).");
            return new YouTubeVerifyCompleteResponse(channel, proof, expiresAt, ZeroLogStatement);
        }
        finally
        {
            // Best-effort immediate revocation & reference clear so access_token cannot persist in RAM or upstream.
            if (!string.IsNullOrEmpty(ephemeralAccessToken))
            {
                var tokenToRevoke = ephemeralAccessToken;
                ephemeralAccessToken = null;
                await RevokeBestEffortAsync(tokenToRevoke, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public VerifiedYouTubeChannel? ValidateVerificationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var parts = token.Trim().Split('.');
        if (parts.Length != 3 || !string.Equals(parts[0], "pyv1", StringComparison.Ordinal))
            return null;

        byte[] payloadBytes;
        byte[] actualSig;
        try
        {
            payloadBytes = Base64UrlDecode(parts[1]);
            actualSig = Base64UrlDecode(parts[2]);
        }
        catch (FormatException)
        {
            return null;
        }

        var expectedSig = HMACSHA256.HashData(_hmacKey, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));
        if (actualSig.Length != expectedSig.Length || !CryptographicOperations.FixedTimeEquals(actualSig, expectedSig))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<ProofPayload>(payloadBytes);
            if (payload is null || string.IsNullOrWhiteSpace(payload.ChannelId))
                return null;
            if (payload.ExpiresAtUnix < _clock.GetUtcNow().ToUnixTimeSeconds())
                return null;

            return new VerifiedYouTubeChannel(
                payload.ChannelId,
                payload.ChannelTitle ?? payload.ChannelId,
                payload.ChannelHandle,
                payload.ThumbnailUrl,
                DateTimeOffset.FromUnixTimeSeconds(payload.VerifiedAtUnix).UtcDateTime);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string MintVerificationToken(VerifiedYouTubeChannel channel, DateTime expiresAtUtc)
    {
        var payload = new ProofPayload(
            channel.ChannelId,
            channel.ChannelTitle,
            channel.ChannelHandle,
            channel.ThumbnailUrl,
            new DateTimeOffset(channel.VerifiedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(expiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds());

        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = "pyv1." + encodedPayload;
        var sig = Base64UrlEncode(HMACSHA256.HashData(_hmacKey, Encoding.ASCII.GetBytes(signingInput)));
        return signingInput + "." + sig;
    }

    private async Task<string> ExchangeCodeInMemoryAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        string clientId,
        string clientSecret,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["code_verifier"] = codeVerifier,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            }),
        };

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Zero-log OAuth token exchange unreachable ({ErrorType}).", ex.GetType().Name);
            throw new ZeroLogVerifyException("google-unreachable", "Google authorization service could not be reached. Please try again.", 502);
        }

        using (res)
        {
            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                var safeError = doc.RootElement.TryGetProperty("error", out var errEl) && errEl.ValueKind == JsonValueKind.String
                    ? SanitizeReason(errEl.GetString())
                    : "unknown";
                _logger.LogWarning("Zero-log OAuth token exchange answered {Status} ({Reason}).", (int)res.StatusCode, safeError);
                throw new ZeroLogVerifyException("verify-token-failed", "Google did not complete YouTube channel verification. Please try again.");
            }

            var accessToken = doc.RootElement.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ZeroLogVerifyException("verify-token-failed", "Google did not return an authorization grant.");

            return accessToken;
        }
    }

    private async Task<VerifiedYouTubeChannel> ReadPublicChannelAsync(string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, ChannelsEndpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("YouTube channels.list unreachable during verification ({ErrorType}).", ex.GetType().Name);
            throw new ZeroLogVerifyException("youtube-unreachable", "YouTube could not be reached to read channel info. Try again.", 502);
        }

        using (res)
        {
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTube channels.list answered {Status} during verification.", (int)res.StatusCode);
                throw new ZeroLogVerifyException("youtube-channel-read-failed", "Could not verify your YouTube channel.");
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            if (!doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array
                || items.GetArrayLength() == 0)
            {
                throw new ZeroLogVerifyException(
                    "youtube-no-channel",
                    "This Google account does not have an active YouTube channel yet.");
            }

            var item = items[0];
            var channelId = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(channelId))
                throw new ZeroLogVerifyException("youtube-no-channel", "YouTube channel ID was missing.");

            item.TryGetProperty("snippet", out var snippet);
            var title = snippet.ValueKind == JsonValueKind.Object && snippet.TryGetProperty("title", out var tEl)
                ? (tEl.GetString() ?? channelId)
                : channelId;
            var handle = snippet.ValueKind == JsonValueKind.Object && snippet.TryGetProperty("customUrl", out var cEl)
                ? NormalizeHandle(cEl.GetString())
                : null;

            string? thumb = null;
            if (snippet.ValueKind == JsonValueKind.Object
                && snippet.TryGetProperty("thumbnails", out var thumbs)
                && thumbs.ValueKind == JsonValueKind.Object)
            {
                foreach (var size in new[] { "high", "medium", "default" })
                {
                    if (thumbs.TryGetProperty(size, out var tObj)
                        && tObj.TryGetProperty("url", out var urlEl)
                        && Uri.TryCreate(urlEl.GetString(), UriKind.Absolute, out var parsed)
                        && parsed.Scheme == Uri.UriSchemeHttps)
                    {
                        thumb = parsed.ToString();
                        break;
                    }
                }
            }

            return new VerifiedYouTubeChannel(
                channelId.Trim(),
                Truncate(title.Trim(), 160),
                handle is null ? null : Truncate(handle, 80),
                thumb,
                _clock.GetUtcNow().UtcDateTime);
        }
    }

    private async Task RevokeBestEffortAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = accessToken });
            using var _ = await _http.PostAsync(RevokeEndpoint, content, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Strictly best-effort: token still expires natively in 1h and is already discarded from memory.
        }
    }

    private async Task<(string? ClientId, string? ClientSecret)> ResolveOAuthCredentialsAsync(CancellationToken ct)
    {
        var localId = FirstNonEmpty(
            _configuration["YouTubeVerify:ClientId"],
            _configuration["GoogleOAuth:ClientId"],
            Environment.GetEnvironmentVariable("YouTubeVerify__ClientId"),
            Environment.GetEnvironmentVariable("GoogleOAuth__ClientId"));
        var localSecret = FirstNonEmpty(
            _configuration["YouTubeVerify:ClientSecret"],
            _configuration["GoogleOAuth:ClientSecret"],
            Environment.GetEnvironmentVariable("YouTubeVerify__ClientSecret"),
            Environment.GetEnvironmentVariable("GoogleOAuth__ClientSecret"));

        if (!string.IsNullOrWhiteSpace(localId) && !string.IsNullOrWhiteSpace(localSecret))
            return (localId, localSecret);

        var central = await _centralConfig.GetSnapshotAsync(ct).ConfigureAwait(false);
        return (localId ?? central.GoogleOAuthClientId, localSecret ?? central.GoogleOAuthClientSecret);
    }

    private void PruneExpiredStates()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var (k, v) in _pending)
        {
            if (v.ExpiresAtUtc < now) _pending.TryRemove(k, out _);
        }
    }

    private static string? NormalizeHandle(string? handle)
    {
        var h = handle?.Trim();
        if (string.IsNullOrWhiteSpace(h)) return null;
        return h.StartsWith('@') ? h : "@" + h;
    }

    private static string SanitizeReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Length <= 48 && reason.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
            ? reason
            : "unknown";

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    public void Dispose() => _http.Dispose();

    private sealed record ProofPayload(
        string ChannelId,
        string? ChannelTitle,
        string? ChannelHandle,
        string? ThumbnailUrl,
        long VerifiedAtUnix,
        long ExpiresAtUnix);
}
