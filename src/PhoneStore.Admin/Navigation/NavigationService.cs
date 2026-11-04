using PhoneStore.Admin.Navigation;

namespace PhoneStore.Admin;

public sealed class NavigationService : IDisposable
{
    private readonly MainForm shell;
    private readonly Panel host;
    private readonly Func<NavigationPageForm> home;
    private readonly Dictionary<NavigationPageForm, PageViewState> states = [];
    private bool transitioning, ended;
    public NavigationHistory<NavigationPageForm> History { get; } = new();
    public NavigationPageForm? CurrentPage => History.Current;
    public bool IsBusy => transitioning || CurrentPage?.IsWorking == true;
    public bool IsEnded => ended;
    public event Action? Changed;
    public NavigationService(MainForm shell, Panel host, Func<NavigationPageForm> home)
    { this.shell = shell; this.host = host; this.home = home; }

    public Task OpenAsync(NavigationPageForm page) => TransitionAsync(page, null, false);
    public Task HomeAsync() => TransitionAsync(null, null, true);
    public Task BackAsync() => GoToAsync(History.Index - 1);
    public Task ForwardAsync() => GoToAsync(History.Index + 1);
    public Task GoToAsync(int index) => index >= 0 && index < History.Entries.Count && index != History.Index
        ? TransitionAsync(null, index, false) : Task.CompletedTask;

    private async Task TransitionAsync(NavigationPageForm? incoming, int? index, bool reset)
    {
        if (ended || IsBusy || shell.OwnedForms.Any(f => f.Visible && f.Modal)) { incoming?.Dispose(); return; }
        transitioning = true; Changed?.Invoke();
        try
        {
            var previous = CurrentPage;
            if (previous is not null && !await UnsavedChanges.TryLeaveAsync(previous)) { incoming?.Dispose(); return; }
            if (ended) { incoming?.Dispose(); return; }
            if (previous is not null) { states[previous] = PageViewState.Capture(previous); previous.Hide(); host.Controls.Remove(previous); }
            IReadOnlyList<NavigationPageForm> removed = [];
            if (reset) removed = History.Reset(home());
            else if (incoming is not null) removed = History.Push(incoming);
            else if (index.HasValue) History.GoTo(index.Value);
            foreach (var page in removed) Release(page);
            var current = CurrentPage!;
            var revisit = current.Initialized;
            current.TopLevel = false; current.FormBorderStyle = FormBorderStyle.None;
            current.MinimumSize = Size.Empty; current.Dock = DockStyle.Fill;
            current.AccessibleName = "Trang: " + Title(current);
            current.Name = current.GetType().Name;
            host.Controls.Add(current); current.Show();
            if (!revisit) AdminTheme.InitializeControls(current);
            if (revisit || !current.Initialized) await current.ReloadPageAsync(); else await current.Loading;
            if (ended) { current.Dispose(); return; }
            if (current.IsDisposed) return;
            if (states.TryGetValue(current, out var state)) state.Restore(current);
            else current.SelectNextControl(null, true, true, true, false);
            shell.AcceptButton = current.AcceptButton;
        }
        finally { transitioning = false; Changed?.Invoke(); }
    }
    public static string Title(Form form) => (form as NavigationPageForm)?.NavigationLabel ?? form.Text.Split(" · Monophone Admin", StringSplitOptions.None)[0];
    private void Release(NavigationPageForm page)
    {
        states.Remove(page); page.Hide(); host.Controls.Remove(page);
        // A pending read may complete, but its detached controls can never reappear in a new session.
        if (!page.IsWorking) page.Dispose(); else _ = DisposeAfterReadAsync(page);
    }
    private static async Task DisposeAfterReadAsync(NavigationPageForm page)
    { try { await page.Loading; while (page.IsWorking) await Task.Delay(20); } finally { page.Dispose(); } }
    public void Dispose()
    {
        if (ended) return; ended = true; shell.AcceptButton = null;
        foreach (var page in History.Clear()) Release(page);
        states.Clear(); Changed?.Invoke();
    }
}

internal sealed class PageViewState
{
    private readonly List<Action> restore = [];
    public static PageViewState Capture(Control page)
    {
        var result = new PageViewState();
        foreach (var grid in UnsavedChanges.Descendants(page).OfType<DataGridView>())
        {
            var first = grid.FirstDisplayedScrollingRowIndex; var horizontal = grid.HorizontalScrollingOffset;
            var selected = grid.CurrentRow?.Index ?? -1; var column = grid.CurrentCell?.ColumnIndex ?? 0;
            var key = grid.CurrentRow?.Tag;
            var id = key?.GetType().GetProperty("Id")?.GetValue(key) ?? key?.GetType().GetProperty("VariantId")?.GetValue(key);
            result.restore.Add(() =>
            {
                if (grid.Rows.Count == 0) return;
                var row = id is null ? null : grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r =>
                    Equals(r.Tag?.GetType().GetProperty("Id")?.GetValue(r.Tag) ?? r.Tag?.GetType().GetProperty("VariantId")?.GetValue(r.Tag), id));
                selected = row?.Index ?? Math.Clamp(selected, 0, grid.Rows.Count - 1);
                if (grid.Columns.Count > 0) grid.CurrentCell = grid.Rows[selected].Cells[Math.Clamp(column, 0, grid.Columns.Count - 1)];
                if (first >= 0) grid.FirstDisplayedScrollingRowIndex = Math.Min(first, grid.Rows.Count - 1);
                grid.HorizontalScrollingOffset = horizontal;
            });
        }
        foreach (var panel in UnsavedChanges.Descendants(page).OfType<ScrollableControl>().Where(p => p.AutoScroll))
        { var point = panel.AutoScrollPosition; result.restore.Add(() => panel.AutoScrollPosition = new(-point.X, -point.Y)); }
        var focus = UnsavedChanges.Descendants(page).FirstOrDefault(c => c.Focused);
        if (focus is not null) result.restore.Add(() => { if (focus.CanFocus) focus.Focus(); });
        return result;
    }
    public void Restore(Control page) { page.PerformLayout(); foreach (var action in restore) action(); }
}
