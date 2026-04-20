using System.Diagnostics;
using System.Text;

namespace LuaEditor;

/// <summary>
/// Handles decompilation (Python ljd) and recompilation (luac.exe).
/// </summary>
internal class LuaProcessor
{
    private readonly string  _helperScript;
    private readonly string  _compileScript;
    private readonly string  _luajitExe;
    private readonly string? _pythonExe;

    public LuaProcessor()
    {
        string baseDir  = AppDomain.CurrentDomain.BaseDirectory;
        _helperScript   = Path.Combine(baseDir, "decompile_helper.py");
        _compileScript  = Path.Combine(baseDir, "compile_helper.py");
        _luajitExe      = Path.Combine(baseDir, "luajit.exe");
        _pythonExe      = FindPython();
    }

    public bool IsReady(out string missing)
    {
        var problems = new List<string>();
        if (!File.Exists(_helperScript))  problems.Add($"Missing: {_helperScript}");
        if (!File.Exists(_compileScript)) problems.Add($"Missing: {_compileScript}");
        if (!File.Exists(_luajitExe))     problems.Add($"Missing: {_luajitExe}");
        if (_pythonExe == null)           problems.Add("Python not found in PATH (install Python 3.7+)");
        missing = string.Join("\n", problems);
        return problems.Count == 0;
    }

    /// <summary>Decompile decrypted LuaJIT bytecode to Lua source text.</summary>
    public string Decompile(byte[] bytecode)
    {
        string tempIn  = Path.GetTempFileName();
        string tempOut = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempIn, bytecode);

            var (exitCode, stdout, stderr) = RunProcess(
                _pythonExe!,
                $"\"{_helperScript}\" \"{tempIn}\" \"{tempOut}\"",
                timeoutMs: 15_000);

            if (exitCode != 0)
                throw new Exception(string.IsNullOrWhiteSpace(stderr) ? "Decompilation failed (no error output)." : stderr.Trim());

            if (!File.Exists(tempOut) || new FileInfo(tempOut).Length == 0)
                throw new Exception("Decompiler produced no output.");

            return File.ReadAllText(tempOut, Encoding.UTF8);
        }
        finally
        {
            TryDelete(tempIn);
            TryDelete(tempOut);
        }
    }

    /// <summary>Compile Lua source text to Elsword LuaJIT bytecode.</summary>
    public byte[] Compile(string sourceCode)
    {
        string tempSrc = Path.GetTempFileName() + ".lua";
        string tempOut = Path.GetTempFileName() + ".ljbc";
        try
        {
            File.WriteAllText(tempSrc, sourceCode, new UTF8Encoding(false));

            var (exitCode, _, stderr) = RunProcess(
                _pythonExe!,
                $"\"{_compileScript}\" \"{tempSrc}\" \"{tempOut}\"",
                timeoutMs: 15_000);

            if (exitCode != 0)
                throw new Exception(string.IsNullOrWhiteSpace(stderr) ? "Compilation failed (no error output)." : stderr.Trim());

            if (!File.Exists(tempOut) || new FileInfo(tempOut).Length == 0)
                throw new Exception("Compiler produced no output.");

            return File.ReadAllBytes(tempOut);
        }
        finally
        {
            TryDelete(tempSrc);
            TryDelete(tempOut);
        }
    }

    private static (int exitCode, string stdout, string stderr) RunProcess(string exe, string args, int timeoutMs)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        using var proc = Process.Start(psi)
            ?? throw new Exception($"Failed to start process: {exe}");

        // Read both streams on background threads to avoid deadlock
        var stdoutTask = Task.Run(() => proc.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => proc.StandardError.ReadToEnd());

        bool finished = proc.WaitForExit(timeoutMs);
        if (!finished)
        {
            try { proc.Kill(); } catch { }
            throw new Exception($"Process timed out after {timeoutMs / 1000}s: {exe}");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();

        return (proc.ExitCode, stdout, stderr);
    }

    private static string? FindPython()
    {
        foreach (var candidate in new[] { "python", "py", "python3" })
        {
            try
            {
                var psi = new ProcessStartInfo(candidate, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
                if (p?.ExitCode == 0) return candidate;
            }
            catch { }
        }
        return null;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
