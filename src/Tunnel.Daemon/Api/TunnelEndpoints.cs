using Tunnel.Daemon.Services;
using Tunnel.Shared;
using Tunnel.Shared.Models;

namespace Tunnel.Daemon.Api;

/// <summary>
/// Defines all Minimal API endpoints. Optimized for Native AOT.
/// </summary>
public static class TunnelEndpoints
{
    public static void MapTunnelEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Health check — no auth required
        app.MapGet("/health", () => "OK");

        // Version — no auth required
        app.MapGet("/api/version", () =>
            Results.Ok(ApiResponse<VersionModel>.Ok(
                new VersionModel { Daemon = AppVersion.Current })));

        // ── Tunnel Control ────────────────────────────────────────────

        api.MapGet("/status", (TunnelService tunnel) =>
        {
            var status = tunnel.GetStatus();
            return Results.Ok(ApiResponse<TunnelStatusModel>.Ok(status));
        });

        api.MapPost("/start", async (Profile profile, TunnelService tunnel, ProfileService profiles) =>
        {
            try
            {
                await tunnel.StartAsync(profile);

                // Persist active profile name to config so daemon can reload it on restart
                var config = profiles.GetConfig();
                config.ActiveProfile = profile.Name;
                await profiles.SaveConfigAsync(config);

                return Results.Ok(ApiResponse<string>.Ok("started",
                    $"Tunnel started: {profile.Name}"));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<string>.Fail(ex.Message));
            }
        });

        api.MapPost("/stop", async (TunnelService tunnel, ProfileService profiles) =>
        {
            await tunnel.StopAsync();

            // Clear persisted active profile
            var config = profiles.GetConfig();
            config.ActiveProfile = null;
            await profiles.SaveConfigAsync(config);

            return Results.Ok(ApiResponse<string>.Ok("stopped", "Tunnel stopped"));
        });

        // ── Reload ─────────────────────────────────────────────────────

        api.MapPost("/reload", async (TunnelService tunnel, ProfileService profiles) =>
        {
            var config = await profiles.ReloadAsync();
            var profileName = config.ActiveProfile;

            if (string.IsNullOrEmpty(profileName))
                return Results.Ok(ApiResponse<string>.Ok("reloaded",
                    "Config reloaded. No active profile to reconnect."));

            var profile = config.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile is null)
                return Results.Ok(ApiResponse<string>.Fail(
                    $"Active profile '{profileName}' not found in reloaded config."));

            try
            {
                await tunnel.StartAsync(profile);
                return Results.Ok(ApiResponse<string>.Ok("reloaded",
                    $"Config reloaded and tunnel reconnected: {profileName}"));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<string>.Fail(
                    $"Config reloaded but reconnect failed: {ex.Message}"));
            }
        });

        // ── Remove ───────────────────────────────────────────────────

        /// <summary>Remove a port forwarding by name from the active profile.</summary>
        api.MapPost("/remove/port", async (RemovePortRequest req,
            TunnelService tunnel, ProfileService profiles) =>
        {
            if (!tunnel.IsConnected)
                return Results.Ok(ApiResponse<string>.Fail("No active tunnel."));

            // Remove from live service
            tunnel.RemovePort(req.Name);

            // Remove from persisted config
            var config = profiles.GetConfig();
            var profile = config.Profiles.FirstOrDefault(
                p => p.Name == tunnel.ActiveProfileName);

            if (profile is not null)
            {
                profile.Ports.RemoveAll(p => p.Name == req.Name);
                await profiles.SaveConfigAsync(config);
            }

            return Results.Ok(ApiResponse<string>.Ok("removed",
                $"Port forwarding '{req.Name}' removed."));
        });

        /// <summary>Remove an entire profile. If active, stop the tunnel first.</summary>
        api.MapPost("/remove/profile", async (RemoveProfileRequest req,
            TunnelService tunnel, ProfileService profiles) =>
        {
            // Stop if this profile is currently active
            if (tunnel.IsConnected && tunnel.ActiveProfileName == req.ProfileName)
                await tunnel.StopAsync();

            var config = profiles.GetConfig();
            var removed = config.Profiles.RemoveAll(p => p.Name == req.ProfileName);
            if (removed == 0)
                return Results.Ok(ApiResponse<string>.Fail(
                    $"Profile '{req.ProfileName}' not found."));

            await profiles.SaveConfigAsync(config);
            return Results.Ok(ApiResponse<string>.Ok("removed",
                $"Profile '{req.ProfileName}' removed."));
        });

        // ── Reconnect ────────────────────────────────────────────────

        api.MapPost("/reconnect", async (ReconnectRequest req, TunnelService tunnel) =>
        {
            try
            {
                if (req.ProfileName is not null)
                {
                    await tunnel.ReconnectProfileAsync();
                    return Results.Ok(ApiResponse<string>.Ok("reconnected",
                        $"Profile '{req.ProfileName}' reconnected."));
                }

                if (req.Name is not null)
                {
                    var ok = tunnel.ReconnectPort(req.Name);
                    return ok
                        ? Results.Ok(ApiResponse<string>.Ok("reconnected",
                            $"Port '{req.Name}' reconnected."))
                        : Results.Ok(ApiResponse<string>.Fail(
                            $"Port forwarding '{req.Name}' not found in active profile."));
                }

                return Results.Ok(ApiResponse<string>.Fail(
                    "Provide --profile-name or --name."));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<string>.Fail(ex.Message));
            }
        });

        // ── Profile Management ───────────────────────────────────────

        api.MapGet("/profiles", (ProfileService profiles) =>
            Results.Ok(ApiResponse<ProfilesConfig>.Ok(profiles.GetConfig())));

        api.MapPost("/profiles", async (ProfilesConfig config, ProfileService profiles) =>
        {
            await profiles.SaveConfigAsync(config);
            return Results.Ok(ApiResponse<string>.Ok("saved",
                $"Saved {config.Profiles.Count} profile(s)"));
        });

        // ── Add port to live tunnel + persist ────────────────────────

        api.MapPost("/ports/add", async (AddPortRequest req,
            TunnelService tunnel, ProfileService profiles) =>
        {
            if (!tunnel.IsConnected || tunnel.ActiveProfileName is null)
                return Results.Ok(ApiResponse<string>.Fail(
                    "No active tunnel. Run 'tunnel use <name>' first."));

            var pm = new PortMapping
            {
                Name       = req.Name,
                Local      = req.Local,
                Remote     = req.Remote,
                RemoteHost = req.RemoteHost
            };

            try
            {
                // 1. Start forwarding on live SSH connection
                tunnel.AddPortLive(pm);
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<string>.Fail(ex.Message));
            }

            // 2. Persist to profiles.json
            var config  = profiles.GetConfig();
            var profile = config.Profiles.FirstOrDefault(
                p => p.Name == tunnel.ActiveProfileName);

            if (profile is not null)
            {
                profile.Ports.RemoveAll(p => p.Name == req.Name); // idempotent
                profile.Ports.Add(pm);
                await profiles.SaveConfigAsync(config);
            }

            return Results.Ok(ApiResponse<string>.Ok("added",
                $"Port '{req.Name}' forwarding localhost:{req.Local} → {req.RemoteHost}:{req.Remote}"));
        });

        // ── Clean ────────────────────────────────────────────────────

        api.MapPost("/clean", async (TunnelService tunnel, ProfileService profiles) =>
        {
            // Stop active tunnel if running
            if (tunnel.IsConnected) await tunnel.StopAsync();

            // Overwrite with empty config
            await profiles.SaveConfigAsync(new ProfilesConfig());
            return Results.Ok(ApiResponse<string>.Ok("cleaned",
                "All profiles deleted. Config reset."));
        });
    }
}
