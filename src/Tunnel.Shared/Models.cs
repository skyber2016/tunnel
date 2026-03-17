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
// API Response / Status Models
// ──────────────────────────────────────────────

public sealed class PortStatus
{
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
