using System.Diagnostics;
using System.Text.Json;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetGuiResources(IntPtr process, uint kind);
    private static async Task WaitNavigation(MainForm main)
    {
        for (var i = 0; i < 200 && main.Navigator.IsBusy; i++) await Task.Delay(25);
        Assert(!main.Navigator.IsBusy, "Navigation did not finish");
    }
    private sealed class NavigationFixturePage : NavigationPageForm
    {
        public DataGridView Grid { get; } = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false };
        public int Reads;
        public NavigationFixturePage(string title)
        {
            Text = title; Grid.Columns.Add("Row", "Dòng QA"); Grid.Columns[0].Width = 1800; Controls.Add(Grid);
            ConfigurePageLoad(() => { Reads++; Grid.Rows.Clear(); for (var i = 0; i < 80; i++) Grid.Rows.Add("Dòng " + i); return Task.CompletedTask; });
        }
    }
    private sealed class DeferredNavigationPage : NavigationPageForm
    {
        public DeferredNavigationPage(Task pending)
        {
            Text = "Đọc dữ liệu phiên cũ"; var label = new Label(); Controls.Add(label);
            ConfigurePageLoad(async () => { await pending; label.Text = "Kết quả đến muộn"; });
        }
    }
    private static async Task NavigationFormsChecks()
    {
        var old = output; output = Path.Combine(FindRoot(), ".artifacts", "admin-w6", "forms", "navigation"); Directory.CreateDirectory(output);
        var start = passed; var captureOffset = captures.Count;
        await Test("NAV.UI.01/02/03/04 STATE.01/06 embedded pages reload, retain filters/page/scroll, branch/home dispose", async () =>
        {
            using var api = new OrdersFake(); using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            var list = new OrderListForm(api); await main.Navigator.OpenAsync(list);
            list.QueryBox.Text = "khách QA"; list.StateBox.SelectedIndex = 1; list.NextButton.PerformClick();
            await Task.Delay(30); Assert(list.MessageLabel.Text.StartsWith("Trang 2"), $"Fixture did not reach page2: {list.MessageLabel.Text}; rows={list.Grid.Rows.Count};next={list.NextButton.Enabled};visible={list.NextButton.Visible};init={list.Initialized};paths={string.Join("|",api.Paths)}");
            var detail = new OrderDetailForm(api, 33, (_, _) => true); await main.Navigator.OpenAsync(detail);
            Assert(!detail.TopLevel && detail.Parent == main.ContentHost && main.BackButton.Enabled && !main.ForwardButton.Enabled, "Page not embedded/button state wrong");
            await detail.ActAsync(OrderAction.Confirm); var reads = api.Paths.Count;
            await main.Navigator.BackAsync();
            Assert(ReferenceEquals(main.Navigator.CurrentPage, list) && list.QueryBox.Text == "khách QA" && list.StateBox.SelectedIndex == 1 && list.MessageLabel.Text.StartsWith("Trang 2") && api.Paths.Count > reads && ((AdminOrder)list.Grid.Rows[0].Tag!).Status == "DA_XAC_NHAN", "Back lost filters/page or retained old data");
            await main.Navigator.ForwardAsync(); Assert(detail.Current?.Status == "DA_XAC_NHAN", "Forward did not refresh detail");
            var crumb = Descendants(main).OfType<LinkLabel>().Single(c => c.AccessibleName == "Đi đến Đơn hàng");
            typeof(LinkLabel).GetMethod("OnLinkClicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(crumb, new object[] { new LinkLabelLinkClickedEventArgs(crumb.Links[0]) });
            await WaitNavigation(main); Assert(ReferenceEquals(main.Navigator.CurrentPage, list), "Breadcrumb did not return to its entry");
            var branch = new NavigationFixturePage("Nhánh mới"); await main.Navigator.OpenAsync(branch);
            Assert(detail.IsDisposed && !main.ForwardButton.Enabled, "Forward branch not disposed");
            branch.Grid.CurrentCell = branch.Grid.Rows[35].Cells[0]; branch.Grid.FirstDisplayedScrollingRowIndex = 30;
            var scroll = branch.Grid.FirstDisplayedScrollingRowIndex; branch.Grid.HorizontalScrollingOffset = 120;
            await main.Navigator.OpenAsync(new NavigationFixturePage("Chi tiết QA")); await main.Navigator.BackAsync();
            Assert(branch.Reads == 2 && branch.Grid.CurrentRow?.Index == 35 && branch.Grid.FirstDisplayedScrollingRowIndex == scroll && branch.Grid.HorizontalScrollingOffset == 120, "Back lost grid scroll/selection or skipped GET");
            await Capture(main, "navigation-back-forward-1280");
            await main.Navigator.HomeAsync(); Assert(list.IsDisposed && branch.IsDisposed && main.Navigator.History.Entries.Count == 1 && !main.BackButton.Enabled && !main.ForwardButton.Enabled, "Home retained history");
            using var auxiliary = new Form { Text = "Cửa sổ phụ QA" }; auxiliary.Show(main); main.CloseAuxiliaryButton.PerformClick();
            Assert(auxiliary.IsDisposed && !main.IsDisposed, "Close auxiliary command did not retain only shell");
            await Capture(main, "navigation-home-buttons-disabled");
            main.EndSession();
        });
        await Test("NAV.STATE.02 after Back stale version rejected and UI requests authoritative reload", async () =>
        {
            using var api = new OrdersFake(); using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            var detail = new OrderDetailForm(api, 33, (_, _) => true); await main.Navigator.OpenAsync(detail);
            await main.Navigator.OpenAsync(new NavigationFixturePage("Trang sau"));
            api.Row = api.Row with { Version = "AwAAAAAAAAA=" }; await main.Navigator.BackAsync();
            Assert(detail.Current?.Version == api.Row.Version, "Back used old version");
            var fresh = detail.Current!.Version; api.Conflict = true; await detail.ActAsync(OrderAction.Confirm);
            Assert(api.LastCommand?.Version == fresh && detail.Current?.Status == "DA_HUY" && detail.MessageLabel.Text.Contains("vui lòng tải lại") && !detail.ActionButtons[OrderAction.Confirm].Visible, "Stale write after Back not rejected/refreshed");
            await Capture(main, "navigation-back-version-conflict"); main.EndSession();
        });
        await Test("NAV.STATE.03 dirty stay/discard/save failure/save success and modal close/Esc", async () =>
        {
            using var api = new CatalogFake(); using var editor = new ProductEditor(api, 1, (_, _) => true); editor.Show(); await Task.Delay(60);
            Assert(!UnsavedChanges.IsDirty(editor), "Loaded editor is dirty"); editor.NameBox.Text += " sửa";
            Assert(!await UnsavedChanges.TryLeaveAsync(editor, _ => LeaveChoice.Stay) && api.Writes == 0, "Stay wrote or left");
            Assert(await UnsavedChanges.TryLeaveAsync(editor, _ => LeaveChoice.Discard) && api.Writes == 0, "Discard wrote");
            api.WriteError = new AdminApiException("INVALID_INPUT", 400);
            Assert(!await UnsavedChanges.TryLeaveAsync(editor, _ => LeaveChoice.Save) && UnsavedChanges.IsDirty(editor), "Failed save left editor");
            api.WriteError = null; Assert(await UnsavedChanges.TryLeaveAsync(editor, _ => LeaveChoice.Save), "Successful save did not leave");
            UnsavedChanges.DiscardAndClose(editor);
            using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            var lookup = new LookupForm(api, "brands"); await main.Navigator.OpenAsync(lookup); lookup.NameBox.Text += " chưa lưu";
            using var timer = new System.Windows.Forms.Timer { Interval = 40 };
            var prompts = 0;
            timer.Tick += (_, _) => { var dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text.StartsWith("Thay đổi chưa lưu")); if (dialog is null) return; prompts++; dialog.DialogResult = DialogResult.Cancel; };
            timer.Start(); await main.Navigator.HomeAsync(); timer.Stop();
            Assert(prompts == 1 && ReferenceEquals(main.Navigator.CurrentPage, lookup), "Home silently lost dirty lookup");
            UnsavedChanges.MarkClean(lookup); main.EndSession();
            using var supply = new SupplyFake(); using var purchase = new PurchaseEditor(supply); purchase.Show(); await Task.Delay(60);
            purchase.Lines.Add(new() { VariantId = 3, Sku = "W4-A", Quantity = 3, Cost = 8000000 });
            Assert(UnsavedChanges.IsDirty(purchase) && !await UnsavedChanges.TryLeaveAsync(purchase, _ => LeaveChoice.Stay) && supply.Writes == 0, "Purchase dirty guard failed");
            var closePrompts = 0;
            using var dismiss = new System.Windows.Forms.Timer { Interval = 40 };
            dismiss.Tick += (_, _) => { var dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text.StartsWith("Thay đổi chưa lưu")); if (dialog is null) return; closePrompts++; dialog.DialogResult = DialogResult.Cancel; };
            dismiss.Start(); purchase.Close(); await Task.Delay(60); dismiss.Stop();
            Assert(closePrompts == 1 && !purchase.IsDisposed && supply.Writes == 0, "Window close bypassed dirty Stay");
            UnsavedChanges.DiscardAndClose(purchase);
            using var modal = new PurchaseEditor(supply); var attempts = 0; var modalPrompts = 0; var retained = false;
            using var escape = new System.Windows.Forms.Timer { Interval = 60 };
            escape.Tick += (_, _) =>
            {
                var question = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Text.StartsWith("Thay đổi chưa lưu"));
                if (question is not null)
                {
                    modalPrompts++;
                    if (modalPrompts == 1) question.DialogResult = DialogResult.Cancel;
                    else Descendants(question).OfType<Button>().Single(b => b.Text == "Bỏ thay đổi").PerformClick();
                    return;
                }
                if (!modal.SaveButton.Enabled || attempts >= 2) return;
                if (attempts == 0) modal.Lines.Add(new() { VariantId = 3, Sku = "W4-A", Quantity = 4, Cost = 8000000 });
                else retained = modal.Lines.Count == 1 && !modal.IsDisposed;
                attempts++;
                typeof(Control).GetMethod("OnKeyDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(modal, new object[] { new KeyEventArgs(Keys.Escape) });
            };
            escape.Start(); modal.ShowDialog(); escape.Stop();
            Assert(modalPrompts == 2 && retained && supply.Writes == 0, "Esc lost modal edits or bypassed Stay/Discard");
        });
        await Test("NAV.FLOW.03 invoice stays embedded, preview returns to invoice, Back returns to order", async () =>
        {
            using var api = new OrdersFake { Row = OrdersFake.Order("HOAN_TAT") }; using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            var detail = new OrderDetailForm(api, 33); await main.Navigator.OpenAsync(detail); await detail.OpenInvoiceAsync();
            var invoice = main.Navigator.CurrentPage as InvoiceForm;
            Assert(invoice is { TopLevel: false, Current: not null }, "Invoice did not open inside shell");
            // Driver-independent contract; the explicit native preview run supplies driver evidence.
            var printer = new TestPrinter { Available = true }; var view = new InvoiceForm(api, 33, printer); await main.Navigator.OpenAsync(view);
            view.Preview(); Assert(printer.Calls == 1 && ReferenceEquals(main.Navigator.CurrentPage, view) && api.Writes == 0, "Preview changed page/data");
            await main.Navigator.BackAsync(); Assert(ReferenceEquals(main.Navigator.CurrentPage, invoice), "Preview Back skipped invoice");
            await main.Navigator.BackAsync(); Assert(ReferenceEquals(main.Navigator.CurrentPage, detail), "Invoice Back skipped order"); main.EndSession();
        });
        await Test("NAV.STATE.04 100 navigations bound live pages, top-level windows and process handles", async () =>
        {
            using var api = new OrdersFake(); using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            var pages = new List<NavigationFixturePage>(); var handles = new List<object>(); var at50 = 0; var userAt50 = 0; var gdiAt50 = 0;
            for (var i = 1; i <= 100; i++)
            {
                var page = new NavigationFixturePage("Trang QA " + i); pages.Add(page); await main.Navigator.OpenAsync(page);
                if (i is 50 or 100)
                {
                    using var process = Process.GetCurrentProcess(); process.Refresh(); var count = process.HandleCount;
                    var user = GetGuiResources(process.Handle, 1); var gdi = GetGuiResources(process.Handle, 0);
                    if (i == 50) { at50 = count; userAt50 = user; gdiAt50 = gdi; }
                    else Assert(count <= at50 + 100 && user <= userAt50 + 30 && gdi <= gdiAt50 + 30, "Handles grew after history reached capacity");
                    handles.Add(new { pages = i, processHandles = count, userHandles = user, gdiHandles = gdi, live = pages.Count(p => !p.IsDisposed) });
                }
            }
            Assert(main.Navigator.History.Entries.Count == 30 && pages.Count(p => !p.IsDisposed) == 30 && pages.Take(70).All(p => p.IsDisposed), "History leaked pages");
            Assert(pages.All(p => p.IsDisposed || !p.TopLevel) && Application.OpenForms.Cast<Form>().Count(f => f.TopLevel && f.Visible && f is not MainForm) <= 1, "Navigation created top-level windows");
            File.WriteAllText(Path.Combine(output, "handles.json"), JsonSerializer.Serialize(handles));
            main.EndSession(); Assert(pages.All(p => p.IsDisposed), "Session did not dispose all100 pages");
        });
        await Test("NAV.STATE.05 actual application context clears history on expiry and fresh login", async () =>
        {
            using var api = new FakeApi(); using var app = new AdminApplication(_ => api, new SettingsStore(Path.Combine(output, "session-settings")));
            await Task.Delay(40); var login = (LoginForm)app.MainForm!; login.UsernameBox.Text = "qa-admin"; login.PasswordBox.Text = "qa-test-input"; await login.SubmitAsync(); await Task.Delay(40);
            var shell = (MainForm)app.MainForm!; await WaitNavigation(shell); var first = new NavigationFixturePage("Dữ liệu phiên cũ"); await shell.Navigator.OpenAsync(first); await shell.Navigator.OpenAsync(new NavigationFixturePage("Trang thứ hai")); await shell.Navigator.BackAsync();
            api.Expire(); await Task.Delay(100);
            Assert(app.MainForm is LoginForm && shell.Navigator.History.Entries.Count == 0 && first.IsDisposed, "Expiry retained history/page");
            await shell.Navigator.BackAsync(); Assert(shell.Navigator.CurrentPage is null, "Back resurrected old data");
            login = (LoginForm)app.MainForm!; login.UsernameBox.Text = "qa-admin"; login.PasswordBox.Text = "qa-test-input"; await login.SubmitAsync(); await Task.Delay(60);
            var fresh = (MainForm)app.MainForm!; await WaitNavigation(fresh); Assert(fresh.Navigator.History.Entries.Count == 1 && !fresh.BackButton.Enabled, "Relogin reused history");
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var delayed = new DeferredNavigationPage(pending.Task);
            var transition = fresh.Navigator.OpenAsync(delayed); await Task.Delay(20); api.Expire(); await Task.Delay(60);
            Assert(app.MainForm is LoginForm && !delayed.Visible && fresh.Navigator.History.Entries.Count == 0, "Expiry retained pending page");
            pending.SetResult(); await transition; Assert(delayed.IsDisposed, "Late read leaked a page after session expiry");
            using var logoutApi = new FakeApi(); using var closing = new MainForm(logoutApi, FakeApi.Ready); closing.Show(); await WaitNavigation(closing);
            var closingRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var closingPage = new DeferredNavigationPage(closingRead.Task);
            var opening = closing.Navigator.OpenAsync(closingPage); await Task.Delay(20); await closing.LogoutAsync();
            Assert(closing.IsDisposed && closing.Navigator.History.Entries.Count == 0 && logoutApi.LogoutCalls == 1 && !closingPage.Visible, "Logout was blocked by a pending GET");
            closingRead.SetResult(); await opening; Assert(closingPage.IsDisposed, "Pending logout read leaked a page");
        });
        await Test("NAV.UI.05/07/08 FLOW.04/05 real shell keyboard/mouse and 1280x800 DPI captures", async () =>
        {
            using var api = new OrdersFake(); using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
            for (var i = 0; i < 10; i++) await main.Navigator.OpenAsync(new NavigationFixturePage("Chi tiết QA " + (i + 1)));
            await Capture(main, "navigation-ten-pages");
            var depth = main.Navigator.History.Entries.Count;
            var message = Message.Create(((NavigationFixturePage)main.Navigator.CurrentPage!).Grid.Handle, 0x020B, new IntPtr(1 << 16), IntPtr.Zero);
            Assert(Application.FilterMessage(ref message), "Mouse Back on child grid was not routed to shell");
            await WaitNavigation(main); Assert(main.Navigator.History.Index == depth - 2, "Mouse Back failed");
            var keyMessage = Message.Create(main.Handle, 0x0100, IntPtr.Zero, IntPtr.Zero);
            typeof(MainForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(main, new object[] { keyMessage, Keys.Alt | Keys.Right });
            await WaitNavigation(main); Assert(main.Navigator.History.Index == depth - 1, "AltRight failed");
            typeof(MainForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(main, new object[] { keyMessage, Keys.Alt | Keys.Left });
            await WaitNavigation(main); Assert(main.Navigator.History.Index == depth - 2, "AltLeft failed");
            message = Message.Create(((NavigationFixturePage)main.Navigator.CurrentPage!).Grid.Handle, 0x020B, new IntPtr(2 << 16), IntPtr.Zero);
            Assert(Application.FilterMessage(ref message), "Mouse Forward on child grid not routed"); await WaitNavigation(main);
            Assert(main.Navigator.History.Index == depth - 1, "Mouse Forward failed");
            var before = main.Navigator.History.Index; main.HomeButton.PerformClick(); await WaitNavigation(main);
            Assert(main.Navigator.History.Index == 0 && main.Navigator.History.Entries.Count == 1, "Home click did not reset");
            File.WriteAllText(Path.Combine(output, "interaction-after.json"), JsonSerializer.Serialize(new { measured = true, pageTransitions = 10, historyBeforeHome = before, homeClicks = 1, topLevelBusinessPages = 0 }));
            foreach (var key in new[] { Keys.Alt | Keys.Home, Keys.Control | Keys.H })
            {
                await main.Navigator.OpenAsync(new NavigationFixturePage("Kiểm tra phím Tổng quan"));
                typeof(MainForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(main, new object[] { keyMessage, key });
                await WaitNavigation(main); Assert(main.Navigator.History.Entries.Count == 1 && !main.BackButton.Enabled, "Home shortcut did not clear history");
            }
            await main.Navigator.OpenAsync(new OrderListForm(api));
            main.MinimumSize = new(1280, 800); main.Size = new(1280, 800); await Capture(main, "navigation-1280-baseline");
            var rect = new NativeRect { Left = 20, Top = 20, Right = 1300, Bottom = 820 };
            SendMessage(main.Handle, 0x02E0, new IntPtr((144 << 16) | 144), ref rect); await Task.Delay(80); main.MinimumSize = new(1280, 800); main.Size = new(1280, 800);
            await Capture(main, "navigation-1280-150-native-event");
            Assert(main.BackButton.AccessibleName == "Quay lại" && main.ContentHost.Width > 0 && main.ContentHost.Height > 100, "Navigation inaccessible or hides content"); main.EndSession();
        });
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { fakeApi = true, platform = "Windows STA", checks = passed - start, captures = captures.Skip(captureOffset).ToArray(), dpiMethod = "Baseline records actual DeviceDpi;150% uses native WM_DPICHANGED144, not physical monitor certification" }, new JsonSerializerOptions { WriteIndented = true }));
        output = old;
    }
    private static async Task NavigationPrintPreview()
    {
        output = Path.Combine(FindRoot(), ".artifacts", "admin-w6", "forms", "navigation"); Directory.CreateDirectory(output);
        Assert(new InvoicePrinter().HasInstalledPrinter, "Native preview requires installed driver");
        using var api = new OrdersFake { Row = OrdersFake.Order("HOAN_TAT") }; using var main = new MainForm(api, await api.ReadinessAsync()); main.Show(); await WaitNavigation(main);
        var detail = new OrderDetailForm(api, 33); await main.Navigator.OpenAsync(detail); await detail.OpenInvoiceAsync();
        var invoice = (InvoiceForm)main.Navigator.CurrentPage!; var seen = false; Exception? failure = null;
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += async (_, _) => { var preview = Application.OpenForms.OfType<PrintPreviewDialog>().FirstOrDefault(); if (preview is null) return; timer.Stop(); try { Assert(preview.Modal && preview.Owner == main, "Preview owner is not shell"); await Task.Delay(400); await Capture(preview, "navigation-native-print-preview"); seen = true; } catch (Exception e) { failure = e; } finally { preview.Close(); } };
        timer.Start(); invoice.Preview();
        Assert(failure is null && seen && ReferenceEquals(main.Navigator.CurrentPage, invoice) && invoice.MessageLabel.Text.StartsWith("Đã đóng"), "Native preview did not return to invoice");
        await main.Navigator.BackAsync(); Assert(ReferenceEquals(main.Navigator.CurrentPage, detail) && api.Writes == 0, "Preview changed history or data");
        File.WriteAllText(Path.Combine(output, "print-preview.json"), JsonSerializer.Serialize(new { fakeApi = true, realInstalledDriver = true, nativeModal = true, ownerIsShell = true, returnedToInvoiceThenOrder = true, writes = api.Writes })); main.EndSession();
        Console.WriteLine("PASS NAV.FLOW.03 actual PrintPreviewDialog/installed driver owned by shell; returns to invoice then Back to order; no printer output");
    }
}
