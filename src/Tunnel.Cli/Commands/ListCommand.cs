using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel list
/// Displays all profiles with connection status: Profile | Host | User | Status
/// </summary>
public sealed class ListCommand
{
    public Command Build()
    {
        var cmd = new Command("list", "List all SSH profiles and their connection status");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    private static async Task HandleAsync()
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Daemon is not running. Showing local config only.[/]");
            await ShowLocalAsync(api);
            return;
        }

        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data;

        if (config is null || config.Profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No profiles found. Create one with:[/] [cyan]tunnel new <name>[/]");
            return;
        }

        // Get active profile name from status
        string activeProfile = string.Empty;
        var statusResp = await api.GetStatusAsync();
        if (statusResp?.Data?.IsConnected == true)
            activeProfile = statusResp.Data.ActiveProfile;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]SSH Profiles[/]")
            .AddColumn(new TableColumn("[cyan]Profile[/]"))
            .AddColumn(new TableColumn("[cyan]Host[/]"))
            .AddColumn(new TableColumn("[cyan]User[/]"))
            .AddColumn(new TableColumn("[cyan]Ports[/]").Centered())
            .AddColumn(new TableColumn("[cyan]Status[/]").Centered());

        foreach (var p in config.Profiles)
        {
            var isActive = p.Name == activeProfile;
            var nameMark  = isActive ? $"[bold green]{p.Name}[/]" : p.Name;
            var statusBadge = isActive ? "[green]● ACTIVE[/]" : "[grey]○ idle[/]";
            var portCount = p.Ports.Count.ToString();

            table.AddRow(nameMark, p.JumpHost.Host, p.JumpHost.User, portCount, statusBadge);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{config.Profiles.Count} profile(s). " +
            $"Use [cyan]tunnel use <name>[/] to connect.[/]");
    }

    private static async Task ShowLocalAsync(ApiClient api)
    {
        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data;
        if (config is null || config.Profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No profiles found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Profile")
            .AddColumn("Host")
            .AddColumn("User")
            .AddColumn("Ports");

        foreach (var p in config.Profiles)
            table.AddRow(p.Name, p.JumpHost.Host, p.JumpHost.User, p.Ports.Count.ToString());

        AnsiConsole.Write(table);
    }
}
