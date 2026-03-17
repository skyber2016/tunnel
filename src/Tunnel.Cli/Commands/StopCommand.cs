using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>tunnel stop — Stops the currently running tunnel.</summary>
public sealed class StopCommand
{
    public Command Build()
    {
        var cmd = new Command("stop", "Stop the active SSH tunnel");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    private static async Task HandleAsync()
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Daemon is not running.[/]");
            return;
        }

        var resp = await api.StopTunnelAsync();

        if (resp?.Success == true)
            AnsiConsole.MarkupLine("[green]✔ Tunnel stopped.[/]");
        else
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {resp?.Message}");
    }
}
