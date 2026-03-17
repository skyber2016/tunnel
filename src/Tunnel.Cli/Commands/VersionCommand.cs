using System.CommandLine;
using Spectre.Console;
using Tunnel.Shared;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel -v | tunnel --version
/// Displays CLI version and queries the daemon for its version.
/// </summary>
public sealed class VersionCommand
{
    public Command Build()
    {
        // System.CommandLine reserves --version on RootCommand, so we expose
        // an explicit subcommand as well as hook into the -v short form.
        var cmd = new Command("version", "Show CLI and daemon versions");
        cmd.AddAlias("-v");
        cmd.SetHandler(async () => await HandleAsync());
        return cmd;
    }

    internal static async Task HandleAsync()
    {
        AnsiConsole.MarkupLine($"[bold]SSH Tunnel Manager[/]");
        AnsiConsole.MarkupLine($"  CLI    [cyan]{AppVersion.Current}[/]");

        using var api = new ApiClient();
        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine("  Daemon [grey]not running[/]");
            return;
        }

        var resp = await api.GetVersionAsync();
        var daemonVer = resp?.Data?.Daemon ?? "unknown";
        AnsiConsole.MarkupLine($"  Daemon [cyan]{daemonVer}[/]");
    }
}
