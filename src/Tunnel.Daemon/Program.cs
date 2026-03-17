using Tunnel.Daemon;
using Tunnel.Daemon.Api;
using Tunnel.Daemon.Services;

// ──────────────────────────────────────────────────────────
// Tunnel Daemon — Minimal API + Systemd User Service
// Port: 6385 | Auth: Basic (hardcoded)
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

// ── Basic Auth Middleware ──────────────────────────────────
// Pre-computed: base64("tunnel:Tun3l@2024!")
const string ValidAuthHeader = "Basic dHVubmVsOlR1bjNsQDIwMjQh";

app.Use(async (ctx, next) =>
{
    // Health endpoint skips auth
    if (ctx.Request.Path == "/health")
    {
        await next();
        return;
    }

    if (!ctx.Request.Headers.TryGetValue("Authorization", out var authVal)
        || authVal.ToString() != ValidAuthHeader)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsync("Unauthorized");
        return;
    }

    await next();
});

app.MapTunnelEndpoints();

app.Run();
