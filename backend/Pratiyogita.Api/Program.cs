using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KeshavSingh.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Pratiyogita.Api.Options;
using Pratiyogita.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms (Render, Railway, Heroku…) inject the listen port via $PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ---- Data (one Mongo collection per bounded context — see README.md for the module map) ----
builder.Services.AddKeshavMongo(builder.Configuration);
builder.Services.AddSingleton<LocationService>();
builder.Services.AddSingleton<SchoolService>();
builder.Services.AddSingleton<StudentProfileService>();
builder.Services.AddSingleton<CompetitionService>();
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<RegistrationService>();
builder.Services.AddSingleton<FixtureService>();
builder.Services.AddSingleton<ResultService>();
builder.Services.AddSingleton<LeaderboardService>();
builder.Services.AddSingleton<ContributionService>();
builder.Services.AddSingleton<NewsService>();
builder.Services.AddSingleton<PlatformSettingsService>();

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.Section));

// ---- Auth ----
// This app is a pure RESOURCE SERVER: it has no login of its own and never mints a token. Users
// sign in once at the identity provider (admin.keshavsingh.in / id.keshavsingh.in) and this app only
// validates the JWT that flow produces — same signing key/issuer/audience as every other
// *.keshavsingh.in app, so a token minted there just works here. Per-app data (school, class,
// DOB, contact details) is kept locally in StudentProfile, keyed by the JWT's "sub" claim.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---- CORS: the SSO family — any keshavsingh.in subdomain over https, plus localhost in dev.
// Credentialed, so this is a scoped predicate allowlist (never AllowAnyOrigin). ----
const string CorsPolicy = "PratiyogitaCors";
builder.Services.AddKeshavSsoCors(CorsPolicy);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Keep "sub"/role claims verbatim.
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
                    ? new string('0', 32) // Placeholder; only ever hit if the shared secret is unset.
                    : jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// ---- Rate limiting: contributions and school self-registration are the two anonymous/low-friction
// write paths, so they get their own tight fixed windows to blunt spam/abuse. ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("contribute", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddHealthChecks();

// Behind Render's TLS-terminating proxy: honour X-Forwarded-* so the app sees the real client IP
// and the original https scheme (so no in-container redirect loop).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// ---- Pipeline ----
app.UseForwardedHeaders();

// Baseline security headers.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Cross-Origin-Resource-Policy"] = "same-site";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    // TLS is terminated at Render's edge (which also redirects http->https), so an in-container
    // HTTPS redirect is redundant and can loop behind the proxy. We still emit HSTS.
    app.UseHsts();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.Services.GetRequiredService<LocationService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<SchoolService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<StudentProfileService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<CompetitionService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<CategoryService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<RegistrationService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<FixtureService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<ResultService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<ContributionService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<NewsService>().EnsureIndexesAsync();
await app.Services.GetRequiredService<PlatformSettingsService>().InitAsync();

app.Run();
