namespace MediaRelic.App;

public sealed class RelicLogger
{
    private readonly object _lock = new();
    private readonly string _path;

    public RelicLogger(string? path = null)
    {
        _path = path ?? Path.Combine(AppContext.BaseDirectory, "MediaRelic.log");
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERR", exception is null ? message : message + Environment.NewLine + exception);
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(
                    _path,
                    $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never kill playback. That would be impressively stupid.
        }
    }
}
