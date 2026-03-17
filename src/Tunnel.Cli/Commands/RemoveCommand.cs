using System.CommandLine;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel remove --name &lt;alias&gt;           Remove a port forwarding rule from the active profile
/// tunnel remove --profile-name &lt;name&gt;    Remove an entire profile (stops tunnel if active)
/// </summary>
public sealed class RemoveCommand
{
    public Command Build()
    {
        var nameOpt        = new Option<string?>("--name", "Name of the port forwarding rule to remove");
        var profileNameOpt = new Option<string?>("--profile-name", "Name of the profile to remove");

        var cmd = new Command("remove", "Remove a port forwarding rule or an entire profile")
        {
            nameOpt, profileNameOpt
        };

        cmd.SetHandler(async (name, profileName) =>
            await HandleAsync(name, profileName),
            nameOpt, profileNameOpt);

        return cmd;
    }

    private static async Task HandleAsync(string? name, string? profileName)
    {
        // Must provide exactly one option
        if (name is null && profileName is null)
        {
            AnsiConsole.MarkupLine("[red]✗ Provide [yellow]--name[/] or [yellow]--profile-name[/].[/]");
            return;
        }
        if (name is not null && profileName is not null)
        {
            AnsiConsole.MarkupLine("[red]✗ Use either [yellow]--name[/] or [yellow]--profile-name[/], not both.[/]");
            return;
        }

        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("[red]✗ Daemon is not running.[/]");
            return;
        }

        // ── Remove port forwarding rule ────────────────────────────
        if (name is not null)
        {
            var statusResp = await api.GetStatusAsync();
            if (statusResp?.Data?.IsConnected != true)
            {
                AnsiConsole.MarkupLine("[red]✗ No active profile. [grey]tunnel remove --name requires an active tunnel.[/][/]");
                return;
            }

            if (!AnsiConsole.Confirm($"Remove port forwarding '[yellow]{name}[/]' from active profile?"))
                return;

            var resp = await api.RemovePortAsync(name);
            if (resp?.Success == true)
                AnsiConsole.MarkupLine($"[green]✔ Port forwarding '[cyan]{name}[/]' removed.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message}");
        }

        // ── Remove entire profile ──────────────────────────────────
        else if (profileName is not null)
        {
            // Check if active
            var statusResp = await api.GetStatusAsync();
            var isActive = statusResp?.Data?.IsConnected == true &&
                           statusResp.Data.ActiveProfile == profileName;

            var warning = isActive
                ? $"[red]ACTIVE[/] profile '[yellow]{profileName}[/]' will be disconnected and deleted"
                : $"Profile '[yellow]{profileName}[/]' and all its port mappings will be deleted";

            if (!AnsiConsole.Confirm($"{warning}. Continue?"))
                return;

            var resp = await api.RemoveProfileAsync(profileName);
            if (resp?.Success == true)
                AnsiConsole.MarkupLine($"[green]✔ Profile '[cyan]{profileName}[/]' removed.[/]");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {resp?.Message}");
        }
    }
}
