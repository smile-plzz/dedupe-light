using System.Drawing.Drawing2D;

namespace DedupeLight;

// Keep native keyboard, accessibility and click behavior while painting a softer surface.
sealed class SoftButton : Button
{
    readonly System.Windows.Forms.Timer animation = new() { Interval = 16 };
    float hover;
    bool pointerInside;
    [System.ComponentModel.DefaultValue(false)]
    public bool Primary { get; init; }
    [System.ComponentModel.DefaultValue(false)]
    public bool Destructive { get; init; }

    public SoftButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        AutoSize = true;
        MinimumSize = new Size(64, 36);
        Padding = new Padding(14, 0, 14, 0);
        Margin = new Padding(0, 0, 8, 0);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        animation.Tick += (_, _) =>
        {
            var target = pointerInside && Enabled ? 1f : 0f;
            hover += (target - hover) * .28f;
            if (Math.Abs(target - hover) < .02f) { hover = target; animation.Stop(); }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); pointerInside = true; animation.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pointerInside = false; animation.Start(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? Color.White);
        var normal = Primary ? Color.FromArgb(0, 113, 227) : Destructive ? Color.FromArgb(255, 240, 240) : Color.FromArgb(241, 242, 245);
        var over = Primary ? Color.FromArgb(0, 94, 195) : Destructive ? Color.FromArgb(255, 224, 224) : Color.FromArgb(226, 229, 234);
        if (!Enabled) normal = over = Color.FromArgb(245, 245, 247);
        var fill = Color.FromArgb((int)(normal.R + (over.R - normal.R) * hover), (int)(normal.G + (over.G - normal.G) * hover), (int)(normal.B + (over.B - normal.B) * hover));
        using var path = Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 9);
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);
        var ink = !Enabled ? Color.FromArgb(155, 158, 165) : Primary ? Color.White : Destructive ? Color.FromArgb(190, 44, 44) : Color.FromArgb(36, 38, 42);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Color.FromArgb(0, 113, 227), 1.5f);
            using var focus = Rounded(new RectangleF(3, 3, Width - 7, Height - 7), 7);
            e.Graphics.DrawPath(pen, focus);
        }
    }

    internal static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); var d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
        return p;
    }
    protected override void Dispose(bool disposing) { if (disposing) animation.Dispose(); base.Dispose(disposing); }
}

sealed class SmoothGrid : DataGridView
{
    public SmoothGrid() { DoubleBuffered = true; }
}

sealed class SurfaceCard : Panel
{
    public SurfaceCard() { DoubleBuffered = true; BackColor = Color.White; Padding = new Padding(18); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var outline = SoftButton.Rounded(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 12);
        using var pen = new Pen(Color.FromArgb(229, 231, 235));
        e.Graphics.DrawPath(pen, outline);
    }
}
