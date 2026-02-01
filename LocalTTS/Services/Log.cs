using System.IO;

namespace LocalTTS.Services;

public static class Log
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "localtts.log");

    private static readonly object Lock = new();

    public static event Action<string>? LineWritten;

    public static void Info(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        lock (Lock)
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        LineWritten?.Invoke(line);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var line = ex != null
            ? $"[{DateTime.Now:HH:mm:ss}] ERROR: {message} - {ex.GetType().Name}: {ex.Message}"
            : $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}";
        lock (Lock)
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        LineWritten?.Invoke(line);
    }
}
