using System.Text.Json;
using Tunnel.Shared.Models;

namespace Tunnel.Daemon.Services;

/// <summary>
/// Reads and writes profiles.json at ~/.tunnel/profiles.json.
/// The daemon is the single source of truth for persisting config.
/// </summary>
public sealed class ProfileService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".tunnel", "profiles.json");

    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ILogger<ProfileService> logger)
    {
        _logger = logger;
        EnsureConfigDir();
    }

    public ProfilesConfig GetConfig()
    {
        if (!File.Exists(ConfigPath))
            return new ProfilesConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.ProfilesConfig)
                   ?? new ProfilesConfig();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cannot read config: {Message}", ex.Message);
            return new ProfilesConfig();
        }
    }

    public async Task SaveConfigAsync(ProfilesConfig config)
    {
        EnsureConfigDir();
        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.ProfilesConfig);
        await File.WriteAllTextAsync(ConfigPath, json);
        _logger.LogInformation("Config saved to {Path}", ConfigPath);
    }

    /// <summary>Re-reads profiles.json from disk — always returns the latest state.</summary>
    public Task<ProfilesConfig> ReloadAsync() => Task.FromResult(GetConfig());

    private static void EnsureConfigDir()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
    }
}
