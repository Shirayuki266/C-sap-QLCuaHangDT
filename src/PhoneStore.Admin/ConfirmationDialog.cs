namespace PhoneStore.Admin;

// Shared presentation only; future modules must invoke this before their service commands.
public sealed class ConfirmationDialog : Form
{
    public Button ConfirmButton { get; } = AdminTheme.Button("&Xác nhận");
    public Button DismissButton { get; } = AdminTheme.Button("&Quay lại", false);
    public ConfirmationDialog(string action, string description)
    {
        AdminTheme.Frame(this, action, new Size(620, 330)); MaximizeBox = false;
        var body = AdminTheme.Stack(28); Controls.Add(body); AdminTheme.Add(body, AdminTheme.Label(action, 18, true));
        var detail = AdminTheme.Label(description); detail.MaximumSize = new Size(530, 0); AdminTheme.Add(body, detail);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; buttons.Controls.Add(DismissButton); buttons.Controls.Add(ConfirmButton); AdminTheme.Add(body, buttons);
        ConfirmButton.DialogResult = DialogResult.OK; DismissButton.DialogResult = DialogResult.Cancel;
        AcceptButton = ConfirmButton; CancelButton = DismissButton; ActiveControl = DismissButton;
    }
}
