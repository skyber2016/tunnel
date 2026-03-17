using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel use &lt;name&gt;
/// Activates a profile: reads it from daemon and initiates SSH tunnel connection.
/// </summary>
public sealed class UseCommand
{
    public Command Build()
    {
        var nameArg = new Argument<string>("name", "Profile name to activate");
        var cmd = new Command("use", "Connect SSH tunnel using a specific profile") { nameArg };
        cmd.SetHandler(async (name) => await HandleAsync(name), nameArg);
        return cmd;
    }

    private static async Task HandleAsync(string name)
    {
        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/] Start it: [grey]systemctl --user start tunnel[/]");
            return;
        }

        var configResp = await api.GetProfilesAsync();
        var profile = configResp?.Data?.Profiles.FirstOrDefault(p => p.Name == name);

        if (profile is null)
        {
            AnsiConsole.MarkupLine($"[red]✗ Profile '[yellow]{name}[/]' not found.[/]");
            AnsiConsole.MarkupLine("[grey]Use 'tunnel list' to see available profiles.[/]");
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"Connecting to {profile.JumpHost.User}@{profile.JumpHost.Host}...", async ctx =>
            {
                var resp = await api.StartTunnelAsync(profile);

                if (resp?.Success == true)
                {
                    ctx.Status("[green]Connected![/]");
                    AnsiConsole.MarkupLine($"[green]✔ Tunnel '[yellow]{name}[/]' is active.[/]");
                    AnsiConsole.MarkupLine($"[grey]Jump host:[/] {profile.JumpHost.User}@{profile.JumpHost.Host}:{profile.JumpHost.Port}");

                    if (profile.Ports.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[grey]Port forwards:[/]");
                        foreach (var pm in profile.Ports)
                            AnsiConsole.MarkupLine(
                                $"  [cyan]localhost:{pm.Local}[/] → [grey]{pm.RemoteHost}:{pm.Remote}[/]");
                    }

                    AnsiConsole.MarkupLine("[grey]\nRun 'tunnel status' to view connection details.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗ Connection failed:[/] {resp?.Message}");
                }
            });
    }
}
