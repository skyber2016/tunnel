using System.Net.Http.Json;
using System.Text.Json;
using Tunnel.Shared.Models;

namespace Tunnel.Cli;

/// <summary>
/// HTTP wrapper for communicating with the Tunnel Daemon API.
/// Basic Auth token is hardcoded to match the daemon.
/// </summary>
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;

    // Hardcoded Basic Auth — must match ValidAuthHeader in Daemon/Program.cs
    // base64("tunnel:Tun3l@2024!") = "dHVubmVsOlR1bjNsQDIwMjQh"
    private const string AuthHeader = "Basic dHVubmVsOlR1bjNsQDIwMjQh";
    private const string BaseUrl = "http://localhost:6385";

    public ApiClient()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthHeader);
    }

    /// <summary>Returns true if the daemon process is reachable.</summary>
    public bool IsDaemonRunning()
    {
        try
        {
            var resp = _http.GetAsync("/health").GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<ApiResponse<TunnelStatusModel>?> GetStatusAsync()
    {
        var json = await _http.GetStringAsync("/api/status");
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.ApiResponseTunnelStatusModel);
    }

    public async Task<ApiResponse<string>?> StartTunnelAsync(Profile profile)
    {
        var content = JsonContent.Create(profile, CliJsonContext.Default.Profile);
        var resp = await _http.PostAsync("/api/start", content);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.ApiResponseString);
    }

    public async Task<ApiResponse<string>?> StopTunnelAsync()
    {
        var resp = await _http.PostAsync("/api/stop", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.ApiResponseString);
    }

    public async Task<ApiResponse<ProfilesConfig>?> GetProfilesAsync()
    {
        var json = await _http.GetStringAsync("/api/profiles");
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.ApiResponseProfilesConfig);
    }

    public async Task<ApiResponse<string>?> SaveProfilesAsync(ProfilesConfig config)
    {
        var content = JsonContent.Create(config, CliJsonContext.Default.ProfilesConfig);
        var resp = await _http.PostAsync("/api/profiles", content);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize(json, CliJsonContext.Default.ApiResponseString);
    }

    public void Dispose() => _http.Dispose();
}
