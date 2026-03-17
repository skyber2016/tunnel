using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Spectre.Console;
using Tunnel.Shared;

namespace Tunnel.Cli.Commands;

/// <summary>
/// sudo tunnel update [--version v1.x.x] [--daemon-only]
/// Checks latest version from GitHub API, downloads and swaps binaries.
/// Must be run with sudo (root) to write to /usr/local/bin/.
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

    // P/Invoke — geteuid() to check if running as root
    [DllImport("libc")]
    private static extern uint geteuid();

    private static async Task HandleAsync(bool daemonOnly, string? version)
    {
        // ── Root check ──────────────────────────────────────────────
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && geteuid() != 0)
        {
            AnsiConsole.MarkupLine("[red]✗ Permission denied.[/] Run with sudo:");
            AnsiConsole.MarkupLine("[cyan]  sudo tunnel update[/]");
            return;
        }

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
                    await DownloadAndSwapAsync(http, "tunnel", baseUrl,
                        "/usr/local/bin/tunnel", cliTask);
                }

                var daemonTask = ctx.AddTask("[cyan]Daemon binary[/]");
                await DownloadAndSwapAsync(http, "tunnel-daemon", baseUrl,
                    "/usr/local/bin/tunnel-daemon", daemonTask);
            });

        // Restart daemon as the original user (not root)
        AnsiConsole.MarkupLine("[grey]Restarting daemon...[/]");
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
        {
            Exec("runuser", $"-l {sudoUser} -c \"systemctl --user restart tunnel\"");
        }
        else
        {
            Exec("systemctl", "--user restart tunnel");
        }
        AnsiConsole.MarkupLine($"[green]✔ Updated to v{targetVersion}. Daemon restarted.[/]");
    }

    // ── Download + atomic swap ──────────────────────────────────────

    private static async Task DownloadAndSwapAsync(
        HttpClient http, string binaryName, string baseUrl, string installPath, ProgressTask task)
    {
        var fileName   = $"{binaryName}-linux-x64";
        var binaryUrl  = $"{baseUrl}/{fileName}";
        var tmpPath    = $"/tmp/{binaryName}_new";
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

        task.Value = 100;
        task.Description = $"[green]✔ {binaryName} updated[/]";
    }

    private static void Exec(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
    }
}
