using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class PasswordForm : Form
{
    private readonly IAdminApi api;
    private bool busy;
    public TextBox CurrentBox { get; } = AdminTheme.Input("Mật khẩu hiện tại", true);
    public TextBox NewBox { get; } = AdminTheme.Input("Mật khẩu mới", true);
    public Button SaveButton { get; } = AdminTheme.Button("&Đổi mật khẩu");
    public Label MessageLabel { get; } = AdminTheme.Label("");
    public Label ProgressLabel { get; } = AdminTheme.Label("Đang cập nhật mật khẩu…", 10, true);
    public PasswordForm(IAdminApi api)
    {
        this.api = api; AdminTheme.Frame(this, "Đổi mật khẩu", new Size(660, 500)); MaximizeBox = false;
        var body = AdminTheme.Stack(32); Controls.Add(body); AdminTheme.Add(body, AdminTheme.Label("Đổi mật khẩu của tôi", 20, true));
        AdminTheme.Add(body, AdminTheme.Label("Thành công sẽ thu hồi mọi phiên. Bạn cần đăng nhập lại."));
        AdminTheme.Field(body, "&Mật khẩu hiện tại", CurrentBox); AdminTheme.Field(body, "Mật khẩu &mới", NewBox);
        AdminTheme.Add(body, AdminTheme.Label("10–128 ký tự, có chữ và số.")); ProgressLabel.Visible = false; ProgressLabel.AccessibleRole = AccessibleRole.StatusBar; AdminTheme.Add(body, ProgressLabel); MessageLabel.MaximumSize = new Size(570, 0); AdminTheme.Add(body, MessageLabel);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; actions.Controls.Add(SaveButton);
        var cancel = AdminTheme.Button("&Hủy", false); cancel.DialogResult = DialogResult.Cancel; actions.Controls.Add(cancel); AdminTheme.Add(body, actions);
        AcceptButton = SaveButton; CancelButton = cancel; SaveButton.Click += async (_, _) => await SubmitAsync();
        FormClosed += (_, _) => { CurrentBox.Clear(); NewBox.Clear(); };
    }
    public async Task SubmitAsync()
    {
        if (busy) return; var current = CurrentBox.Text; var next = NewBox.Text; CurrentBox.Clear(); NewBox.Clear();
        busy = true; SaveButton.Enabled = false; SaveButton.Text = "Đang cập nhật…"; ProgressLabel.Visible = true;
        try { await api.ChangePasswordAsync(current, next); if (!IsDisposed) DialogResult = DialogResult.OK; }
        catch (Exception error) { if (!IsDisposed) MessageLabel.Text = AdminTheme.Failure(error); }
        finally { current = next = ""; if (!IsDisposed) { busy = false; SaveButton.Enabled = true; SaveButton.Text = "&Đổi mật khẩu"; ProgressLabel.Visible = false; } }
    }
}
