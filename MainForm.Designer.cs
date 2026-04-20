namespace LuaEditor;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Controls
    private MenuStrip     menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem menuOpen;
    private ToolStripMenuItem menuSave;
    private ToolStripMenuItem menuSaveAs;
    private ToolStripSeparator menuSep1;
    private ToolStripMenuItem menuOpenLog;
    private ToolStripSeparator menuSep2;
    private ToolStripMenuItem menuExit;

    private Panel      infoPanel;
    private Label      lblFilePath;
    private Label      lblState;

    private RichTextBox editor;

    private StatusStrip  statusStrip;
    private ToolStripStatusLabel statusMsg;
    private ToolStripStatusLabel statusCaret;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        //  Menu 
        menuStrip = new MenuStrip { BackColor = Color.White };

        fileMenu  = new ToolStripMenuItem("File");
        menuOpen  = new ToolStripMenuItem("Open Encrypted Lua…", null, OnMenuOpen)  { ShortcutKeys = Keys.Control | Keys.O };
        menuSave  = new ToolStripMenuItem("Save",                null, OnMenuSave)  { ShortcutKeys = Keys.Control | Keys.S, Enabled = false };
        menuSaveAs= new ToolStripMenuItem("Save As…",            null, OnMenuSaveAs){ ShortcutKeys = Keys.Control | Keys.Shift | Keys.S, Enabled = false };
        menuSep1    = new ToolStripSeparator();
        menuOpenLog = new ToolStripMenuItem("Open Log File", null, OnMenuOpenLog);
        menuSep2    = new ToolStripSeparator();
        menuExit    = new ToolStripMenuItem("Exit", null, (s, e) => Close());

        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            { menuOpen, menuSave, menuSaveAs, menuSep1, menuOpenLog, menuSep2, menuExit });
        menuStrip.Items.Add(fileMenu);

        //  Info panel 
        infoPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = Color.FromArgb(240, 240, 240),
            Padding   = new Padding(8, 0, 8, 0),
        };

        lblFilePath = new Label
        {
            AutoSize  = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(60, 60, 60),
            Font      = new Font("Segoe UI", 9f),
            Text      = "No file open",
        };

        lblState = new Label
        {
            AutoSize  = false,
            Width     = 110,
            TextAlign = ContentAlignment.MiddleRight,
            Dock      = DockStyle.Right,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Text      = "",
        };

        infoPanel.Controls.Add(lblFilePath);
        infoPanel.Controls.Add(lblState);

        // Separator line below info panel
        var separator = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = Color.FromArgb(220, 220, 220),
        };

        //  Editor 
        editor = new RichTextBox
        {
            Dock          = DockStyle.Fill,
            BackColor     = Color.White,
            ForeColor     = Color.FromArgb(30, 30, 30),
            Font          = new Font("Consolas", 10f),
            WordWrap      = false,
            ScrollBars    = RichTextBoxScrollBars.Both,
            AcceptsTab    = true,
            DetectUrls    = false,
            MaxLength     = 0,
            BorderStyle   = BorderStyle.None,
            Padding       = new Padding(4),
            ReadOnly      = true,
        };
        editor.TextChanged += OnEditorTextChanged;
        editor.SelectionChanged += OnEditorSelectionChanged;

        //  Status bar 
        statusStrip = new StatusStrip
        {
            BackColor  = Color.FromArgb(240, 240, 240),
            SizingGrip = false,
        };

        statusMsg = new ToolStripStatusLabel("Ready")
        {
            Spring    = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        statusCaret = new ToolStripStatusLabel("Ln 1, Col 1")
        {
            AutoSize  = false,
            Width     = 110,
            TextAlign = ContentAlignment.MiddleRight,
        };

        statusStrip.Items.AddRange(new ToolStripItem[] { statusMsg, statusCaret });

        //  Form 
        SuspendLayout();

        Controls.Add(editor);
        Controls.Add(separator);
        Controls.Add(infoPanel);
        Controls.Add(menuStrip);
        Controls.Add(statusStrip);

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor     = Color.White;
        ClientSize    = new Size(960, 680);
        Font          = new Font("Segoe UI", 9f);
        MainMenuStrip = menuStrip;
        MinimumSize   = new Size(600, 400);
        Text          = "Elsword Lua Editor";

        ResumeLayout(false);
        PerformLayout();
    }
}
