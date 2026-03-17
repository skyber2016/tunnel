using System.Text.Json.Serialization;
using Tunnel.Shared.Models;

namespace Tunnel.Cli;

/// <summary>
/// AOT-safe JSON context cho CLI (gọi API Daemon).
/// Phải đăng ký đúng tất cả types để tránh reflection lúc runtime.
/// </summary>
[JsonSerializable(typeof(ProfilesConfig))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(JumpHostConfig))]
[JsonSerializable(typeof(PortMapping))]
[JsonSerializable(typeof(TunnelStatusModel))]
[JsonSerializable(typeof(PortStatus))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(ApiResponse<TunnelStatusModel>))]
[JsonSerializable(typeof(ApiResponse<ProfilesConfig>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CliJsonContext : JsonSerializerContext;
