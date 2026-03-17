using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel status
/// Displays a detailed table of all active port forwarding connections.
/// </summary>
public sealed class StatusCommand
{
    public Command Build()
    {
        var cmd = new Command("status", "Show status of active tunnel connections");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    private static async Task HandleAsync()
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            var rule = new Rule("[red]● Daemon is not running[/]");
            rule.RuleStyle(Style.Parse("red dim"));
            AnsiConsole.Write(rule);
            AnsiConsole.MarkupLine("[grey]Start it with:[/] [bold]systemctl --user start tunnel[/]");
            return;
        }

        var resp = await api.GetStatusAsync();
        var status = resp?.Data;

        if (status is null)
        {
            AnsiConsole.MarkupLine("[red]✗ Could not retrieve status from daemon.[/]");
            return;
        }

        // ── Header ─────────────────────────────────────────────────────
        if (status.IsConnected)
        {
            var connRule = new Rule($"[green]● CONNECTED[/] — [yellow]{status.ActiveProfile}[/]");
            connRule.RuleStyle(Style.Parse("green"));
            AnsiConsole.Write(connRule);
            AnsiConsole.MarkupLine($"[grey]Jump Host:[/] {status.JumpHost}");
        }
        else
        {
            var idleRule = new Rule("[grey]○ IDLE — No active tunnel[/]");
            idleRule.RuleStyle(Style.Parse("grey"));
            AnsiConsole.Write(idleRule);
        }

        AnsiConsole.WriteLine();

        // ── Port Connections Table ─────────────────────────────────────
        if (status.Ports.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No active port forwarding connections.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]Port Forwarding Connections[/]")
            .AddColumn(new TableColumn("#").Centered())
            .AddColumn(new TableColumn("[cyan]Local Port[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Direction[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Remote[/]"))
            .AddColumn(new TableColumn("[cyan]Status[/]").Centered());

        for (int i = 0; i < status.Ports.Count; i++)
        {
            var p = status.Ports[i];
            var badge = p.IsStarted ? "[green]● OPEN[/]" : "[red]○ CLOSED[/]";

            table.AddRow(
                $"[grey]{i + 1}[/]",
                $"[bold]:{p.LocalPort}[/]",
                "[grey]→[/]",
                $"{p.RemoteHost}[grey]:[/]{p.RemotePort}",
                badge);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{status.Ports.Count} port(s) forwarded.[/]");
    }
}
