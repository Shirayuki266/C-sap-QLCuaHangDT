namespace PhoneStore.Admin;

public abstract class NavigationPageForm : Form
{
    private Func<Task>? load;
    private Func<bool>? working;
    public Task Loading { get; private set; } = Task.CompletedTask;
    public bool Initialized { get; private set; }
    public bool IsWorking => !Loading.IsCompleted || working?.Invoke() == true;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? NavigationLabel { get; protected set; }
    protected void ConfigurePageLoad(Func<Task> action, Func<bool>? isWorking = null)
    {
        load = action; working = isWorking;
        Shown += (_, _) => { if (!Initialized) _ = ReloadPageAsync(); };
    }
    public Task ReloadPageAsync()
    {
        if (!Loading.IsCompleted) return Loading;
        Initialized = true;
        return Loading = load?.Invoke() ?? Task.CompletedTask;
    }
}

internal sealed class OverviewPage : NavigationPageForm
{
    public DashboardView Dashboard { get; }
    public OverviewPage(PhoneStore.Admin.Client.IAdminApi api)
    {
        Text = "Tổng quan";
        Dashboard = new(api); Controls.Add(Dashboard);
        ConfigurePageLoad(Dashboard.LoadAsync);
    }
}
