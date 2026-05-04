using System.CommandLine;
using Spectre.Console;
using Tunnel.Shared;
using Tunnel.Shared.Models;

namespace Tunnel.Cli.Commands;

public sealed class ImportCommand
{
    public Command Build()
    {
        var pathArg = new Argument<string>("file", "Path to the .fwr file");
        var nameOpt = new Option<string>(["--name", "-n"], "Name of the new profile") { IsRequired = true };
        var hostOpt = new Option<string>(["--host", "-h"], "SSH JumpHost address") { IsRequired = true };
        var userOpt = new Option<string>(["--user", "-u"], () => "root", "SSH User");
        var portOpt = new Option<int>(["--port", "-p"], () => 22, "SSH Port");
        var keyOpt = new Option<string>(["--key", "-k"], () => "~/.ssh/id_rsa", "Path to SSH private key");

        var cmd = new Command("import", "Import port forwarding rules from a Bitvise .fwr file")
        {
            pathArg, nameOpt, hostOpt, userOpt, portOpt, keyOpt
        };

        cmd.SetHandler(async (path, name, host, user, port, key) =>
        {
            await HandleAsync(path, name, host, user, port, key);
        }, pathArg, nameOpt, hostOpt, userOpt, portOpt, keyOpt);

        return cmd;
    }

    private static async Task HandleAsync(string path, string name, string host, string user, int port, string key)
    {
        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]✗ File not found:[/] {path}");
            return;
        }

        var ports = FwrParser.Parse(path);
        if (ports.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ No valid Client-to-Server port forwardings found in the file.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]✔ Parsed {ports.Count} port mappings from {path}.[/]");

        using var api = new ApiClient();

        if (!api.IsDaemonRunning())
        {
            AnsiConsole.MarkupLine($"[red]✗ Daemon is not running.[/] Start it with: [grey]{DaemonManager.GetStartCommandHelp()}[/]");
            return;
        }

        var configResp = await api.GetProfilesAsync();
        var config = configResp?.Data ?? new ProfilesConfig();

        if (config.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            AnsiConsole.MarkupLine($"[red]✗ Profile '[yellow]{name}[/]' already exists.[/]");
            return;
        }

        config.Profiles.Add(new Tunnel.Shared.Models.Profile
        {
            Name = name,
            JumpHost = new JumpHostConfig
            {
                Host = host, User = user, Port = port, KeyPath = key
            },
            Ports = ports
        });

        var saveResp = await api.SaveProfilesAsync(config);

        if (saveResp?.Success == true)
        {
            AnsiConsole.MarkupLine($"[green]✔ Profile '[yellow]{name}[/]' imported successfully.[/]");
            AnsiConsole.MarkupLine($"Run [cyan]tunnel use {name}[/] to start it.");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {saveResp?.Message}");
        }
    }
}
