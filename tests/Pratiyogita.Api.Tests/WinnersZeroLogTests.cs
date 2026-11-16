using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using KeshavSingh.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Pratiyogita.Api.Controllers;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;
using Xunit;

namespace Pratiyogita.Api.Tests;

public sealed class WinnersZeroLogTests
{
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(request.RequestUri?.ToString() ?? string.Empty);
            return Task.FromResult(responder(request));
        }
    }

    [Fact]
    public async Task GoogleYouTubeVerifyService_EnforcesReadOnlyScope_ZeroLogs_AndRevokesToken()
    {
        const string secretCode = "SENSITIVE_AUTH_CODE_777";
        const string secretToken = "ya29.SENSITIVE_ACCESS_TOKEN_XYZ";
        const string clientSecret = "GOCSPX-SUPER_SECRET_CLIENT_999";

        var logger = new RecordingLogger<GoogleYouTubeVerifyService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CentralMongo:Enabled"] = "false",
                ["YouTubeVerify:ClientId"] = "client-id-123.apps.googleusercontent.com",
                ["YouTubeVerify:ClientSecret"] = clientSecret,
                ["YouTubeVerify:ProofSigningKey"] = "unit-test-hmac-signing-key-32bytes!",
            })
            .Build();

        var handler = new StubHttpHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.StartsWith("https://oauth2.googleapis.com/token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { access_token = secretToken, expires_in = 3600, scope = GoogleYouTubeVerifyService.ReadOnlyScope }),
                        Encoding.UTF8,
                        "application/json"),
                };
            }
            if (url.StartsWith("https://www.googleapis.com/youtube/v3/channels", StringComparison.Ordinal))
            {
                Assert.Equal(secretToken, req.Headers.Authorization?.Parameter);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                          "items": [
                            {
                              "id": "UC_VIEWER_CHANNEL_42",
                              "snippet": {
                                "title": "Aarav Tech Studio",
                                "customUrl": "@aaravtech",
                                "thumbnails": { "high": { "url": "https://yt3.ggpht.com/aarav.jpg" } }
                              }
                            }
                          ]
                        }
                        """, Encoding.UTF8, "application/json"),
                };
            }
            if (url.StartsWith("https://oauth2.googleapis.com/revoke", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var centralConfig = new CentralAdminConfigReader(config, NullLogger<CentralAdminConfigReader>.Instance);
        using var service = new GoogleYouTubeVerifyService(config, centralConfig, logger, TimeProvider.System, handler);

        const string redirectUri = "https://pratiyogita.keshavsingh.in/winners";
        var auth = await service.CreateAuthorizationUrlAsync(redirectUri);

        Assert.Contains("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fyoutube.readonly", auth.AuthorizationUrl);
        Assert.Contains("access_type=online", auth.AuthorizationUrl);
        Assert.DoesNotContain("force-ssl", auth.AuthorizationUrl);
        Assert.DoesNotContain("youtube.upload", auth.AuthorizationUrl);

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(auth.AuthorizationUrl).Query);
        var state = query["state"].ToString();

        var completed = await service.VerifyChannelZeroLogAsync(secretCode, state, redirectUri);

        Assert.Equal("UC_VIEWER_CHANNEL_42", completed.Channel.ChannelId);
        Assert.Equal("Aarav Tech Studio", completed.Channel.ChannelTitle);
        Assert.Equal("@aaravtech", completed.Channel.ChannelHandle);

        // Token was revoked best-effort right after reading public channel info
        Assert.Contains(handler.RequestedUrls, u => u.StartsWith("https://oauth2.googleapis.com/revoke", StringComparison.Ordinal));

        // Zero-log verification: NO OAuth code, token, client secret, or channel title in logs OR proof token
        var allLogs = string.Join("\n", logger.Entries);
        Assert.DoesNotContain(secretCode, allLogs);
        Assert.DoesNotContain(secretToken, allLogs);
        Assert.DoesNotContain(clientSecret, allLogs);
        Assert.DoesNotContain(secretToken, completed.VerificationToken);
        Assert.DoesNotContain(secretCode, completed.VerificationToken);

        // Proof validates cleanly, and tampering breaks HMAC
        var validated = service.ValidateVerificationToken(completed.VerificationToken);
        Assert.NotNull(validated);
        Assert.Equal("UC_VIEWER_CHANNEL_42", validated!.ChannelId);

        var tampered = completed.VerificationToken[..^1] + (completed.VerificationToken.EndsWith('A') ? 'B' : 'A');
        Assert.Null(service.ValidateVerificationToken(tampered));

        // Open-redirect protection
        Assert.False(service.IsAllowedRedirectUri("https://evil.example.com/winners"));
        Assert.False(service.IsAllowedRedirectUri("https://pratiyogita.keshavsingh.in/competitions"));
        Assert.True(service.IsAllowedRedirectUri("http://localhost:4200/winners"));
    }

    [Fact]
    public async Task QuizWinnersService_MasksOpenAnswer_BlocksDuplicateSubmit_AndFiltersTimeline()
    {
        var service = new QuizWinnersService(NullLogger<QuizWinnersService>.Instance);

        service.SeedInMemorySession(new BsonDocument
        {
            ["_id"] = "lqz_live_001",
            ["StreamTitle"] = "Grand First Live Quiz",
            ["StreamChannelHandle"] = "@keshavsinghworld",
            ["Question"] = "Which channel is for 3D comedy?",
            ["QuestionHi"] = "3D कॉमेडी किस चैनल पर है?",
            ["Options"] = new BsonArray
            {
                new BsonDocument { ["Key"] = "A", ["Text"] = "@omkarshivbhakti" },
                new BsonDocument { ["Key"] = "B", ["Text"] = "@KESHAV-ANIM-FF" },
            },
            ["CorrectOptionKey"] = "B",
            ["PrizeTitle"] = "Winner Crown",
            ["Language"] = "hi",
            ["Status"] = "Open",
            ["CreatedAtUtc"] = DateTime.UtcNow,
        });

        var active = await service.GetActiveQuizAsync();
        Assert.NotNull(active);
        Assert.Null(active!.RevealedCorrectOptionKey);

        var channel = new VerifiedYouTubeChannel("UC_TEST_88", "Diya Reader", "@diyareads", null, DateTime.UtcNow);
        var submission = await service.SubmitVerifiedAnswerAsync("lqz_live_001", channel, "B");
        Assert.Equal("B", submission.AnswerOption);
        Assert.Equal("UC_TEST_88", submission.ChannelId);

        // Second submission by same channel is rejected
        var dup = await Assert.ThrowsAsync<ZeroLogVerifyException>(() =>
            service.SubmitVerifiedAnswerAsync("lqz_live_001", channel, "A"));
        Assert.Equal("already-answered", dup.Code);

        // Manual winner announcement & timeline filtering
        await service.RecordWinnerManualAsync(new AnnounceQuizWinnerRequest(
            StreamTitle: "Comedy Launch Stream",
            StreamChannelHandle: "@KESHAV-ANIM-FF",
            BroadcastId: "VID12345678",
            Question: "Who laughed first?",
            QuestionHi: "पहले कौन हँसा?",
            CorrectOptionKey: "B",
            CorrectOptionText: "Chintu",
            WinnerChannelId: "UC_TEST_88",
            WinnerChannelTitle: "Diya Reader",
            WinnerChannelHandle: "@diyareads",
            WinnerThumbnailUrl: null,
            PrizeTitle: "Comedy Trophy"));

        var comedyWinners = await service.ListWinnersAsync(channelFilter: "KESHAV-ANIM-FF");
        var w = Assert.Single(comedyWinners);
        Assert.Equal("@diyareads", w.WinnerChannelHandle);
    }

    [Fact]
    public void WinnersController_EnforcesPublicReadAndAdminAnnounce()
    {
        var announce = typeof(WinnersController).GetMethod(nameof(WinnersController.AnnounceWinner));
        var auth = announce!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Equal(Roles.Admin, auth!.Roles);

        foreach (var name in new[]
        {
            nameof(WinnersController.ListWinners),
            nameof(WinnersController.GetActiveQuiz),
            nameof(WinnersController.GetYouTubeAuthUrl),
            nameof(WinnersController.CompleteYouTubeVerify),
            nameof(WinnersController.SubmitAnswer),
        })
        {
            var m = typeof(WinnersController).GetMethod(name);
            Assert.NotNull(m!.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }
}
