using Renci.SshNet;
using Tunnel.Shared.Models;

namespace Tunnel.Daemon.Services;

/// <summary>
/// Core SSH Tunnel Service.
/// Manages an SshClient and ForwardedPortLocal instances for multi-port tunneling via jump host.
/// </summary>
public sealed class TunnelService : IDisposable
{
    private SshClient? _client;
    private readonly List<ForwardedPortLocal> _activePorts = [];
    private Profile? _activeProfile;
    private readonly ILogger<TunnelService> _logger;

    public TunnelService(ILogger<TunnelService> logger) => _logger = logger;

    public bool IsConnected => _client?.IsConnected == true;

    /// <summary>Returns full status including per-port connection details.</summary>
    public TunnelStatusModel GetStatus() => new()
    {
        IsConnected = IsConnected,
        ActiveProfile = _activeProfile?.Name ?? string.Empty,
        JumpHost = _activeProfile?.JumpHost is { } jh
            ? $"{jh.User}@{jh.Host}:{jh.Port}"
            : string.Empty,
        Ports = _activePorts.Select(p => new PortStatus
        {
            LocalPort = (int)p.BoundPort,
            RemoteHost = p.Host,
            RemotePort = (int)p.Port,
            IsStarted = p.IsStarted
        }).ToList()
    };

    /// <summary>Connects to the jump host and begins forwarding all ports from the profile.</summary>
    public async Task StartAsync(Profile profile)
    {
        if (IsConnected) await StopAsync();

        var jh = profile.JumpHost;
        _logger.LogInformation("Connecting to {User}@{Host}:{Port}...", jh.User, jh.Host, jh.Port);

        var keyPath = jh.KeyPath.Replace("~",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var authMethod = new PrivateKeyAuthenticationMethod(jh.User, new PrivateKeyFile(keyPath));
        var connInfo = new ConnectionInfo(jh.Host, jh.Port, jh.User, authMethod);

        _client = new SshClient(connInfo);

        // SSH connect is blocking — run on thread pool
        await Task.Run(() => _client.Connect());

        _logger.LogInformation("SSH connected. Forwarding {Count} port(s)...", profile.Ports.Count);

        foreach (var pm in profile.Ports)
        {
            var fwdPort = new ForwardedPortLocal(
                "127.0.0.1", (uint)pm.Local,
                pm.RemoteHost, (uint)pm.Remote);

            _client.AddForwardedPort(fwdPort);
            fwdPort.Start();
            _activePorts.Add(fwdPort);

            _logger.LogInformation(
                "  forwarding localhost:{Local} -> {RemoteHost}:{Remote}",
                pm.Local, pm.RemoteHost, pm.Remote);
        }

        _activeProfile = profile;
    }

    /// <summary>Stops all port forwarders and closes the SSH connection.</summary>
    public async Task StopAsync()
    {
        if (!IsConnected && _activePorts.Count == 0) return;

        foreach (var port in _activePorts)
        {
            if (port.IsStarted) port.Stop();
            port.Dispose();
        }
        _activePorts.Clear();

        await Task.Run(() => _client?.Disconnect());
        _client?.Dispose();
        _client = null;
        _activeProfile = null;

        _logger.LogInformation("Tunnel stopped.");
    }

    public void Dispose()
    {
        _activePorts.ForEach(p => p.Dispose());
        _client?.Dispose();
    }
}
