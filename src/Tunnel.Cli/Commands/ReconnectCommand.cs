using System.CommandLine;
using Spectre.Console;
using Tunnel.Shared.Models;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel reconnect --profile-name &lt;name&gt;   Close and reopen the entire SSH connection + all ports
/// tunnel reconnect --name &lt;alias&gt;           Reconnect a single port forwarding in the active profile
/// </summary>
public sealed class ReconnectCommand
{
    public Command Build()
    {
        var profileNameOpt = new Option<string?>("--profile-name", "Reconnect a full profile (close + reopen)");
        var nameOpt        = new Option<string?>("--name", "Reconnect a single port forwarding by name");

        var cmd = new Command("reconnect", "Reconnect a profile or a specific port forwarding rule")
        {
            profileNameOpt, nameOpt
        };

        cmd.SetHandler(async (profileName, name) =>
            await HandleAsync(profileName, name),
            profileNameOpt, nameOpt);

        return cmd;
    }

    private static async Task HandleAsync(string? profileName, string? name)
    {
        if (profileName is not null && name is not null)
        {
            AnsiConsole.MarkupLine("[red]✗ Use either [yellow]--profile-name[/] or [yellow]--name[/], not both.[/]");
            return;
        }

        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/]");
            return;
        }

        // Resolve active profile
        var statusResp = await api.GetStatusAsync();
        var status = statusResp?.Data;

        if (status?.IsConnected != true || string.IsNullOrEmpty(status.ActiveProfile))
        {
            AnsiConsole.MarkupLine("[red]✗ No active tunnel. Run [yellow]tunnel use <name>[/] first.[/]");
            return;
        }

        // Default: reconnect active profile when no args provided
        if (profileName is null && name is null)
            profileName = status.ActiveProfile;

        ApiResponse<string>? resp = null;

        if (profileName is not null)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"[cyan]Reconnecting profile '{profileName}'...[/]", async _ =>
                {
                    resp = await api.ReconnectAsync(profileName: profileName);
                });

            if (resp?.Success == true)
                AnsiConsole.MarkupLine($"[green]✔ Profile '[cyan]{profileName}[/]' reconnected.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message}");
        }
        else if (name is not null)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"[cyan]Reconnecting port '{name}'...[/]", async _ =>
                {
                    resp = await api.ReconnectAsync(name: name);
                });

            if (resp?.Success == true)
                AnsiConsole.MarkupLine($"[green]✔ Port forwarding '[cyan]{name}[/]' reconnected.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message}");
        }
    }

}
