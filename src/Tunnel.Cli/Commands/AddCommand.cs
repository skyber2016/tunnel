using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel add &lt;profile&gt; --local &lt;lport&gt; --remote &lt;rport&gt; [--remote-host &lt;host&gt;]
/// Adds a port mapping to an existing profile. Pushes updated config to daemon (hot-reload).
/// </summary>
public sealed class AddCommand
{
    public Command Build()
    {
        var profileArg    = new Argument<string>("profile", "Profile name");
        var localOpt      = new Option<int>("--local", "Local port to bind") { IsRequired = true };
        var remoteOpt     = new Option<int>("--remote", "Remote port to forward to") { IsRequired = true };
        var remoteHostOpt = new Option<string>("--remote-host", () => "127.0.0.1", "Remote host");

        var cmd = new Command("add", "Add a port mapping to a profile")
        {
            profileArg, localOpt, remoteOpt, remoteHostOpt
        };

        cmd.SetHandler(async (profile, local, remote, remoteHost) =>
            await HandleAsync(profile, local, remote, remoteHost),
            profileArg, localOpt, remoteOpt, remoteHostOpt);

        return cmd;
    }

    private static async Task HandleAsync(string profileName, int local, int remote, string remoteHost)
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/]");
            return;
        }

        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data;
        if (config is null)
        {
            AnsiConsole.MarkupLine("[red]✗ Could not retrieve config from daemon.[/]");
            return;
        }

        var profile = config.Profiles.FirstOrDefault(p => p.Name == profileName);
        if (profile is null)
        {
            AnsiConsole.MarkupLine($"[red]✗ Profile '[yellow]{profileName}[/]' not found.[/]");
            return;
        }

        profile.Ports.Add(new Tunnel.Shared.Models.PortMapping
        {
            Local = local, Remote = remote, RemoteHost = remoteHost
        });

        // Hot-reload: push updated config to daemon
        var saveResp = await api.SaveProfilesAsync(config);

        if (saveResp?.Success == true)
            AnsiConsole.MarkupLine(
                $"[green]✔ Added:[/] localhost:[bold]{local}[/] → {remoteHost}:[bold]{remote}[/] on profile [yellow]{profileName}[/]");
        else
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {saveResp?.Message}");
    }
}
