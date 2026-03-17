using System.CommandLine;
using Spectre.Console;
using Tunnel.Shared.Models;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel add --name &lt;alias&gt; --local &lt;port&gt; --remote &lt;port&gt; [--remote-host &lt;host&gt;]
/// Adds a port mapping to the currently ACTIVE profile.
/// </summary>
public sealed class AddCommand
{
    public Command Build()
    {
        var nameOpt       = new Option<string>("--name", "Unique name for this forwarding rule") { IsRequired = true };
        var localOpt      = new Option<int>("--local", "Local port to bind") { IsRequired = true };
        var remoteOpt     = new Option<int>("--remote", "Remote port to forward to") { IsRequired = true };
        var remoteHostOpt = new Option<string>("--remote-host", () => "127.0.0.1", "Remote host (default: 127.0.0.1)");

        var cmd = new Command("add", "Add a port mapping to the active profile")
        {
            nameOpt, localOpt, remoteOpt, remoteHostOpt
        };

        cmd.SetHandler(async (name, local, remote, remoteHost) =>
            await HandleAsync(name, local, remote, remoteHost),
            nameOpt, localOpt, remoteOpt, remoteHostOpt);

        return cmd;
    }

    private static async Task HandleAsync(string name, int local, int remote, string remoteHost)
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/]");
            return;
        }

        // Need active profile
        var statusResp = await api.GetStatusAsync();
        var status = statusResp?.Data;

        if (status is null || !status.IsConnected || string.IsNullOrEmpty(status.ActiveProfile))
        {
            AnsiConsole.MarkupLine("[red]✗ No active profile. Run [yellow]tunnel use <name>[/] first.[/]");
            return;
        }

        var activeProfileName = status.ActiveProfile;

        // Load profiles config
        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data;
        if (config is null)
        {
            AnsiConsole.MarkupLine("[red]✗ Could not retrieve config from daemon.[/]");
            return;
        }

        var profile = config.Profiles.FirstOrDefault(p => p.Name == activeProfileName);
        if (profile is null)
        {
            AnsiConsole.MarkupLine($"[red]✗ Active profile '[yellow]{activeProfileName}[/]' not found in config.[/]");
            return;
        }

        // Validate unique name
        if (profile.Ports.Any(p => p.Name == name))
        {
            AnsiConsole.MarkupLine($"[red]✗ Port forwarding name '[yellow]{name}[/]' already exists in profile '{activeProfileName}'.[/]");
            return;
        }

        profile.Ports.Add(new PortMapping
        {
            Name = name, Local = local, Remote = remote, RemoteHost = remoteHost
        });

        // Hot-reload: push updated config to daemon
        var saveResp = await api.SaveProfilesAsync(config);

        if (saveResp?.Success == true)
            AnsiConsole.MarkupLine(
                $"[green]✔ Added '[cyan]{name}[/]':[/] localhost:[bold]{local}[/] → {remoteHost}:[bold]{remote}[/] " +
                $"[grey](profile: {activeProfileName})[/]");
        else
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {saveResp?.Message}");
    }
}
