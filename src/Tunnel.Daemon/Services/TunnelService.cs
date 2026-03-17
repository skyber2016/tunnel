using Renci.SshNet;
using Tunnel.Shared.Models;
using SshConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace Tunnel.Daemon.Services;

/// <summary>
/// Core SSH Tunnel Service.
/// Manages an SshClient and ForwardedPortLocal instances for multi-port tunneling via jump host.
/// </summary>
public sealed class TunnelService : IDisposable
{
    private SshClient? _client;
    private readonly List<(PortMapping Mapping, ForwardedPortLocal Forwarder)> _activePorts = [];
    private Profile? _activeProfile;
    private readonly ILogger<TunnelService> _logger;

    public TunnelService(ILogger<TunnelService> logger) => _logger = logger;

    public bool IsConnected => _client?.IsConnected == true;
    public string? ActiveProfileName => _activeProfile?.Name;

    // ── Status ──────────────────────────────────────────────────────

    /// <summary>Returns full status including per-port connection details.</summary>
    public TunnelStatusModel GetStatus() => new()
    {
        IsConnected = IsConnected,
        ActiveProfile = _activeProfile?.Name ?? string.Empty,
        JumpHost = _activeProfile?.JumpHost is { } jh
            ? $"{jh.User}@{jh.Host}:{jh.Port}"
            : string.Empty,
        Ports = _activePorts.Select(x => new PortStatus
        {
            Name       = x.Mapping.Name,
            Profile    = _activeProfile?.Name ?? string.Empty,
            LocalPort  = x.Mapping.Local,                // use config value — BoundPort can be 0 before start
            RemoteHost = x.Mapping.RemoteHost,           // remote forwarding target (not Forwarder.Host which is local bound addr)
            RemotePort = x.Mapping.Remote,               // remote forwarding port  (not Forwarder.Port which is local bound port)
            IsStarted  = x.Forwarder.IsStarted
        }).ToList()
    };

    // ── Start / Stop ────────────────────────────────────────────────

    /// <summary>Connects to the jump host and begins forwarding all ports from the profile.</summary>
    public async Task StartAsync(Profile profile)
    {
        if (IsConnected) await StopAsync();

        var jh = profile.JumpHost;
        _logger.LogInformation("Connecting to {User}@{Host}:{Port}...", jh.User, jh.Host, jh.Port);

        var keyPath = jh.KeyPath.Replace("~",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var authMethod = new PrivateKeyAuthenticationMethod(jh.User, new PrivateKeyFile(keyPath));
        var connInfo = new SshConnectionInfo(jh.Host, jh.Port, jh.User, authMethod);

        _client = new SshClient(connInfo);

        // SSH connect is blocking — run on thread pool
        await Task.Run(() => _client.Connect());

        _logger.LogInformation("SSH connected. Forwarding {Count} port(s)...", profile.Ports.Count);

        foreach (var pm in profile.Ports)
        {
            AddAndStartPort(pm);
        }

        _activeProfile = profile;
    }

    /// <summary>Stops all port forwarders and closes the SSH connection.</summary>
    public async Task StopAsync()
    {
        if (!IsConnected && _activePorts.Count == 0) return;

        foreach (var (_, fwd) in _activePorts)
        {
            if (fwd.IsStarted) fwd.Stop();
            fwd.Dispose();
        }
        _activePorts.Clear();

        await Task.Run(() => _client?.Disconnect());
        _client?.Dispose();
        _client = null;
        _activeProfile = null;

        _logger.LogInformation("Tunnel stopped.");
    }

    // ── Add port to live tunnel ──────────────────────────────────────

    /// <summary>Adds and starts a new port forwarding on the existing SSH connection.</summary>
    public void AddPortLive(PortMapping pm)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No active SSH connection.");

        if (_activePorts.Any(x => x.Mapping.Name == pm.Name))
            throw new InvalidOperationException(
                $"Port forwarding '{pm.Name}' already exists in the active session.");

        AddAndStartPort(pm);
    }

    // ── Remove ──────────────────────────────────────────────────────

    /// <summary>Removes a port forwarding by name within the active profile.</summary>
    public bool RemovePort(string name)
    {
        var idx = _activePorts.FindIndex(x => x.Mapping.Name == name);
        if (idx < 0) return false;

        var (_, fwd) = _activePorts[idx];
        if (fwd.IsStarted) fwd.Stop();
        fwd.Dispose();
        _activePorts.RemoveAt(idx);

        _logger.LogInformation("Port forwarding '{Name}' removed.", name);
        return true;
    }

    // ── Reconnect ───────────────────────────────────────────────────

    /// <summary>Closes and reopens the entire SSH connection (profile + all ports).</summary>
    public async Task ReconnectProfileAsync()
    {
        if (_activeProfile is null) throw new InvalidOperationException("No active profile.");
        var profile = _activeProfile;
        await StopAsync();
        await StartAsync(profile);
        _logger.LogInformation("Profile '{Name}' reconnected.", profile.Name);
    }

    /// <summary>Stops and restarts a single port forwarding by name within the active profile.</summary>
    public bool ReconnectPort(string name)
    {
        var idx = _activePorts.FindIndex(x => x.Mapping.Name == name);
        if (idx < 0) return false;

        var (mapping, fwd) = _activePorts[idx];

        if (fwd.IsStarted) fwd.Stop();
        fwd.Dispose();
        _activePorts.RemoveAt(idx);

        var newFwd = AddAndStartPort(mapping, insertAt: idx);
        _logger.LogInformation("Port forwarding '{Name}' reconnected on :{Port}.", name, newFwd.BoundPort);
        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private ForwardedPortLocal AddAndStartPort(PortMapping pm, int? insertAt = null)
    {
        var fwdPort = new ForwardedPortLocal(
            "127.0.0.1", (uint)pm.Local,
            pm.RemoteHost, (uint)pm.Remote);

        _client!.AddForwardedPort(fwdPort);
        fwdPort.Start();

        if (insertAt.HasValue)
            _activePorts.Insert(insertAt.Value, (pm, fwdPort));
        else
            _activePorts.Add((pm, fwdPort));

        _logger.LogInformation(
            "  forwarding localhost:{Local} -> {RemoteHost}:{Remote} [{Name}]",
            pm.Local, pm.RemoteHost, pm.Remote, pm.Name);

        return fwdPort;
    }

    public void Dispose()
    {
        foreach (var (_, fwd) in _activePorts) fwd.Dispose();
        _client?.Dispose();
    }
}
