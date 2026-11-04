namespace PhoneStore.Admin;

internal static class PageNavigation
{
    public static MainForm? Shell(Control? source)
    {
        for (var form = source as Form ?? source?.FindForm(); form is not null; form = form.Owner)
        {
            if (form is MainForm shell) return shell;
            for (Control? parent = form.Parent; parent is not null; parent = parent.Parent)
                if (parent is MainForm host) return host;
        }
        return null;
    }
    // Standalone form fixtures retain their original modal contract. Production pages use the shell.
    public static async Task OpenAsync(Control source, NavigationPageForm page)
    {
        if (Shell(source) is { } shell) await shell.Navigator.OpenAsync(page);
        else { using (page) page.ShowDialog(source.FindForm()); }
    }
    public static IWin32Window Owner(Control source) => (IWin32Window?)Shell(source) ?? source.FindForm()!;
    public static void StateChanged(Control source)
    { if (source is NavigationPageForm) Shell(source)?.RefreshNavigationState(); }
}
