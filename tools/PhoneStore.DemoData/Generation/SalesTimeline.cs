using System.Globalization;
using PhoneStore.DemoData.Contracts;

namespace PhoneStore.DemoData.Generation;

public enum EventKind { Retire, Purchase, Place, Confirm, Dispatch, Complete, Cancel, Fail, Return, Count }
public sealed record TimelineEvent(DateTime At, EventKind Kind, int Index);
public sealed record SaleLine(string Sku, int Quantity);
public sealed class TimelineSale(int index, DateTime createdAt, string ending)
{
    public int Index { get; } = index;
    public DateTime CreatedAt { get; set; } = createdAt;
    public string Ending { get; } = ending;
    public int Customer { get; set; }
    public int? ForcedProduct { get; set; }
    public DateTime? ForcedCompletion { get; set; }
    public List<SaleLine> Lines { get; } = [];
}
public sealed record ReceiptLine(string Sku, int Quantity, decimal Cost);
public sealed class TimelineReceipt(int index, int supplier, string status)
{
    public int Index { get; } = index;
    public int Supplier { get; } = supplier;
    public string Status { get; } = status;
    public List<ReceiptLine> Lines { get; } = [];
}
public sealed record TimelineCount(string Sku, int Counted, string Reason);
public sealed record TimelineCart(int Customer, string Sku, int Quantity);
public sealed record SalesTimeline(DatasetContext Context, List<TimelineEvent> Events, List<TimelineSale> Sales,
    List<TimelineReceipt> Receipts, List<TimelineCount> Counts, List<TimelineCart> Carts)
{
    private sealed class Balance(PlannedProduct product, PlannedVariant variant)
    {
        public PlannedProduct Product { get; } = product;
        public PlannedVariant Variant { get; } = variant;
        public int OnHand { get; set; }
        public int Reserved { get; set; }
        public int Receipts { get; set; }
    }

    public static SalesTimeline Create(DemoPlan plan)
    {
        var full = plan.Scale == "full";
        // Midnight at the beginning of as-of (UTC+7) is deterministic and never in today's future.
        var cutoff = plan.AsOf.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var retiredCount = full ? 4 : 1;
        var context = new DatasetContext(plan.Seed, plan.AsOf, plan.Scale, plan.Start, cutoff,
            plan.Products.Select((p, i) => new ProductLifecycle(p.Code, p.LaunchAt, i < retiredCount ? cutoff.AddDays(-45) : null)).ToArray());
        var result = new SalesTimeline(context, [], [], [], [], []);
        var balances = plan.Products.SelectMany(p => p.Variants.Select(v => new Balance(p, v)))
            .ToDictionary(x => x.Variant.Sku, StringComparer.Ordinal);
        var localStart = plan.Start.AddHours(7);
        var firstCustomer = plan.Customers.Min(x => x.CreatedAt).AddDays(2);
        for (var quarter = 0; quarter < 9; quarter++)
        {
            var start = localStart.AddMonths(quarter * 3).AddHours(-7);
            var end = quarter == 8 ? cutoff : localStart.AddMonths((quarter + 1) * 3).AddHours(-7).AddTicks(-1);
            var month = start.AddHours(7).Month;
            int[] baseTargets = [62, 58, 66, 70];
            int[] grownTargets = [76, 70, 80, 84];
            var orders = full ? quarter == 8 ? 35 : (quarter < 4 ? baseTargets : grownTargets)[(month - 1) / 3]
                : quarter == 8 ? 12 : 6;
            var cancels = full ? quarter == 8 ? 2 : (int)Math.Round(orders * .08, MidpointRounding.AwayFromZero) : quarter is 1 or 8 ? 1 : 0;
            var fails = full ? quarter == 8 ? 1 : (int)Math.Round(orders * .04, MidpointRounding.AwayFromZero) : quarter is 2 or 8 ? 1 : 0;
            var open = quarter == 8 ? full ? 18 : 3 : 0;
            for (var n = 0; n < orders; n++)
            {
                var index = result.Sales.Count;
                var ending = n < open ? new[] { "pending", "confirmed", "shipping" }[n % 3]
                    : n < open + cancels ? n % 2 == 0 ? "cancel-pending" : "cancel-confirmed"
                    : n < open + cancels + fails ? index % 5 == 0 ? "failed" : "returned" : "complete";
                var latest = end.AddDays(ending is "failed" or "returned" ? -9 : -7);
                var earliest = start > firstCustomer ? start : firstCustomer;
                var at = WeightedDate(plan.Seed, index, earliest, latest < earliest ? earliest : latest);
                if (quarter == 8 && n < open)
                    at = cutoff.AddHours(ending == "pending" ? -2 : ending == "confirmed" ? -18 : -72);
                if (at < start) at = start;
                if (at > end) at = end;
                result.Sales.Add(new(index, at, ending));
            }
            var receiptCount = full ? quarter == 0 ? 7 : quarter == 8 ? 3 : 6 : 2;
            for (var n = 0; n < receiptCount; n++)
            {
                var at = start.AddDays((int)Math.Max(0, (end - start).TotalDays - 1) * n / Math.Max(1, receiptCount - 1)).AddHours(8);
                if (at > end) at = end;
                var index = result.Receipts.Count;
                result.Receipts.Add(new(index, index % plan.Suppliers.Length, "DA_XAC_NHAN"));
                result.Events.Add(new(at, EventKind.Purchase, index));
            }
        }
        // Complete eight eligible products in THIS month, even on its first day, using late prior-month orders.
        var monthStart = new DateTime(cutoff.AddHours(7).Year, cutoff.AddHours(7).Month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
        var forcedCreated = monthStart.AddDays(-5);
        // Quarter membership is in UTC+7, including boundaries where UTC is the previous calendar day.
        var forcedQuarter = (forcedCreated.AddHours(7).Year - localStart.Year) * 4 + (forcedCreated.AddHours(7).Month - 1) / 3 - (localStart.Month - 1) / 3;
        var forcedStart = localStart.AddMonths(forcedQuarter * 3).AddHours(-7);
        var forcedEnd = localStart.AddMonths((forcedQuarter + 1) * 3).AddHours(-7);
        var bestCount = full ? 8 : 5;
        var forced = result.Sales.Where(x => x.Ending == "complete" && x.CreatedAt >= forcedStart && x.CreatedAt < forcedEnd).TakeLast(bestCount).ToArray();
        if (forced.Length != bestCount) throw new InvalidOperationException("Không đủ đơn hoàn tất cho tháng hiện tại.");
        for (var n = 0; n < forced.Length; n++)
        {
            forced[n].CreatedAt = forcedCreated;
            forced[n].ForcedCompletion = monthStart;
            forced[n].ForcedProduct = retiredCount + n;
        }
        // At month end the previous-month placements used above may be older than 30 days.
        // Move a few other current-quarter intentions forward, preserving their quarter and completion windows.
        var recentStart = cutoff.AddDays(-30);
        var requiredRecent = full ? 20 : 6;
        foreach (var sale in result.Sales.Where(x => x.CreatedAt >= localStart.AddMonths(24).AddHours(-7) &&
                     x.CreatedAt < recentStart && x.ForcedCompletion is null).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Index))
        {
            if (result.Sales.Count(x => x.CreatedAt >= recentStart) >= requiredRecent) break;
            var shifted = cutoff.AddDays(sale.Ending.StartsWith("cancel", StringComparison.Ordinal) ? -2 : -9);
            var currentQuarterStart = localStart.AddMonths(24).AddHours(-7);
            sale.CreatedAt = shifted < currentQuarterStart ? currentQuarterStart : shifted;
        }
        // Preserve evidence of actual sales for the four old products before retiring them.
        var oldSales = result.Sales.Where(x => x.Ending == "complete" && x.ForcedProduct is null && x.CreatedAt > plan.Start.AddDays(45))
            .OrderBy(x => x.CreatedAt).Take(retiredCount).ToArray();
        for (var i = 0; i < oldSales.Length; i++) oldSales[i].ForcedProduct = i;
        AssignCustomers(plan, result.Sales);
        foreach (var sale in result.Sales)
        {
            result.Events.Add(new(sale.CreatedAt, EventKind.Place, sale.Index));
            if (sale.Ending == "cancel-pending") { result.Events.Add(new(sale.CreatedAt.AddHours(6), EventKind.Cancel, sale.Index)); continue; }
            if (sale.Ending == "pending") continue;
            result.Events.Add(new(sale.CreatedAt.AddHours(12), EventKind.Confirm, sale.Index));
            if (sale.Ending == "cancel-confirmed") { result.Events.Add(new(sale.CreatedAt.AddHours(20), EventKind.Cancel, sale.Index)); continue; }
            if (sale.Ending == "confirmed") continue;
            result.Events.Add(new(sale.CreatedAt.AddHours(36), EventKind.Dispatch, sale.Index));
            if (sale.Ending == "shipping") continue;
            result.Events.Add(new(sale.ForcedCompletion ?? sale.CreatedAt.AddDays(5), sale.Ending == "complete" ? EventKind.Complete : EventKind.Fail, sale.Index));
            if (sale.Ending == "returned") result.Events.Add(new(sale.CreatedAt.AddDays(7), EventKind.Return, sale.Index));
        }
        for (var i = 0; i < retiredCount; i++) result.Events.Add(new(context.Products[i].RetiredAt!.Value, EventKind.Retire, i));
        for (var i = 0; i < (full ? 2 : 1); i++)
        {
            result.Counts.Add(new("", 0, "Kiểm kho định kỳ giữa kỳ: phát hiện một máy lỗi."));
            result.Events.Add(new(localStart.AddMonths(6 + i * 9).AddDays(10).AddHours(8).AddHours(-7), EventKind.Count, i));
        }
        // Three drafts (one empty), one cancelled document. They never change simulated stock or cost.
        for (var i = 0; i < 4; i++)
        {
            var index = result.Receipts.Count;
            result.Receipts.Add(new(index, i % plan.Suppliers.Length, i == 3 ? "DA_HUY" : "NHAP"));
            result.Events.Add(new(cutoff.AddDays(-12 + i), EventKind.Purchase, index));
        }
        result.Events.RemoveAll(x => x.At > cutoff);
        result.Events.Sort((a, b) => a.At != b.At ? a.At.CompareTo(b.At) : a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Index.CompareTo(b.Index));
        foreach (var item in result.Events)
        {
            if (item.Kind == EventKind.Purchase)
            {
                var receipt = result.Receipts[item.Index];
                if (receipt.Status == "NHAP" && item.Index == result.Receipts.Count - 4) continue;
                var eligible = balances.Values.Where(x => x.Product.LaunchAt < item.At.AddMinutes(-30) &&
                    !(context.Products.Single(p => p.Code == x.Product.Code).RetiredAt <= item.At))
                    .OrderBy(x => x.Receipts == 0 ? 0 : 1).ThenBy(x => x.OnHand - x.Reserved)
                    .ThenBy(x => x.Product.LaunchAt).ThenBy(x => x.Variant.Sku, StringComparer.Ordinal).Take(full ? 5 : 3).ToArray();
                foreach (var stock in eligible)
                {
                    var age = QuarterDistance(stock.Product.LaunchAt, item.At);
                    var multiplier = Enumerable.Range(0, age).Aggregate(1m, (value, _) => value * .95m);
                    var cost = decimal.Round(stock.Variant.Cost * multiplier, 2, MidpointRounding.AwayFromZero);
                    var quantity = Math.Max(1, 16 + DemoPlan.Number(plan.Seed, $"receipt/{item.Index}/{stock.Variant.Sku}", 12) - stock.OnHand);
                    receipt.Lines.Add(new(stock.Variant.Sku, quantity, cost));
                    if (receipt.Status == "DA_XAC_NHAN") { stock.OnHand += quantity; stock.Receipts++; }
                }
            }
            else if (item.Kind == EventKind.Place)
            {
                var sale = result.Sales[item.Index];
                var lineCount = 1 + (sale.Index % 3 == 0 ? 1 : 0) + (sale.Index % 60 == 0 ? 1 : 0);
                for (var line = 0; line < lineCount; line++)
                {
                    var quantity = line == 0 && sale.Index % 37 == 0 ? 2 : 1;
                    var candidates = balances.Values.Where(x => x.OnHand - x.Reserved >= quantity && x.Product.LaunchAt < item.At &&
                        !(context.Products.Single(p => p.Code == x.Product.Code).RetiredAt <= item.At) && sale.Lines.All(l => l.Sku != x.Variant.Sku)).ToArray();
                    if (line == 0 && sale.ForcedProduct is int product) candidates = candidates.Where(x => x.Product.Code == plan.Products[product].Code).ToArray();
                    if (candidates.Length == 0) throw new InvalidOperationException($"Không đủ tồn trong kế hoạch tại đơn {sale.Index}, dòng {line}.");
                    // Most baskets use budget/midrange phones; premium/foldable sales remain occasional.
                    var selected = candidates.OrderBy(x => DemoPlan.Number(plan.Seed, $"sale/{sale.Index}/{line}/{x.Variant.Sku}", int.MaxValue)
                        / (x.Product.Category == 0 ? 2 : x.Product.Category == 1 ? 3 : 1)).First();
                    selected.Reserved += quantity;
                    sale.Lines.Add(new(selected.Variant.Sku, quantity));
                }
            }
            else if (item.Kind is EventKind.Cancel or EventKind.Dispatch or EventKind.Return)
            {
                foreach (var line in result.Sales[item.Index].Lines)
                {
                    var stock = balances[line.Sku];
                    if (item.Kind is EventKind.Cancel or EventKind.Dispatch) stock.Reserved -= line.Quantity;
                    if (item.Kind == EventKind.Dispatch) stock.OnHand -= line.Quantity;
                    if (item.Kind == EventKind.Return) stock.OnHand += line.Quantity;
                }
            }
            else if (item.Kind == EventKind.Count)
            {
                var selected = balances.Values.Where(x => x.OnHand > x.Reserved + 1)
                    .OrderBy(x => x.Variant.Sku, StringComparer.Ordinal).First();
                selected.OnHand--;
                result.Counts[item.Index] = result.Counts[item.Index] with { Sku = selected.Variant.Sku, Counted = selected.OnHand };
            }
            if (balances.Values.Any(x => x.Reserved < 0 || x.OnHand < x.Reserved)) throw new InvalidOperationException("Kế hoạch vi phạm tồn kho.");
        }
        var countedStocks = balances.Values.Where(x => x.Reserved <= 5 && x.OnHand > 0 && !(x.Reserved == 5 && x.OnHand == 5) &&
                context.Products.Single(p => p.Code == x.Product.Code).RetiredAt is null)
            .OrderBy(x => x.Reserved == 0 ? 0 : 1).ThenBy(x => x.Variant.Sku, StringComparer.Ordinal).Take(full ? 8 : 5).ToArray();
        if (countedStocks.Length != (full ? 8 : 5)) throw new InvalidOperationException($"Thiếu biến thể cho kiểm kê cuối kỳ: {plan.Scale}/{plan.AsOf}/{plan.Seed}, có {countedStocks.Length}.");
        for (var i = 0; i < countedStocks.Length; i++)
        {
            var counted = i < (full ? 3 : 1) ? 0 : Math.Max(countedStocks[i].Reserved, i % 5 + 1);
            if (counted == countedStocks[i].OnHand) counted = counted < 5 ? counted + 1 : Math.Max(countedStocks[i].Reserved, 4);
            countedStocks[i].OnHand = counted;
            var index = result.Counts.Count;
            result.Counts.Add(new(countedStocks[i].Variant.Sku, counted, "Kiểm kho định kỳ: đối chiếu hàng lỗi và số máy thực tế."));
            result.Events.Add(new(cutoff, EventKind.Count, index));
        }
        for (var customer = 0; customer < (full ? 12 : 3); customer++)
        {
            var count = full ? customer == 0 ? 3 : 2 : 2;
            foreach (var stock in balances.Values.Where(x => x.OnHand - x.Reserved > 0 && context.Products.Single(p => p.Code == x.Product.Code).RetiredAt is null)
                         .OrderBy(x => DemoPlan.Number(plan.Seed, $"cart/{customer}/{x.Variant.Sku}", int.MaxValue)).Take(count))
                result.Carts.Add(new(customer, stock.Variant.Sku, 1));
        }
        return result;
    }

    private static int QuarterDistance(DateTime from, DateTime to)
    {
        from = from.AddHours(7); to = to.AddHours(7);
        return Math.Max(0, (to.Year - from.Year) * 4 + (to.Month - 1) / 3 - (from.Month - 1) / 3);
    }

    private static DateTime WeightedDate(int seed, int order, DateTime earliest, DateTime latest)
    {
        if (latest <= earliest) return earliest;
        var days = new List<(DateTime At, int Weight)>();
        var calendar = new ChineseLunisolarCalendar();
        for (var day = earliest.AddHours(7).Date; day <= latest.AddHours(7).Date; day = day.AddDays(1))
        {
            var at = DateTime.SpecifyKind(day.AddHours(18 + DemoPlan.Number(seed, $"hour/{order}", 5)), DateTimeKind.Utc).AddHours(-7);
            if (at < earliest || at > latest) continue;
            var spring = calendar.ToDateTime(day.Year, 1, 1, 0, 0, 0, 0);
            var weight = day.Day <= 5 ? 3 : 1;
            if ((spring - day).TotalDays is >= 1 and <= 21) weight += 5;
            if (day.Month is 11 or 12) weight += 2;
            if ((day.Month == 11 && day.Day == 11) || (day.Month == 12 && day.Day == 12) ||
                (day.Month == 11 && day.Day is >= 23 and <= 29 && day.DayOfWeek == DayOfWeek.Friday)) weight += 18;
            days.Add((at, weight));
        }
        if (days.Count == 0) return earliest;
        var selected = DemoPlan.Number(seed, $"date/{order}", days.Sum(x => x.Weight));
        foreach (var day in days) { selected -= day.Weight; if (selected < 0) return day.At; }
        return days[^1].At;
    }

    private static void AssignCustomers(DemoPlan plan, List<TimelineSale> sales)
    {
        var remaining = sales.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Index).ToList();
        // Reserve two non-buyers, cover every other account, retain many one-time buyers.
        for (var customer = plan.Customers.Length - 3; customer >= 0; customer--)
        {
            var target = plan.Scale == "full" && customer is >= 12 and < 38 ? 5 + customer % 6 : 1;
            for (var n = 0; n < target; n++)
            {
                var sale = remaining.FirstOrDefault(x => x.CreatedAt > plan.Customers[customer].CreatedAt);
                if (sale is null) { if (n == 0) throw new InvalidOperationException("Không phủ đủ khách mua theo ngày tạo tài khoản."); break; }
                sale.Customer = customer;
                remaining.Remove(sale);
            }
        }
        foreach (var sale in remaining.OrderBy(x => x.CreatedAt).ThenBy(x => x.Index))
        {
            var eligible = Enumerable.Range(0, plan.Scale == "full" ? 12 : 2).Where(i => plan.Customers[i].CreatedAt < sale.CreatedAt).ToArray();
            sale.Customer = eligible[DemoPlan.Number(plan.Seed, $"customer/{sale.Index}", eligible.Length)];
        }
    }
}
