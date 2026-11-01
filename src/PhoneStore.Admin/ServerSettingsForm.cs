using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class ServerSettingsForm : Form
{
    public TextBox AddressBox { get; } = AdminTheme.Input("Địa chỉ máy chủ");
    public TextBox EnvironmentBox { get; } = AdminTheme.Input("Môi trường kỳ vọng");
    public TextBox FingerprintBox { get; } = AdminTheme.Input("Dấu nhận dạng kỳ vọng");
    public Label MessageLabel { get; } = AdminTheme.Label("");
    public Button SaveButton { get; } = AdminTheme.Button("&Lưu cấu hình");
    public ServerSettings? Result { get; private set; }
    public ServerSettingsForm(ServerSettings settings)
    {
        AdminTheme.Frame(this, "Cấu hình máy chủ", new Size(720, 620)); MaximizeBox = false;
        var body = AdminTheme.Stack(32); Controls.Add(body);
        AdminTheme.Add(body, AdminTheme.Label("Kết nối máy chủ", 20, true));
        AdminTheme.Field(body, "&Địa chỉ máy chủ", AddressBox); AddressBox.Text = settings.BaseUrl;
        AdminTheme.Field(body, "&Môi trường kỳ vọng (tùy chọn)", EnvironmentBox); EnvironmentBox.Text = settings.ExpectedEnvironment;
        AdminTheme.Field(body, "&Dấu nhận dạng kỳ vọng (tùy chọn)", FingerprintBox); FingerprintBox.Text = settings.ExpectedFingerprint;
        AdminTheme.Add(body, AdminTheme.Label("Máy khác phải dùng HTTPS. Không lưu thông tin đăng nhập."));
        MessageLabel.MaximumSize = new Size(580, 0); AdminTheme.Add(body, MessageLabel);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var cancel = AdminTheme.Button("&Hủy", false); cancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(SaveButton); actions.Controls.Add(cancel); AdminTheme.Add(body, actions);
        AcceptButton = SaveButton; CancelButton = cancel;
        SaveButton.Click += (_, _) =>
        {
            try { ServerAddress.Validate(AddressBox.Text.Trim()); Result = new(AddressBox.Text.Trim(), EnvironmentBox.Text.Trim(), FingerprintBox.Text.Trim()); DialogResult = DialogResult.OK; }
            catch (AdminApiException error) { MessageLabel.Text = error.Message; }
        };
    }
}
