using System.Text.Json.Serialization;
using Tunnel.Shared.Models;

namespace Tunnel.Cli;

/// <summary>
/// AOT-safe JSON context for CLI (communicates with Daemon API).
/// All types must be registered to avoid reflection at runtime.
/// </summary>
[JsonSerializable(typeof(ProfilesConfig))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(JumpHostConfig))]
[JsonSerializable(typeof(PortMapping))]
[JsonSerializable(typeof(TunnelStatusModel))]
[JsonSerializable(typeof(PortStatus))]
[JsonSerializable(typeof(ProfileListItem))]
[JsonSerializable(typeof(RemovePortRequest))]
[JsonSerializable(typeof(RemoveProfileRequest))]
[JsonSerializable(typeof(ReconnectRequest))]
[JsonSerializable(typeof(VersionModel))]
[JsonSerializable(typeof(ApiResponse<VersionModel>))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<TunnelStatusModel>))]
[JsonSerializable(typeof(ApiResponse<ProfilesConfig>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CliJsonContext : JsonSerializerContext;
