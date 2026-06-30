namespace OUT_RayMicro.Runtime;

public static class OutmCrashLog
{
    private const string FileName = "OUT_RayMicro_crash.log";

    public static void Write(string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, FileName), line);
            File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), FileName), line);
        }
        catch
        {
            // Crash logging must never become the crash. Humanity has already explored that genre.
        }
    }
}
