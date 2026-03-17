using Tunnel.Daemon;
using Tunnel.Daemon.Api;
using Tunnel.Daemon.Services;
using Tunnel.Shared;

// ──────────────────────────────────────────────────────────
// Tunnel Daemon — Minimal API + Systemd User Service
// Port: 6385 | Auth: Bearer token from ~/.tunnel/.auth
// ──────────────────────────────────────────────────────────

var builder = WebApplication.CreateSlimBuilder(args);

// AOT-safe JSON serialization via Source Generators
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

builder.Services.AddSingleton<ProfileService>();
builder.Services.AddSingleton<TunnelService>();

// Kestrel: listen only on localhost (not externally accessible)
builder.WebHost.UseKestrel(kestrel =>
    kestrel.ListenLocalhost(6385));

builder.Logging.ClearProviders();
builder.Logging.AddConsole(opts =>
    opts.FormatterName = "simple");

var app = builder.Build();

// ── Auth Token ────────────────────────────────────────────
// Generated once at startup; stored in ~/.tunnel/.auth (chmod 600).
// CLI reads the same file — no secret ever touches source code.
var validToken = AuthTokenStore.LoadOrGenerate();

app.Use(async (ctx, next) =>
{
    // Health + version endpoints skip auth
    if (ctx.Request.Path == "/health" || ctx.Request.Path == "/api/version")
    {
        await next();
        return;
    }

    if (!ctx.Request.Headers.TryGetValue("Authorization", out var authVal)
        || authVal.ToString() != $"Bearer {validToken}")
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsync("Unauthorized");
        return;
    }

    await next();
});

app.MapTunnelEndpoints();

app.Run();
