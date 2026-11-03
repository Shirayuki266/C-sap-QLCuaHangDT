using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Core.Commerce;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Generation;
using PhoneStore.Infrastructure.Accounts;
using PhoneStore.Infrastructure.Commerce;
using PhoneStore.Infrastructure.Data;

namespace PhoneStore.DemoData.Persistence;

public sealed class TimelineWriter(StoreDbContext db, SimulationClock clock, DemoPlan plan, Secrets secrets)
{
    public async Task Write(SalesTimeline timeline)
    {
        var kernel = new BusinessKernel(db, clock);
        var sessions = new AccountSessions(db, new PasswordHasher<Account>(), clock);
        var cached = new Dictionary<string, PhoneStore.Core.Accounts.AccountSession>(StringComparer.Ordinal);
        async Task<Actor> Login(int? customer = null)
        {
            var username = customer.HasValue ? plan.Customers[customer.Value].Username : "demo.admin.large";
            if (!cached.TryGetValue(username, out var session) || session.ExpiresAt.UtcDateTime <= clock.Current)
            {
                session = await sessions.LoginAsync(new(username, customer.HasValue ? secrets.Customer : secrets.Admin, customer.HasValue ? "customer" : "admin"));
                cached[username] = session;
                db.ChangeTracker.Clear();
            }
            return new(session.Account.Id, session.SessionId);
        }
        var variants = await db.Variants.AsNoTracking().ToDictionaryAsync(x => x.Sku, x => x.Id);
        var products = await db.Products.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id);
        var suppliers = await db.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id);
        var orderIds = new Dictionary<int, long>();
        var purchaseService = new PurchaseService(kernel);
        var query = new OrderQueryService(kernel);
        var workflow = new OrderWorkflow(kernel);
        var carts = new CartService(kernel);
        foreach (var item in timeline.Events)
        {
            clock.Current = item.At;
            var key = DemoPlan.Key(plan.Seed, $"{item.Kind}/{item.Index}");
            switch (item.Kind)
            {
                case EventKind.Purchase:
                {
                    var receipt = timeline.Receipts[item.Index];
                    var admin = await Login();
                    var id = await purchaseService.Save(admin, 0, new(suppliers[plan.Suppliers[receipt.Supplier].Code],
                        receipt.Lines.Select(x => new PurchaseLineInput(variants[x.Sku], x.Quantity, x.Cost)).ToList(),
                        RequestKey: DemoPlan.Key(plan.Seed, $"purchase/create/{receipt.Index}")));
                    db.ChangeTracker.Clear();
                    var view = await purchaseService.Read(admin, id);
                    if (receipt.Status == "DA_XAC_NHAN") await purchaseService.Confirm(admin, id, new(view.Version, key));
                    else if (receipt.Status == "DA_HUY") await purchaseService.Cancel(admin, id, new(view.Version, key));
                    break;
                }
                case EventKind.Place:
                {
                    var sale = timeline.Sales[item.Index];
                    var actor = await Login(sale.Customer);
                    var customer = plan.Customers[sale.Customer];
                    foreach (var line in sale.Lines) { await carts.Set(actor, new(variants[line.Sku], line.Quantity)); db.ChangeTracker.Clear(); }
                    var cart = await carts.Read(actor);
                    var other = sale.Index % 5 == 0;
                    var order = await new CheckoutService(kernel).Place(actor, new(key,
                        other ? $"Người nhận minh họa {sale.Index + 1}" : customer.Name, customer.Phone,
                        other ? $"{10000 + sale.Index} Đường Giao Mẫu, Đà Nẵng" : customer.Address,
                        cart.Lines.Select(x => new PriceConfirmation(x.VariantId, x.Quantity, x.Price!.Value)).ToList()));
                    orderIds[sale.Index] = order.Id;
                    break;
                }
                case EventKind.Retire:
                {
                    var admin = await Login();
                    var service = new ProductService(kernel);
                    var product = await service.Read(admin, products[plan.Products[item.Index].Code]);
                    await service.Save(admin, product.Id, new(product.Code, product.Name, product.BrandId, product.CategoryId,
                        product.Specifications, product.Description, product.ImageUrl, false,
                        product.Variants.Select(x => new VariantInput(x.Id, x.Sku, x.Color, x.StorageGb, x.Cost, x.ProfitPercent, x.Active, x.Version)).ToList(), product.Images));
                    break;
                }
                case EventKind.Count:
                {
                    var count = timeline.Counts[item.Index];
                    var admin = await Login();
                    var stock = await db.Inventory.AsNoTracking().SingleAsync(x => x.VariantId == variants[count.Sku]);
                    await new InventoryService(kernel).Adjust(admin, stock.VariantId,
                        new(count.Counted, count.Reason, Convert.ToBase64String(stock.Version), key));
                    break;
                }
                default:
                {
                    var sale = timeline.Sales[item.Index];
                    var customerCancel = item.Kind == EventKind.Cancel && sale.Ending == "cancel-pending" && sale.Index % 2 == 0;
                    var actor = await Login(customerCancel ? sale.Customer : null);
                    var order = await query.Read(actor, orderIds[sale.Index], !customerCancel);
                    var command = new VersionedCommand(order.Version, key, item.Kind switch
                    {
                        EventKind.Cancel => "Khách thay đổi kế hoạch mua hàng.",
                        EventKind.Fail => "Không thể liên hệ người nhận trong đợt giao.",
                        EventKind.Return => "Kho đã đối chiếu và nhận đủ máy sau giao thất bại.",
                        _ => null
                    });
                    switch (item.Kind)
                    {
                        case EventKind.Confirm: await workflow.Confirm(actor, order.Id, command); break;
                        case EventKind.Dispatch: await workflow.Dispatch(actor, order.Id, command); break;
                        case EventKind.Complete: await workflow.Complete(actor, order.Id, command); break;
                        case EventKind.Cancel: await workflow.Cancel(actor, order.Id, command, !customerCancel); break;
                        case EventKind.Fail: await workflow.Fail(actor, order.Id, command); break;
                        case EventKind.Return: await workflow.ReceiveReturn(actor, order.Id, command); break;
                        default: throw new InvalidOperationException("Sự kiện mô phỏng không hợp lệ.");
                    }
                    break;
                }
            }
            db.ChangeTracker.Clear();
        }
        clock.Current = timeline.Context.Cutoff;
        foreach (var line in timeline.Carts)
        {
            await carts.Set(await Login(line.Customer), new(variants[line.Sku], line.Quantity));
            db.ChangeTracker.Clear();
        }
        var finalAdmin = await Login();
        foreach (var customer in plan.Customers.Where(x => x.Locked))
        {
            var id = await db.Accounts.Where(x => x.Username == customer.Username).Select(x => x.Id).SingleAsync();
            await new CustomerAdminService(kernel).SetActive(finalAdmin, id, new(false, true));
            db.ChangeTracker.Clear();
        }
        foreach (var supplier in plan.Suppliers.TakeLast(plan.Scale == "full" ? 2 : 1))
        {
            await new SupplierService(kernel).Save(finalAdmin, suppliers[supplier.Code], new(supplier.Code, supplier.Name, supplier.Phone, supplier.Address, false));
            db.ChangeTracker.Clear();
        }
    }
}
