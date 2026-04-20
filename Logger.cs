namespace LuaEditor;

internal static class Logger
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "LuaEditor.log");

    public static void Write(string context, Exception ex)
        => Write(context, ex.ToString());

    public static void Write(string context, string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n{message}\n{new string('-', 60)}\n";
            File.AppendAllText(LogPath, line);
        }
        catch { /* never crash on logging */ }
    }

    public static string FilePath => LogPath;
}
