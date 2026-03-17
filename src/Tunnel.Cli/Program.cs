using System.CommandLine;
using Spectre.Console;
using Tunnel.Cli.Commands;
using Tunnel.Shared;

// ─────────────────────────────────────────────────────────────
// SSH Tunnel Manager CLI
// Controls the Tunnel Daemon via HTTP API (localhost:6385)
// ─────────────────────────────────────────────────────────────

// Show version header (skip when -v / version subcommand handles it)
if (args is not ["-v" or "version", ..])
{
    AnsiConsole.MarkupLine($"[bold cyan]🚇 SSH Tunnel Manager[/] [grey]v{AppVersion.Current}[/]");
    AnsiConsole.WriteLine();
}

var rootCommand = new RootCommand("Multi-port SSH tunnel manager for Ubuntu");

rootCommand.AddCommand(new NewCommand().Build());
rootCommand.AddCommand(new AddCommand().Build());
rootCommand.AddCommand(new ListCommand().Build());
rootCommand.AddCommand(new UseCommand().Build());
rootCommand.AddCommand(new StopCommand().Build());
rootCommand.AddCommand(new StatusCommand().Build());
rootCommand.AddCommand(new RemoveCommand().Build());
rootCommand.AddCommand(new ReconnectCommand().Build());
rootCommand.AddCommand(new CleanCommand().Build());
rootCommand.AddCommand(new UpdateCommand().Build());
rootCommand.AddCommand(new VersionCommand().Build());

return await rootCommand.InvokeAsync(args);
