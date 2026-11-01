namespace PhoneStore.Admin;

internal static class AdminTheme
{
    public static readonly Color Ink = Color.FromArgb(25, 38, 61), Navy = Color.FromArgb(20, 45, 92), Blue = Color.FromArgb(36, 84, 220), Canvas = Color.FromArgb(244, 246, 251), Muted = Color.FromArgb(82, 100, 127);
    public static void Frame(Form form, string title, Size? size = null)
    {
        form.Text = title + " · Monophone Admin"; form.Font = new Font("Segoe UI", 10);
        form.AutoScaleMode = AutoScaleMode.Dpi; form.BackColor = Canvas; form.ForeColor = Ink;
        form.Size = size ?? new Size(1280, 800); form.MinimumSize = size ?? new Size(1280, 800);
        form.StartPosition = FormStartPosition.CenterScreen;
    }
    public static Label Label(string text, float size = 10, bool bold = false) => new()
    { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = Ink, Margin = new Padding(0, 8, 0, 8) };
    public static TextBox Input(string name, bool password = false) => new()
    { Name = name, AccessibleName = name, UseSystemPasswordChar = password, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), MaxLength = password ? 128 : 200, Font = new Font("Segoe UI", 12) };
    public static Button Button(string text, bool primary = true) => new()
    { Text = text, AutoSize = true, MinimumSize = new Size(120, 42), Padding = new Padding(12, 5, 12, 5), FlatStyle = FlatStyle.Flat, BackColor = primary ? Blue : Color.White, ForeColor = primary ? Color.White : Ink, Cursor = Cursors.Hand, Margin = new Padding(0, 8, 8, 0) };
    public static TableLayoutPanel Stack(int padding = 24) => new() { Dock = DockStyle.Fill, AutoSize = true, AutoScroll = true, ColumnCount = 1, Padding = new Padding(padding), BackColor = Color.White };
    public static void Add(TableLayoutPanel panel, Control control) { panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.Controls.Add(control, 0, panel.RowCount - 1); }
    public static void Field(TableLayoutPanel panel, string label, TextBox input) { Add(panel, Label(label, 10, true)); Add(panel, input); }
    public static string Failure(Exception error) => error is Client.AdminApiException known ? known.Message : "Chưa thể hoàn tất thao tác. Vui lòng thử lại sau.";
}
