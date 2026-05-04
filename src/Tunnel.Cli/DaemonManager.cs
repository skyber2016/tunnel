using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace Tunnel.Cli;

/// <summary>
/// Cross-platform helper for managing the daemon process lifecycle
/// </summary>
public static class DaemonManager
{
    /// <summary>
    /// Gets the appropriate command/instruction to start the daemon based on OS.
    /// </summary>
    public static string GetStartCommandHelp()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Start 'tunnel-daemon.exe' in background or via Windows Services";
        }
        return "systemctl --user start tunnel";
    }

    /// <summary>
    /// Restarts the daemon gracefully across platforms.
    /// </summary>
    public static void RestartDaemon()
    {
        AnsiConsole.MarkupLine("[grey]Restarting daemon...[/]");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Kill existing process and restart it
            var processes = Process.GetProcessesByName("tunnel-daemon");
            foreach (var p in processes)
            {
                try { p.Kill(); p.WaitForExit(3000); } catch { /* ignore */ }
            }

            var exePath = Path.Combine(AppContext.BaseDirectory, "tunnel-daemon.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Warning: tunnel-daemon.exe not found in CLI directory. Cannot auto-restart on Windows.[/]");
            }
        }
        else
        {
            // Linux
            var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
            if (!string.IsNullOrEmpty(sudoUser))
            {
                Exec("runuser", $"-l {sudoUser} -c \"systemctl --user restart tunnel\"");
            }
            else
            {
                Exec("systemctl", "--user restart tunnel");
            }
        }
    }

    private static void Exec(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to execute daemon command:[/] {ex.Message}");
        }
    }
}
