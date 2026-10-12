namespace DedupeLight;

sealed class RecycleOverrideDialog : Form
{
    public RecycleOverrideDialog(IReadOnlyList<int> groups, IReadOnlyList<DuplicateItem> chosen)
    {
        Text = "Recycle every listed copy?"; Size = new Size(620, 440); MinimumSize = new Size(560, 400);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10); BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = $"Your selection includes every listed copy in {groups.Count} set(s).\nThis override applies only to this operation. Files go to the Recycle Bin.\nOther copies outside this scan are not verified." }, 0, 0);
        layout.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
            Text = string.Join(Environment.NewLine, chosen.Where(x => groups.Contains(x.Group)).Select(x => $"Set {x.Group}: {x.Path}")) }, 0, 1);
        layout.Controls.Add(new Label { Text = "Type RECYCLE to allow this operation.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        var entry = new TextBox { Dock = DockStyle.Fill }; layout.Controls.Add(entry, 0, 3);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
        var cancel = new SoftButton { Text = "Keep files", DialogResult = DialogResult.Cancel };
        var confirm = new SoftButton { Text = "Recycle all selected", Destructive = true, Enabled = false };
        confirm.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        entry.TextChanged += (_, _) => confirm.Enabled = entry.Text.Trim().Equals("RECYCLE", StringComparison.Ordinal);
        buttons.Controls.AddRange([cancel, confirm]); layout.Controls.Add(buttons, 0, 4); Controls.Add(layout); CancelButton = cancel;
    }
}
