using Tunnel.Daemon.Services;
using Tunnel.Shared.Models;

namespace Tunnel.Daemon.Api;

/// <summary>
/// Defines all Minimal API endpoints.
/// Optimized for Native AOT: no reflection, no MVC conventions.
/// </summary>
public static class TunnelEndpoints
{
    public static void MapTunnelEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Health check — no auth required
        app.MapGet("/health", () => "OK");

        // ── Tunnel Control ──────────────────────────────────────────────

        api.MapGet("/status", (TunnelService tunnel) =>
        {
            var status = tunnel.GetStatus();
            return Results.Ok(ApiResponse<TunnelStatusModel>.Ok(status));
        });

        api.MapPost("/start", async (Profile profile, TunnelService tunnel) =>
        {
            try
            {
                await tunnel.StartAsync(profile);
                return Results.Ok(ApiResponse<string>.Ok("started",
                    $"Tunnel started: {profile.Name}"));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<string>.Fail(ex.Message));
            }
        });

        api.MapPost("/stop", async (TunnelService tunnel) =>
        {
            await tunnel.StopAsync();
            return Results.Ok(ApiResponse<string>.Ok("stopped", "Tunnel stopped"));
        });

        // ── Profile Management ──────────────────────────────────────────

        api.MapGet("/profiles", (ProfileService profiles) =>
            Results.Ok(ApiResponse<ProfilesConfig>.Ok(profiles.GetConfig())));

        api.MapPost("/profiles", async (ProfilesConfig config, ProfileService profiles) =>
        {
            await profiles.SaveConfigAsync(config);
            return Results.Ok(ApiResponse<string>.Ok("saved",
                $"Saved {config.Profiles.Count} profile(s)"));
        });
    }
}
