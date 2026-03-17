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

        var resp = await api.AddPortLiveAsync(name, local, remote, remoteHost);

        if (resp?.Success == true)
            AnsiConsole.MarkupLine(
                $"[green]✔ Added '[cyan]{name}[/]':[/] localhost:[bold]{local}[/] → {remoteHost}:[bold]{remote}[/]");
        else
            AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message ?? "No response from daemon."}");
    }
}
