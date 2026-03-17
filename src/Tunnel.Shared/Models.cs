namespace Tunnel.Shared.Models;

// ──────────────────────────────────────────────
// Profile Configuration Models
// ──────────────────────────────────────────────

public sealed class JumpHostConfig
{
    public string Host { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string KeyPath { get; set; } = "~/.ssh/id_rsa";
}

public sealed class PortMapping
{
    /// <summary>Unique name for this port forwarding rule (used by tunnel remove --name).</summary>
    public string Name { get; set; } = string.Empty;
    public int Local { get; set; }
    public int Remote { get; set; }
    public string RemoteHost { get; set; } = "127.0.0.1";
}

public sealed class Profile
{
    public string Name { get; set; } = string.Empty;
    public JumpHostConfig JumpHost { get; set; } = new();
    public List<PortMapping> Ports { get; set; } = [];
}

public sealed class ProfilesConfig
{
    public List<Profile> Profiles { get; set; } = [];
}

// ──────────────────────────────────────────────
// API Request Models
// ──────────────────────────────────────────────

public sealed class RemovePortRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class RemoveProfileRequest
{
    public string ProfileName { get; set; } = string.Empty;
}

public sealed class ReconnectRequest
{
    /// <summary>If set, reconnect the entire profile (close + reopen).</summary>
    public string? ProfileName { get; set; }
    /// <summary>If set, reconnect only the named port forwarding in the active profile.</summary>
    public string? Name { get; set; }
}

// ──────────────────────────────────────────────
// API Response / Status Models
// ──────────────────────────────────────────────

public sealed class PortStatus
{
    public string Name { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public int LocalPort { get; set; }
    public int RemotePort { get; set; }
    public string RemoteHost { get; set; } = string.Empty;
    public bool IsStarted { get; set; }
}

public sealed class TunnelStatusModel
{
    public bool IsConnected { get; set; }
    public string ActiveProfile { get; set; } = string.Empty;
    public string JumpHost { get; set; } = string.Empty;
    public List<PortStatus> Ports { get; set; } = [];
}

public sealed class ProfileListItem
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class VersionModel
{
    public string Daemon { get; set; } = string.Empty;
}

public sealed class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "OK") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
