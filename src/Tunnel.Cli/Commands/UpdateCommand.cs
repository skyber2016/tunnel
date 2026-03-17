using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel update
/// Self-update: downloads new binary from GitHub Release, swaps file atomically, restarts daemon.
/// Release URL: https://github.com/skyber2016/tunnel/releases/latest/download/tunnel-linux-{arch}
/// </summary>
public sealed class UpdateCommand
{
    private const string GithubBaseUrl =
        "https://github.com/skyber2016/tunnel/releases/latest/download";

    public Command Build()
    {
        var daemonOnlyFlag = new Option<bool>("--daemon-only", "Only update the daemon binary");
        var cmd = new Command("update", "Self-update tunnel to the latest version") { daemonOnlyFlag };
        cmd.SetHandler(async (daemonOnly) => await HandleAsync(daemonOnly), daemonOnlyFlag);
        return cmd;
    }

    private static async Task HandleAsync(bool daemonOnly)
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64   => "x64",
            Architecture.Arm64 => "arm64",
            _                  => throw new PlatformNotSupportedException(
                $"Architecture {RuntimeInformation.OSArchitecture} is not supported.")
        };

        AnsiConsole.MarkupLine($"[cyan]Architecture:[/] linux-{arch}");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(),
                     new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("tunnel-updater/1.0");

                if (!daemonOnly)
                {
                    var cliTask = ctx.AddTask("[yellow]CLI binary[/]");
                    await DownloadAndSwapAsync(http, "tunnel", arch, "/usr/local/bin/tunnel", cliTask);
                }

                var daemonTask = ctx.AddTask("[cyan]Daemon binary[/]");
                await DownloadAndSwapAsync(http, "tunnel-daemon", arch, "/usr/local/bin/tunnel-daemon", daemonTask);
            });

        AnsiConsole.MarkupLine("[grey]Restarting daemon...[/]");
        RunShell("systemctl --user restart tunnel");
        AnsiConsole.MarkupLine("[green]✔ Update complete. Daemon restarted.[/]");
    }

    private static async Task DownloadAndSwapAsync(
        HttpClient http, string binaryName, string arch, string installPath, ProgressTask task)
    {
        var downloadUrl = $"{GithubBaseUrl}/{binaryName}-linux-{arch}";
        var tmpPath     = $"/tmp/{binaryName}_new";
        var backupPath  = $"{installPath}.old";

        task.Description = $"[grey]Downloading {binaryName}...[/]";

        using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file   = File.Create(tmpPath);

        var buffer = new byte[8192];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (totalBytes > 0) task.Value = (double)downloaded / totalBytes * 100;
        }

        task.Value = 100;
        task.Description = $"[grey]Installing {binaryName}...[/]";

        // Atomic swap: backup → install → chmod
        if (File.Exists(installPath)) RunShell($"mv {installPath} {backupPath}");
        RunShell($"mv {tmpPath} {installPath}");
        RunShell($"chmod +x {installPath}");

        task.Description = $"[green]✔ {binaryName} updated[/]";
    }

    private static void RunShell(string command)
    {
        var psi = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
    }
}
