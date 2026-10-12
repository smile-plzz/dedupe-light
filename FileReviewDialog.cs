using System.ComponentModel;
using System.Diagnostics;

namespace DedupeLight;

sealed class FileReviewDialog : Form
{
    readonly DuplicateItem item;
    readonly Panel content = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 245, 247) };
    readonly Label note = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Loading preview…" };
    PictureBox? picture;
    VideoHost? video;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool? ExcludeFolder { get; private set; }

    public FileReviewDialog(DuplicateItem item)
    {
        this.item = item;
        Text = "Review · " + item.Name; Size = new Size(900, 680); MinimumSize = new Size(760, 540);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10); BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        var details = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
            Text = $"{item.Name}\r\n{item.Path}\r\n{item.SizeText}  ·  Modified {item.ModifiedText}  ·  Set {item.Group}\r\nSHA-256: {item.Hash}" };
        layout.Controls.Add(details, 0, 0); content.Controls.Add(note); layout.Controls.Add(content, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0) };
        Add(buttons, "Open in default app", () => Launch(item.Path));
        Add(buttons, "Show in Explorer", () => { try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.Path}\"") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } });
        Add(buttons, "Exclude this file", () => { ExcludeFolder = false; DialogResult = DialogResult.OK; Close(); });
        Add(buttons, "Exclude folder", () => { ExcludeFolder = true; DialogResult = DialogResult.OK; Close(); });
        var close = new SoftButton { Text = "Close", DialogResult = DialogResult.Cancel }; buttons.Controls.Add(close); CancelButton = close;
        layout.Controls.Add(buttons, 0, 2); Controls.Add(layout); Shown += async (_, _) => await LoadPreviewAsync();
    }
    static void Add(Control parent, string title, Action click) { var b = new SoftButton { Text = title }; b.Click += (_, _) => click(); parent.Controls.Add(b); }
    void Launch(string path) { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open file"); } }

    async Task LoadPreviewAsync()
    {
        var extension = Path.GetExtension(item.Path).ToLowerInvariant();
        try
        {
            if (new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff" }.Contains(extension))
            {
                var image = await Task.Run(() =>
                {
                    if (new FileInfo(item.Path).Length > 32 * 1024 * 1024) throw new IOException("This image is too large for the built-in preview.");
                    using var stream = File.OpenRead(item.Path); using var original = Image.FromStream(stream);
                    if ((long)original.Width * original.Height > 40_000_000) throw new IOException("This image is too large for the built-in preview.");
                    var scale = Math.Min(1d, Math.Min(1200d / original.Width, 800d / original.Height));
                    var thumbnail = new Bitmap(Math.Max(1, (int)(original.Width * scale)), Math.Max(1, (int)(original.Height * scale)));
                    using var g = Graphics.FromImage(thumbnail); g.DrawImage(original, 0, 0, thumbnail.Width, thumbnail.Height); return thumbnail;
                });
                if (IsDisposed) { image.Dispose(); return; }
                picture = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Image = image };
                content.Controls.Add(picture); picture.BringToFront(); note.Visible = false;
            }
            else if (new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".webm", ".mp3", ".wav", ".m4a" }.Contains(extension))
            {
                note.Text = "Playback depends on Windows Media Player and installed codecs.\nUse Open in default app if this format cannot play.";
                video = new VideoHost(); ((ISupportInitialize)video).BeginInit(); content.Controls.Add(video); ((ISupportInitialize)video).EndInit();
                video.CreateControl(); video.LoadFile(item.Path); video.BringToFront();
            }
            else note.Text = "A built-in preview is not available for this file type.\nUse Open in default app to review it.";
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            if (video is not null) { content.Controls.Remove(video); video.Dispose(); video = null; }
            note.Visible = true; note.Text = "Preview unavailable. Use Open in default app.\n\n" + ex.Message;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { video?.StopPlayback(); picture?.Image?.Dispose(); }
        base.Dispose(disposing);
    }

    sealed class VideoHost : AxHost
    {
        public VideoHost() : base("6BF52A52-394A-11D3-B153-00C04F79FAA6") { Dock = DockStyle.Fill; }
        public void LoadFile(string path)
        {
            dynamic player = GetOcx();
            player.settings.autoStart = false;
            player.uiMode = "full"; player.stretchToFit = true; player.URL = path;
        }
        public void StopPlayback() { try { dynamic player = GetOcx(); player.controls.stop(); player.close(); } catch { } }
    }
}
