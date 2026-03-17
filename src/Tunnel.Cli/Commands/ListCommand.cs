using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel list
/// Lists all profiles from daemon (falls back to local file if daemon is not running).
/// </summary>
public sealed class ListCommand
{
    public Command Build()
    {
        var cmd = new Command("list", "List all SSH profiles");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    private static async Task HandleAsync()
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            var localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".tunnel", "profiles.json");

            AnsiConsole.MarkupLine("[yellow]⚠ Daemon is not running. Reading local file:[/]");

            if (!File.Exists(localPath))
            {
                AnsiConsole.MarkupLine("[grey]No profiles yet.[/]");
                return;
            }

            var json = File.ReadAllText(localPath);
            var config = System.Text.Json.JsonSerializer.Deserialize(
                json, CliJsonContext.Default.ProfilesConfig);
            RenderTable(config?.Profiles ?? []);
            return;
        }

        var resp = await api.GetProfilesAsync();
        RenderTable(resp?.Data?.Profiles ?? []);
    }

    private static void RenderTable(IList<Tunnel.Shared.Models.Profile> profiles)
    {
        if (profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No profiles found. Use 'tunnel new <name>' to create one.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[cyan]Profile[/]"))
            .AddColumn(new TableColumn("[cyan]Jump Host[/]"))
            .AddColumn(new TableColumn("[cyan]User[/]"))
            .AddColumn(new TableColumn("[cyan]Ports[/]"));

        foreach (var p in profiles)
        {
            var portsSummary = p.Ports.Count == 0
                ? "[grey]none[/]"
                : string.Join(", ", p.Ports.Select(pm => $"{pm.Local}→{pm.Remote}"));

            table.AddRow(
                $"[yellow]{p.Name}[/]",
                p.JumpHost.Host,
                p.JumpHost.User,
                portsSummary);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{profiles.Count} profile(s) total.[/]");
    }
}
