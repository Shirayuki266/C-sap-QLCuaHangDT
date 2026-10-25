using PhoneStore.Core.Models;


var passed = 0;
void Check(string name, Action test) { test(); Console.WriteLine($"PASS {name}"); passed++; }
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
void Reject(string code, Action action)
{
    try { action(); } catch (BusinessException ex) when (ex.Code == code) { return; }
    throw new Exception($"Expected {code}");
}
Order Create() => new(Guid.NewGuid(), Guid.NewGuid(), "123 Nguyễn Trãi", [new(1, "Điện thoại", 100m, 1)]);
Check("Stock cannot oversell or become negative", () => {
    var stock = new Stock(); stock.Receive(1); stock.Reserve(1);
    Reject("OUT_OF_STOCK", () => stock.Reserve(1));
    Assert(stock.OnHand == 1 && stock.Reserved == 1 && stock.Available == 0);
    Reject("INVALID_INPUT", () => stock.Adjust(-1, "Kiểm kê"));
    stock.Dispatch(1); Assert(stock.OnHand == 0 && stock.Reserved == 0);
});
Check("Invalid stock operations preserve balances", () => {
    var stock = new Stock(); Reject("INVALID_INPUT", () => stock.Receive(0));
    Reject("INVALID_STATE", () => stock.Dispatch(1)); Assert(stock.OnHand == 0);
});
Check("Cancellation requires ownership and pending status", () => {
    var order = Create(); Reject("FORBIDDEN", () => order.Cancel(Guid.NewGuid(), false, 0));
    order.Confirm(0); Reject("INVALID_STATE", () => order.Cancel(order.CustomerId, false, 1));
    order.Cancel(Guid.NewGuid(), true, 1); Assert(order.Status == OrderStatus.Cancelled);
});
Check("Stale version and repeated transitions are rejected", () => {
    var order = Create(); order.Confirm(0);
    Reject("CONCURRENT_UPDATE", () => order.Cancel(order.CustomerId, false, 0));
    Reject("INVALID_STATE", () => order.Confirm(1));
});
Check("COD is paid only on successful completion", () => {
    var order = Create(); Reject("INVALID_STATE", () => order.CompleteAndCollectCod(0));
    order.Confirm(0); order.Ship(1); Assert(order.Payment == PaymentStatus.Unpaid);
    order.CompleteAndCollectCod(2); Assert(order.Payment == PaymentStatus.Paid && order.Status == OrderStatus.Completed);
});
Check("Failed delivery can be received only once", () => {
    var order = Create(); order.Confirm(0); order.Ship(1); order.MarkFailed(2);
    Assert(!order.ReturnReceived && order.Payment == PaymentStatus.Unpaid);
    order.ReceiveReturn(3); Reject("INVALID_STATE", () => order.ReceiveReturn(4));
});
Check("Order captures immutable lines and prices", () => {
    var lines = new List<OrderLine> { new(1, "Tên cũ", 100m, 2) };
    var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "Địa chỉ", lines);
    lines[0] = new(1, "Tên mới", 200m, 1); Assert(order.Total == 200m && order.Lines[0].Name == "Tên cũ");
    Reject("INVALID_INPUT", () => new Order(Guid.NewGuid(), Guid.NewGuid(), " ", lines));
});
Check("Order rejects duplicate variants", () => {
    Reject("INVALID_INPUT", () => new Order(Guid.NewGuid(), Guid.NewGuid(), "Địa chỉ hợp lệ", [new(1, "Điện thoại", 100m, 1),new(1, "Điện thoại", 100m, 2)]));
});
Console.WriteLine($"{passed} checks passed.");
if(args.Contains("--integration")) await IntegrationChecks.Run();
if(args.Contains("--api")) await ApiChecks.Run(Environment.GetEnvironmentVariable("PHONESTORE_API_URL")??throw new InvalidOperationException("Cần PHONESTORE_API_URL của môi trường QA; không fallback."));

