using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Spectre.Console;
using Tunnel.Shared;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel update [--version v1.x.x] [--daemon-only]
/// Checks latest version from GitHub API, downloads and swaps binaries.
/// If permission denied, suggests running with sudo.
/// </summary>
public sealed class UpdateCommand
{
    private const string GithubReleaseBase =
        "https://github.com/skyber2016/tunnel/releases";

    private const string GitHubApiUrl =
        "https://api.github.com/repos/skyber2016/tunnel/releases/latest";

    public Command Build()
    {
        var daemonOnlyFlag = new Option<bool>("--daemon-only", "Only update the daemon binary");
        var versionOpt = new Option<string?>(
            "--version",
            () => null,
            "Target version tag, e.g. v1.2.0 (default: latest)");

        var cmd = new Command("update", "Update tunnel to a specific or latest version")
        {
            daemonOnlyFlag, versionOpt
        };

        cmd.SetHandler(async (daemonOnly, version) =>
            await HandleAsync(daemonOnly, version),
            daemonOnlyFlag, versionOpt);

        return cmd;
    }

    private static async Task HandleAsync(bool daemonOnly, string? version)
    {

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"tunnel-updater/{AppVersion.Current}");

        string targetVersion;
        string baseUrl;

        if (version is not null)
        {
            // User specified a version explicitly
            var tag = version.StartsWith('v') ? version : $"v{version}";
            targetVersion = tag.TrimStart('v');
            baseUrl = $"{GithubReleaseBase}/download/{tag}";
        }
        else
        {
            // Check latest from GitHub API
            AnsiConsole.MarkupLine("[grey]Checking latest version...[/]");

            try
            {
                var json = await http.GetStringAsync(GitHubApiUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                targetVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed to check latest version:[/] {ex.Message}");
                return;
            }

            if (targetVersion == AppVersion.Current)
            {
                AnsiConsole.MarkupLine($"[green]✓ Already up to date (v{AppVersion.Current}).[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[yellow]New version available:[/] [bold]{targetVersion}[/]");
            baseUrl = $"{GithubReleaseBase}/latest/download";
        }

        // Show current vs target
        AnsiConsole.MarkupLine($"  Current [cyan]{AppVersion.Current}[/] → Target [green]{targetVersion}[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(),
                     new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                if (!daemonOnly)
                {
                    var cliTask = ctx.AddTask("[yellow]CLI binary[/]");
                    var cliInstallPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                        ? Environment.ProcessPath! 
                        : "/usr/local/bin/tunnel";
                    await DownloadAndSwapAsync(http, "tunnel", baseUrl,
                        cliInstallPath, cliTask);
                }

                var daemonTask = ctx.AddTask("[cyan]Daemon binary[/]");
                var daemonInstallPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.Combine(AppContext.BaseDirectory, "tunnel-daemon.exe")
                    : "/usr/local/bin/tunnel-daemon";
                await DownloadAndSwapAsync(http, "tunnel-daemon", baseUrl,
                    daemonInstallPath, daemonTask);
            });

        // Restart daemon gracefully
        DaemonManager.RestartDaemon();
        AnsiConsole.MarkupLine($"[green]✔ Updated to v{targetVersion}. Daemon restarted.[/]");
    }

    // ── Download + atomic swap ──────────────────────────────────────

    private static async Task DownloadAndSwapAsync(
        HttpClient http, string binaryName, string baseUrl, string installPath, ProgressTask task)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName   = isWindows ? $"{binaryName}-win-x64.exe" : $"{binaryName}-linux-x64";
        var binaryUrl  = $"{baseUrl}/{fileName}";
        var tmpPath    = isWindows 
            ? Path.Combine(Path.GetTempPath(), $"{binaryName}_new.exe")
            : $"/tmp/{binaryName}_new";
        var backupPath = $"{installPath}.old";

        // ── Download binary ─────────────────────────────────────────
        task.Description = $"[grey]Downloading {binaryName}...[/]";

        using var response = await http.GetAsync(binaryUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var netStream = await response.Content.ReadAsStreamAsync();
        await using var tmpFile   = File.Create(tmpPath);

        var buffer = new byte[8192];
        long downloaded = 0;
        int  read;
        while ((read = await netStream.ReadAsync(buffer)) > 0)
        {
            await tmpFile.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (totalBytes > 0) task.Value = (double)downloaded / totalBytes * 95;
        }
        await tmpFile.FlushAsync();
        tmpFile.Close();

        task.Value = 96;

        // ── Atomic swap — pure C#, no shell ─────────────────────────
        task.Description = $"[grey]Installing {binaryName}...[/]";

        try
        {
            if (File.Exists(installPath))
                File.Move(installPath, backupPath, overwrite: true);

            File.Move(tmpPath, installPath, overwrite: true);

            // Set executable permissions (755)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                File.SetUnixFileMode(installPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
        catch (UnauthorizedAccessException)
        {
            task.Description = $"[red]✗ {binaryName} — permission denied[/]";
            AnsiConsole.MarkupLine($"\n[red]✗ Permission denied writing to {installPath}[/]");
            AnsiConsole.MarkupLine("[yellow]  Try again with:[/] [cyan]sudo tunnel update[/]");
            throw;
        }

        task.Value = 100;
        task.Description = $"[green]✔ {binaryName} updated[/]";
    }
}
