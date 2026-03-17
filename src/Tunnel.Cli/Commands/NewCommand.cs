using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel new &lt;name&gt;
/// Interactively creates a new SSH profile via Spectre.Console prompts.
/// </summary>
public sealed class NewCommand
{
    public Command Build()
    {
        var nameArg = new Argument<string>("name", "Profile name (e.g., prod-db)");
        var cmd = new Command("new", "Create a new SSH profile") { nameArg };
        cmd.SetHandler(async (name) => await HandleAsync(name), nameArg);
        return cmd;
    }

    private static async Task HandleAsync(string name)
    {
        AnsiConsole.MarkupLine($"[bold cyan]Creating profile:[/] [yellow]{name}[/]");
        AnsiConsole.WriteLine();

        var host    = AnsiConsole.Ask<string>("[cyan]Jump Host[/] (e.g., jump.example.com):");
        var user    = AnsiConsole.Ask<string>("[cyan]SSH User[/] (e.g., admin):");
        var port    = AnsiConsole.Ask("[cyan]SSH Port[/] [grey](default: 22)[/]:", 22);
        var keyPath = AnsiConsole.Ask("[cyan]Key Path[/] [grey](default: ~/.ssh/id_rsa)[/]:",
            "~/.ssh/id_rsa");

        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/] Start it with: [grey]systemctl --user start tunnel[/]");
            return;
        }

        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data ?? new Tunnel.Shared.Models.ProfilesConfig();

        if (config.Profiles.Any(p => p.Name == name))
        {
            AnsiConsole.MarkupLine($"[red]✗ Profile '[yellow]{name}[/]' already exists.[/]");
            return;
        }

        config.Profiles.Add(new Tunnel.Shared.Models.Profile
        {
            Name = name,
            JumpHost = new Tunnel.Shared.Models.JumpHostConfig
            {
                Host = host, User = user, Port = port, KeyPath = keyPath
            }
        });

        var saveResp = await api.SaveProfilesAsync(config);

        if (saveResp?.Success == true)
            AnsiConsole.MarkupLine($"[green]✔ Profile '[yellow]{name}[/]' created.[/]");
        else
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {saveResp?.Message}");
    }
}
