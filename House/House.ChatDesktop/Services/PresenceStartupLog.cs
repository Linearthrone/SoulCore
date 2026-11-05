using System.Runtime.InteropServices;
using System.Text;

namespace House.ChatDesktop.Services;

/// <summary>
/// PROP-4.2: WinExe builds exit silently on crash — write a startup trail and
/// show a MessageBox on fatal errors so Setup.exe / Start Menu launches are diagnosable.
/// Log: %LocalAppData%\SoulCore\presence-startup.log
/// </summary>
public static class PresenceStartupLog
{
    public static string LogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SoulCore",
            "presence-startup.log");

    public static void Write(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line, Encoding.UTF8);
        }
        catch
        {
            // Never throw from diagnostics.
        }
    }

    public static void WriteException(string stage, Exception ex) =>
        Write($"{stage}: {ex}");

    /// <summary>Show a blocking error on Windows so WinExe failures are not invisible.</summary>
    public static void ShowFatal(string title, string message)
    {
        Write($"FATAL UI: {title} — {message}");
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            _ = MessageBoxW(
                IntPtr.Zero,
                message + Environment.NewLine + Environment.NewLine + "Log: " + LogPath,
                title,
                0x00000010 /* MB_ICONERROR */);
        }
        catch
        {
            // Ignore MessageBox failures.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
