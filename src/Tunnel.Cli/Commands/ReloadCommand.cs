using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel reload
/// Tells the daemon to re-read profiles.json and reconnect the previously active profile.
/// </summary>
public sealed class ReloadCommand
{
    public Command Build()
    {
        var cmd = new Command("reload", "Reload config from disk and reconnect the active profile");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    private static async Task HandleAsync()
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[grey]Reloading config...[/]");

        var resp = await api.ReloadAsync();

        if (resp?.Success == true)
            AnsiConsole.MarkupLine($"[green]✔[/] {resp.Message}");
        else
            AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message ?? "No response from daemon."}");
    }
}
