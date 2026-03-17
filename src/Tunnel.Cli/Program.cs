using System.CommandLine;
using Spectre.Console;
using Tunnel.Cli.Commands;

// ─────────────────────────────────────────────────────────────
// SSH Tunnel Manager CLI
// Controls the Tunnel Daemon via HTTP API (localhost:6385)
// ─────────────────────────────────────────────────────────────

AnsiConsole.MarkupLine("[bold cyan]🚇 SSH Tunnel Manager[/] [grey]v1.0.0[/]");
AnsiConsole.MarkupLine("[grey]  github.com/skyber2016/tunnel[/]");
AnsiConsole.WriteLine();

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

return await rootCommand.InvokeAsync(args);
