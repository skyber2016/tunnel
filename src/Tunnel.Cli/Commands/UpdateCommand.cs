using System.CommandLine;
using System.Diagnostics;
using System.Security.Cryptography;
using Spectre.Console;
using Tunnel.Shared;

namespace Tunnel.Cli.Commands;

/// <summary>
/// tunnel update [--version v1.x.x] [--daemon-only]
/// Downloads a specific (or latest) release, verifies MD5 checksum, then atomically swaps binaries.
/// Checksum file convention: {binary}-linux-x64.md5
/// </summary>
public sealed class UpdateCommand
{
    private const string GithubReleaseBase =
        "https://github.com/skyber2016/tunnel/releases";

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
        // Resolve download base URL
        var tag      = NormalizeTag(version);
        var baseUrl  = tag is null
            ? $"{GithubReleaseBase}/latest/download"
            : $"{GithubReleaseBase}/download/{tag}";
        var label    = tag ?? "latest";

        // Show current vs target
        AnsiConsole.MarkupLine($"  Current [cyan]{AppVersion.Current}[/] → Target [green]{label}[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(),
                     new PercentageColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    $"tunnel-updater/{AppVersion.Current}");

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

        AnsiConsole.MarkupLine("[grey]Restarting daemon...[/]");
        RunShell("systemctl --user restart tunnel");
        AnsiConsole.MarkupLine($"[green]✔ Updated to {label}. Daemon restarted.[/]");
    }

    // ── Download + MD5 verify + atomic swap ─────────────────────────

    private static async Task DownloadAndSwapAsync(
        HttpClient http, string binaryName, string baseUrl, string installPath, ProgressTask task)
    {
        var fileName   = $"{binaryName}-linux-x64";
        var binaryUrl  = $"{baseUrl}/{fileName}";
        var md5Url     = $"{baseUrl}/{fileName}.md5";
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
            if (totalBytes > 0) task.Value = (double)downloaded / totalBytes * 90; // reserve 10% for verify
        }
        await tmpFile.FlushAsync();
        tmpFile.Close();

        // ── MD5 verification ────────────────────────────────────────
        task.Description = $"[grey]Verifying {binaryName}...[/]";
        task.Value = 92;

        try
        {
            var expectedMd5Resp = await http.GetStringAsync(md5Url);
            var expectedMd5     = expectedMd5Resp.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

            await using var verifyStream = File.OpenRead(tmpPath);
            var actualHash = await MD5.HashDataAsync(verifyStream);
            var actualMd5  = Convert.ToHexString(actualHash).ToLowerInvariant();

            if (actualMd5 != expectedMd5)
            {
                File.Delete(tmpPath);
                throw new InvalidDataException(
                    $"MD5 mismatch for {binaryName}: expected {expectedMd5}, got {actualMd5}");
            }
        }
        catch (HttpRequestException)
        {
            // No .md5 file published — skip verification (warn only)
            AnsiConsole.MarkupLine($"[yellow]⚠ No checksum file for {binaryName} — skipping MD5 verify.[/]");
        }

        task.Value = 96;

        // ── Atomic swap ─────────────────────────────────────────────
        task.Description = $"[grey]Installing {binaryName}...[/]";

        if (File.Exists(installPath)) RunShell($"mv {installPath} {backupPath}");
        RunShell($"mv {tmpPath} {installPath}");
        RunShell($"chmod +x {installPath}");

        task.Value = 100;
        task.Description = $"[green]✔ {binaryName} updated[/]";
    }

    private static string? NormalizeTag(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        return version.StartsWith('v') ? version : $"v{version}";
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
