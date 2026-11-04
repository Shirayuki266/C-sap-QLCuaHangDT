using System.Text.Json;

namespace PhoneStore.Admin.Tests;

internal static partial class Program
{
    // Run only against an archived pre-navigation build. This measures window mechanics,
    // not a claim that the old business graph has a ten-page-deep route.
    private static async Task LegacyNavigationBaseline()
    {
        Assert(typeof(MainForm).GetProperty("Navigator") is null, "Baseline requires the pre-navigation source archive");
        var target = Environment.GetEnvironmentVariable("PHONESTORE_NAV_BASELINE_OUTPUT") ?? throw new InvalidOperationException("Baseline output required");
        Directory.CreateDirectory(target);
        using var api = new OrdersFake(); using var shell = new MainForm(api, await api.ReadinessAsync()); shell.Show();
        var pages = new List<OrderListForm>(); var closing = false; var closeActions = 0; var peak = 0;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += async (_, _) =>
        {
            try
            {
                if (!closing && pages.Count < 10)
                {
                    var owner = pages.LastOrDefault() as Form ?? shell;
                    using var page = new OrderListForm(api); pages.Add(page); peak = Math.Max(peak, pages.Count);
                    page.ShowDialog(owner); return;
                }
                if (!closing)
                {
                    closing = true; timer.Stop();
                    var oldOutput = output; output = target; await Capture(pages[^1], "legacy-tenth-modal"); output = oldOutput;
                    timer.Start();
                }
                if (pages.Count > 0) { var page = pages[^1]; pages.RemoveAt(pages.Count - 1); closeActions++; page.Close(); }
                else { timer.Stop(); done.TrySetResult(); }
            }
            catch (Exception error) { timer.Stop(); done.TrySetException(error); }
        };
        timer.Start(); await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert(peak == 10 && closeActions == 10, "Legacy close count differs");
        File.WriteAllText(Path.Combine(target, "interaction-before.json"), JsonSerializer.Serialize(new
        {
            measured = true, sourceCommit = Environment.GetEnvironmentVariable("PHONESTORE_NAV_BASELINE_COMMIT"),
            method = "Archived develop build; STA fixture opens ten real OrderListForm instances using legacy ShowDialog(owner); timer simulates ten separate close actions",
            businessRouteWalkthrough = false, peakModalPages = peak, topLevelWindowsIncludingShell = peak + 1, closeActionsToOverview = closeActions
        }, new JsonSerializerOptions { WriteIndented = true }));
        shell.EndSession(); Console.WriteLine("PASS legacy modal baseline:10 pages,11 top-level windows,10 close actions to overview");
    }
}
