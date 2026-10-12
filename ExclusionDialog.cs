using System.Text.Json;

namespace DedupeLight;

sealed class ExclusionDialog : Form
{
    readonly ListBox list = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, SelectionMode = SelectionMode.MultiExtended };
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public List<ExclusionRule> Rules { get; private set; }
    public ExclusionDialog(IEnumerable<ExclusionRule> rules)
    {
        Rules = ExclusionRules.Validate(rules);
        Text = "Excluded sources"; Size = new Size(800, 480); MinimumSize = new Size(680, 400);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10); BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Intentional duplicates stay yours.\nExcluded files and folders are skipped in current results and future scans.", AutoSize = false }, 0, 0);
        layout.Controls.Add(list, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), WrapContents = true };
        Add(buttons, "Add folder", () => { using var d = new FolderBrowserDialog(); if (d.ShowDialog(this) == DialogResult.OK) { Rules.Add(new(d.SelectedPath, true)); RefreshRules(); } });
        Add(buttons, "Add files", () => { using var d = new OpenFileDialog { Multiselect = true }; if (d.ShowDialog(this) == DialogResult.OK) { Rules.AddRange(d.FileNames.Select(p => new ExclusionRule(p, false))); RefreshRules(); } });
        Add(buttons, "Remove selected", () => { var selected = list.SelectedItems.Cast<ExclusionRule>().ToHashSet(); Rules.RemoveAll(selected.Contains); RefreshRules(); });
        Add(buttons, "Import", Import);
        Add(buttons, "Export", Export);
        Add(buttons, "Apply", () => { DialogResult = DialogResult.OK; Close(); });
        var cancel = new SoftButton { Text = "Cancel", DialogResult = DialogResult.Cancel }; buttons.Controls.Add(cancel); CancelButton = cancel;
        layout.Controls.Add(buttons, 0, 2); Controls.Add(layout); RefreshRules();
    }
    static void Add(Control parent, string text, Action action) { var b = new SoftButton { Text = text }; b.Click += (_, _) => action(); parent.Controls.Add(b); }
    void RefreshRules() { Rules = ExclusionRules.Validate(Rules); list.DataSource = null; list.DataSource = Rules.ToArray(); }
    void Import()
    {
        using var d = new OpenFileDialog { Filter = "Exclusions (*.json)|*.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            if (new FileInfo(d.FileName).Length > 1024 * 1024) throw new IOException("Exclusion files must be smaller than 1 MB.");
            var imported = JsonSerializer.Deserialize<List<ExclusionRule>>(File.ReadAllText(d.FileName)) ?? throw new IOException("The file contains no rules.");
            var validated = ExclusionRules.Validate(imported); Rules.AddRange(validated); RefreshRules();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not import exclusions"); }
    }
    void Export()
    {
        using var d = new SaveFileDialog { Filter = "Exclusions (*.json)|*.json", FileName = "dedupelight-exclusions.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(d.FileName, JsonSerializer.Serialize(Rules, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not export exclusions"); }
    }
}
