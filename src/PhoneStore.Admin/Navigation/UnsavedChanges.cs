using System.Runtime.CompilerServices;

namespace PhoneStore.Admin;

public enum LeaveChoice { Stay, Discard, Save }

public static class UnsavedChanges
{
    private sealed class Guard(Func<string> snapshot, Func<bool> busy, Func<Task>? save, Action? discard)
    {
        public readonly Func<string> Snapshot = snapshot;
        public readonly Func<bool> Busy = busy;
        public readonly Func<Task>? Save = save;
        public readonly Action? Discard = discard;
        public string Baseline = snapshot();
        public bool Bypass, Closing;
    }
    private static readonly ConditionalWeakTable<Form, Guard> guards = new();
    public static void Attach(Form form, Func<string> snapshot, Func<bool> busy, Func<Task>? save, Action? discard = null)
    {
        var guard = new Guard(snapshot, busy, save, discard); guards.Add(form, guard);
        form.FormClosing += async (_, args) =>
        {
            if (guard.Bypass) return;
            if (!guard.Busy() && !IsDirty(form)) return;
            args.Cancel = true;
            if (guard.Closing) return;
            guard.Closing = true;
            try
            {
                if (await TryLeaveAsync(form) && !form.IsDisposed)
                { guard.Bypass = true; form.BeginInvoke(() => { if (!form.IsDisposed) form.Close(); }); }
            }
            finally { guard.Closing = false; }
        };
    }
    public static bool IsDirty(Form form) => guards.TryGetValue(form, out var guard) && guard.Baseline != guard.Snapshot();
    public static void MarkClean(Form form)
    { if (guards.TryGetValue(form, out var guard)) guard.Baseline = guard.Snapshot(); }
    public static async Task<bool> TryLeaveAsync(Form form, Func<bool, LeaveChoice>? choose = null)
    {
        if (!guards.TryGetValue(form, out var guard)) return true;
        foreach (var grid in Descendants(form).OfType<DataGridView>()) if (!grid.EndEdit()) return false;
        if (guard.Busy()) return false;
        if (!IsDirty(form)) return true;
        var choice = choose?.Invoke(guard.Save is not null) ?? Ask(form, guard.Save is not null);
        if (choice == LeaveChoice.Stay) return false;
        if (choice == LeaveChoice.Discard) { guard.Discard?.Invoke(); return true; }
        if (guard.Save is null) return false;
        await guard.Save();
        return !form.IsDisposed && !guard.Busy() && !IsDirty(form);
    }
    // Session invalidation must not offer a write under the old identity. Fixtures also use this for teardown.
    public static void DiscardAndClose(Form form)
    { if (guards.TryGetValue(form, out var guard)) guard.Bypass = true; form.Close(); }
    private static LeaveChoice Ask(Form owner, bool canSave)
    {
        using var dialog = new Form(); AdminTheme.Frame(dialog, "Thay đổi chưa lưu", new(660, 310));
        dialog.MaximizeBox = dialog.MinimizeBox = false;
        var choice = LeaveChoice.Stay;
        var body = AdminTheme.Stack(20);
        AdminTheme.Add(body, AdminTheme.Label("Bạn có thay đổi chưa lưu.", 16, true));
        AdminTheme.Add(body, AdminTheme.Label(canSave ? "Lưu trước khi rời, bỏ thay đổi hoặc ở lại để tiếp tục." : "Bỏ nội dung đang nhập hoặc ở lại để chọn và xác nhận thao tác."));
        var stay = AdminTheme.Button("Ở lại", false); stay.DialogResult = DialogResult.Cancel;
        var discard = AdminTheme.Button("Bỏ thay đổi", false);
        discard.Click += (_, _) => { choice = LeaveChoice.Discard; dialog.DialogResult = DialogResult.OK; };
        var buttons = new List<Control>();
        if (canSave)
        {
            var save = AdminTheme.Button("Lưu");
            save.Click += (_, _) => { choice = LeaveChoice.Save; dialog.DialogResult = DialogResult.OK; };
            buttons.Add(save);
        }
        buttons.Add(discard); buttons.Add(stay); AdminTheme.Add(body, CatalogUi.Toolbar(buttons.ToArray()));
        dialog.Controls.Add(body); dialog.AcceptButton = stay; dialog.CancelButton = stay; dialog.ActiveControl = stay;
        dialog.ShowDialog(PageNavigation.Owner(owner)); return choice;
    }
    internal static IEnumerable<Control> Descendants(Control parent)
    { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
}
