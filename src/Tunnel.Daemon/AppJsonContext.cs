using System.Text.Json.Serialization;
using Tunnel.Shared.Models;

namespace Tunnel.Daemon;

/// <summary>
/// AOT-safe JSON serialization context for Minimal API.
/// All types used in endpoints must be declared here.
/// No reflection — compile-time source generation.
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
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<VersionModel>))]
[JsonSerializable(typeof(ApiResponse<TunnelStatusModel>))]
[JsonSerializable(typeof(ApiResponse<ProfilesConfig>))]
[JsonSerializable(typeof(List<ProfileListItem>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class AppJsonContext : JsonSerializerContext;
