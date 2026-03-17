using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel status
/// Displays port forwarding details of the ACTIVE profile.
/// Columns: Name | Profile | Local Port | Remote Host | Remote Port | Status
/// Requires an active tunnel connection.
/// </summary>
public sealed class StatusCommand
{
    public Command Build()
    {
        var cmd = new Command("status", "Show port forwarding status of the active tunnel");
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

        if (!status.IsConnected)
        {
            var idleRule = new Rule("[grey]○ No active tunnel[/]");
            idleRule.RuleStyle(Style.Parse("grey"));
            AnsiConsole.Write(idleRule);
            AnsiConsole.MarkupLine("[grey]Connect with:[/] [cyan]tunnel use <profile>[/]");
            return;
        }

        // ── Header ────────────────────────────────────────────────────
        var connRule = new Rule($"[green]● CONNECTED[/] — [yellow]{status.ActiveProfile}[/]");
        connRule.RuleStyle(Style.Parse("green"));
        AnsiConsole.Write(connRule);
        AnsiConsole.MarkupLine($"[grey]Jump Host:[/] {status.JumpHost}");
        AnsiConsole.WriteLine();

        if (status.Ports.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No port forwarding rules. Add one with:[/] " +
                "[cyan]tunnel add --name <n> --local <p> --remote <p>[/]");
            return;
        }

        // ── Port Table ────────────────────────────────────────────────
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]Port Forwarding Rules[/]")
            .AddColumn(new TableColumn("[cyan]Name[/]"))
            .AddColumn(new TableColumn("[cyan]Profile[/]"))
            .AddColumn(new TableColumn("[cyan]Local Port[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Remote Host[/]"))
            .AddColumn(new TableColumn("[cyan]Remote Port[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Status[/]").Centered());

        foreach (var p in status.Ports)
        {
            var badge = p.IsStarted ? "[green]● OPEN[/]" : "[red]○ CLOSED[/]";
            table.AddRow(
                $"[bold]{p.Name}[/]",
                p.Profile,
                $"[bold]:{p.LocalPort}[/]",
                p.RemoteHost,
                $":{p.RemotePort}",
                badge);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{status.Ports.Count} rule(s) active.[/]");
    }
}
