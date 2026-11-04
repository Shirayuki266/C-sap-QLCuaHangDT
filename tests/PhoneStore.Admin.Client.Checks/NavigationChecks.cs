using PhoneStore.Admin.Navigation;

namespace PhoneStore.Admin.Client.Checks;

internal static class NavigationChecks
{
    public static void Run()
    {
        static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        var history = new NavigationHistory<string>();
        Check(history.Current is null && !history.Back() && !history.Forward(), "Empty history moves");
        history.Push("home"); history.Push("customers"); history.Push("customer/7");
        Check(history.Back() && history.Current == "customers" && history.CanForward, "Back failed");
        Check(history.Forward() && history.Current == "customer/7", "Forward failed");
        Check(history.GoTo(0) && !history.GoTo(-1) && !history.GoTo(3), "Breadcrumb bounds failed");
        var removed = history.Push("orders");
        Check(removed.SequenceEqual(new[] { "customers", "customer/7" }) && !history.CanForward, "New branch retains forward entries");
        for (var i = 0; i < 100; i++) history.Push("page/" + i);
        Check(history.Entries.Count == 30 && history.Index == 29 && history.Entries[0] == "page/70", "Capacity failed");
        Check(history.Reset("home").Count == 30 && history.Current == "home" && !history.CanBack && !history.CanForward, "Home failed");
        Check(history.Clear().Count == 1 && history.Index == -1 && history.Current is null, "Logout clear failed");
        Check(!history.GoTo(0), "Cleared history resurrects a page");
        try { _ = new NavigationHistory<string>(0); throw new InvalidOperationException("Capacity zero accepted"); }
        catch (ArgumentOutOfRangeException) { }
        Console.WriteLine("PASS NAV.BASE.03/UI.02/UI.04/STATE.04 history: push/back/forward/branch/breadcrumb/100 entries/capacity30/reset/clear");
    }
}
