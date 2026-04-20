using System.Runtime.Versioning;
using System.Text;

namespace LuaEditor;

public partial class MainForm : Form
{
    //  State 

    private string?       _currentFilePath;
    private bool          _isDirty;
    private bool          _suppressDirty;
    private bool          _fileWasEncrypted;

    private LuaProcessor _processor = null!;

    //  Colours 

    private static readonly Color ColourOk      = Color.FromArgb(0,  140, 0);
    private static readonly Color ColourDirty   = Color.FromArgb(200, 120, 0);
    private static readonly Color ColourError   = Color.FromArgb(180, 0,   0);
    private static readonly Color ColourNeutral = Color.FromArgb(100, 100, 100);

    //  Construction 

    public MainForm()
    {
        InitializeComponent();
        _processor = new LuaProcessor();
        UpdateWindowTitle();
    }

    //  Menu handlers 

    private void OnMenuOpen(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;

        using var dlg = new OpenFileDialog
        {
            Title  = "Open Lua File",
            Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
        };

        if (dlg.ShowDialog() == DialogResult.OK)
            OpenFile(dlg.FileName);
    }

    private void OnMenuSave(object? sender, EventArgs e)    => SaveFile(_currentFilePath!);
    private void OnMenuSaveAs(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title      = "Save Lua File",
            Filter     = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
            FileName   = Path.GetFileName(_currentFilePath ?? ""),
            DefaultExt = "lua",
        };
        if (dlg.ShowDialog() == DialogResult.OK)
            SaveFile(dlg.FileName);
    }

    private void OnMenuOpenLog(object? sender, EventArgs e)
    {
        if (!File.Exists(Logger.FilePath))
        {
            SetStatus("No log file yet.", ColourNeutral);
            return;
        }
        System.Diagnostics.Process.Start("notepad.exe", Logger.FilePath);
    }

    //  Open 

    private void OpenFile(string path)
    {
        SetStatus("Opening…", ColourNeutral);
        UseWaitCursor = true;
        try
        {
            byte[] raw = File.ReadAllBytes(path);
            byte[] bytecode;
            bool wasEncrypted;

            if (LuaCryptor.HasElswordMagic(raw))
            {
                bytecode     = raw;
                wasEncrypted = false;
            }
            else
            {
                byte[] decrypted = LuaCryptor.Crypt(raw);
                if (LuaCryptor.HasElswordMagic(decrypted))
                {
                    bytecode     = decrypted;
                    wasEncrypted = true;
                }
                else
                {
                    // Not bytecode - open as plain text
                    LoadPlainText(path, File.ReadAllText(path, Encoding.UTF8));
                    return;
                }
            }

            if (!_processor.IsReady(out string missingTools))
            {
                Logger.Write("Open", $"Tools missing for {path}:\n{missingTools}");
                SetStatus("Tools missing - see LuaEditor.log", ColourError);
                return;
            }

            string source = _processor.Decompile(bytecode);
            LoadSource(path, source, wasEncrypted);
        }
        catch (Exception ex)
        {
            Logger.Write($"Open: {Path.GetFileName(path)}", ex);
            SetStatus($"Failed to open - see LuaEditor.log", ColourError);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void LoadSource(string path, string source, bool wasEncrypted)
    {
        _suppressDirty    = true;
        _currentFilePath  = path;
        _fileWasEncrypted = wasEncrypted;
        editor.ReadOnly   = false;
        editor.Text       = source.Replace("\r\n", "\n").Replace("\r", "\n");
        editor.SelectionStart = 0;
        _isDirty       = false;
        _suppressDirty = false;

        lblFilePath.Text = path;
        SetState(wasEncrypted ? "Decompiled (encrypted)" : "Decompiled (raw)", ColourOk);
        SetStatus($"Opened: {Path.GetFileName(path)}", ColourOk);
        UpdateMenuState();
        UpdateWindowTitle();
        UpdateCaretLabel();
    }

    private void LoadPlainText(string path, string text)
    {
        _suppressDirty   = true;
        _currentFilePath = path;
        editor.ReadOnly  = false;
        editor.Text      = text;
        editor.SelectionStart = 0;
        _isDirty       = false;
        _suppressDirty = false;

        lblFilePath.Text = path;
        SetState("Plain Text", ColourNeutral);
        SetStatus($"Opened (plain text): {Path.GetFileName(path)}", ColourNeutral);
        UpdateMenuState();
        UpdateWindowTitle();
    }

    //  Save 

    private void SaveFile(string path)
    {
        if (!_processor.IsReady(out string missing))
        {
            Logger.Write("Save", $"Tools missing:\n{missing}");
            SetStatus("Tools missing - see LuaEditor.log", ColourError);
            return;
        }

        SetStatus("Compiling…", ColourNeutral);
        UseWaitCursor = true;
        try
        {
            byte[] bytecode = _processor.Compile(editor.Text);

            if (!LuaCryptor.HasElswordMagic(bytecode))
                throw new Exception(
                    "Compiler did not produce Elsword bytecode (\\x1bKL\\x81 magic missing).\n" +
                    "Ensure LuaCompileTool/luac.exe is the Elsword-modified build.");

            byte[] output = _fileWasEncrypted ? LuaCryptor.Crypt(bytecode) : bytecode;
            File.WriteAllBytes(path, output);

            _currentFilePath = path;
            _isDirty         = false;

            lblFilePath.Text = path;
            SetState(_fileWasEncrypted ? "Saved (encrypted)" : "Saved (raw)", ColourOk);
            SetStatus($"Saved: {Path.GetFileName(path)}", ColourOk);
            UpdateMenuState();
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            Logger.Write($"Save: {Path.GetFileName(path)}", ex);
            SetStatus("Save failed - see LuaEditor.log", ColourError);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    //  Editor events 

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_suppressDirty) return;
        if (!_isDirty)
        {
            _isDirty = true;
            SetState("Modified", ColourDirty);
            UpdateWindowTitle();
            UpdateMenuState();
        }
    }

    private void OnEditorSelectionChanged(object? sender, EventArgs e)
        => UpdateCaretLabel();

    //  UI helpers 

    private void SetStatus(string text, Color colour)
    {
        statusMsg.Text      = text;
        statusMsg.ForeColor = colour;
    }

    private void SetState(string text, Color colour)
    {
        lblState.Text      = text;
        lblState.ForeColor = colour;
    }

    private void UpdateWindowTitle()
    {
        string name = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "No file";
        Text = _isDirty ? $"*{name} - Elsword Lua Editor" : $"{name} - Elsword Lua Editor";
    }

    private void UpdateMenuState()
    {
        bool hasFile = _currentFilePath != null;
        menuSave.Enabled   = hasFile && _isDirty;
        menuSaveAs.Enabled = hasFile;
    }

    private void UpdateCaretLabel()
    {
        int idx  = editor.SelectionStart;
        int line = editor.GetLineFromCharIndex(idx) + 1;
        int col  = idx - editor.GetFirstCharIndexOfCurrentLine() + 1;
        statusCaret.Text = $"Ln {line}, Col {col}";
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_isDirty) return true;
        return MessageBox.Show(
            "You have unsaved changes. Discard them?",
            "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            == DialogResult.Yes;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmDiscardChanges()) e.Cancel = true;
        base.OnFormClosing(e);
    }

}
