using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel clean
/// Confirms and deletes the entire config file (~/.tunnel/profiles.json).
/// Stops the active tunnel first if running.
/// </summary>
public sealed class CleanCommand
{
    public Command Build()
    {
        var cmd = new Command("clean", "Remove all profiles and reset config (destructive)");
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

        // Show what will be deleted
        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data;
        var profileCount = config?.Profiles.Count ?? 0;

        AnsiConsole.MarkupLine("[yellow]⚠  This will permanently delete ALL profiles and port mappings:[/]");
        AnsiConsole.MarkupLine($"   [grey]~/.tunnel/profiles.json[/] ([cyan]{profileCount}[/] profile(s) will be lost)");
        AnsiConsole.MarkupLine("[yellow]   The active tunnel will be stopped.[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("[red bold]Delete everything?[/]"))
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        var resp = await api.CleanAsync();

        if (resp?.Success == true)
        {
            AnsiConsole.MarkupLine("[green]✔ All profiles deleted. Config reset.[/]");
            AnsiConsole.MarkupLine("[grey]Create a new profile with:[/] [cyan]tunnel new <name>[/]");
        }
        else
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {resp?.Message}");
    }
}
