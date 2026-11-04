using System.Text;
using System.Text.Json.Serialization;

namespace PhoneStore.Admin.Client;

public sealed record AdminOrderLine(long VariantId,string Name,string Sku,string Description,int Quantity,decimal Price,decimal Total);
public sealed record AdminOrderHistory(long Id,string? From,string To,long ActorId,string Actor,DateTimeOffset At,string? Reason);
public sealed record AdminOrder(long Id,string Code,long CustomerId,string Recipient,string Phone,string Address,string Status,string Payment,
    decimal Total,DateTimeOffset CreatedAt,DateTimeOffset? CompletedAt,string Version,bool ReturnReceived,List<AdminOrderLine> Lines,List<AdminOrderHistory> History);
public sealed record AdminInvoice(string Title,string Notice,AdminOrder Order);
public enum OrderAction { CustomerCancel,Confirm,Cancel,Dispatch,Complete,Fail,ReceiveReturn,Invoice }
public sealed record OrderCommand(string Version,Guid OperationKey,[property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Reason=null);
public sealed class OrderOperation(AdminOrder snapshot,OrderAction action,string? reason=null)
{
    public AdminOrder Snapshot { get; }=snapshot;
    public OrderAction Action { get; }=action;
    public string? Reason { get; }=string.IsNullOrWhiteSpace(reason)?null:reason.Trim();
    public CommandIntent Intent { get; }=new();
    public bool NeedsReadback { get; internal set; }
    public bool Finished { get; internal set; }
    public OrderCommand Request()=>new(Snapshot.Version,Intent.Key,Reason);
}
public static class OrderPresentation
{
    public static readonly string[] States=["CHO_XAC_NHAN","DA_XAC_NHAN","DANG_GIAO","HOAN_TAT","DA_HUY","GIAO_THAT_BAI"];
    // Display policy only. Every requested command is independently validated by the server.
    public static bool Allowed(string status,bool returned,OrderAction action)=>action switch
    {
        OrderAction.CustomerCancel or OrderAction.Confirm=>status=="CHO_XAC_NHAN",
        OrderAction.Cancel=>status is "CHO_XAC_NHAN" or "DA_XAC_NHAN",
        OrderAction.Dispatch=>status=="DA_XAC_NHAN",
        OrderAction.Complete or OrderAction.Fail=>status=="DANG_GIAO",
        OrderAction.ReceiveReturn=>status=="GIAO_THAT_BAI"&&!returned,
        OrderAction.Invoice=>status is "DA_XAC_NHAN" or "DANG_GIAO" or "HOAN_TAT",_=>false
    };
    public static string Route(OrderAction action)=>action switch{OrderAction.Confirm=>"confirm",OrderAction.Cancel or OrderAction.CustomerCancel=>"cancel",OrderAction.Dispatch=>"dispatch",OrderAction.Complete=>"complete",OrderAction.Fail=>"fail",OrderAction.ReceiveReturn=>"receive-return",OrderAction.Invoice=>"invoice",_=>throw new ArgumentOutOfRangeException(nameof(action))};
    public static string Label(OrderAction action)=>action switch{OrderAction.CustomerCancel=>"Khách hủy",OrderAction.Confirm=>"Xác nhận đơn",OrderAction.Cancel=>"Hủy đơn",OrderAction.Dispatch=>"Xuất giao",OrderAction.Complete=>"Hoàn tất + thu COD",OrderAction.Fail=>"Giao thất bại",OrderAction.ReceiveReturn=>"Nhận lại hàng",OrderAction.Invoice=>"Xem hóa đơn",_=>""};
    public static bool RequiresReason(OrderAction action)=>action is OrderAction.Cancel or OrderAction.CustomerCancel or OrderAction.Fail;
    public static string? ReasonError(OrderAction action,string? reason)=>RequiresReason(action)&&string.IsNullOrWhiteSpace(reason)?"Nhập lý do trước khi thực hiện.":reason?.Trim().Length>1000?"Lý do tối đa 1000 ký tự.":null;
    // Contract COD invariant: payment is a string; amount/collectedAt are not response fields.
    public static decimal Collected(AdminOrder order)=>order.Payment=="DA_THU"?order.Total:0;
    public static DateTimeOffset? CollectedAt(AdminOrder order)=>order.Payment=="DA_THU"?order.CompletedAt:null;
    public static string Summary(AdminOrder order)=>$"{order.Code} · {ContactForms.OrderState(order.Status)}\r\nNgười nhận: {order.Recipient} · {order.Phone}\r\nĐịa chỉ: {order.Address}\r\nĐặt lúc: {AdminFormatting.Time(order.CreatedAt)} · Tổng: {AdminFormatting.Money(order.Total)}\r\nCOD: {ContactForms.Payment(order.Payment)} · Đã thu: {AdminFormatting.Money(Collected(order))} · Thu lúc: {(CollectedAt(order) is {} at?AdminFormatting.Time(at):"Chưa thu")}\r\nNhận lại hàng: {(order.ReturnReceived?"Đã nhận lại":"Chưa nhận lại")} · Version: {order.Version}";
    public static string Confirmation(OrderOperation op)=>Label(op.Action)+"\n"+op.Snapshot.Code+" · "+op.Snapshot.Recipient+"\n"+(op.Action switch{OrderAction.Complete=>"Hoàn tất và ghi thu COD theo tổng máy chủ: "+AdminFormatting.Money(op.Snapshot.Total),OrderAction.Cancel=>"Hủy đơn và trả lượng đang giữ; tồn thực không đổi.",OrderAction.Dispatch=>"Xuất hàng thực tế và giảm lượng đang giữ.",OrderAction.Fail=>"Ghi giao thất bại; chưa trả hàng về kho, COD chưa thu.",OrderAction.ReceiveReturn=>"Chỉ xác nhận khi đã nhận hàng thực tế. Máy chủ ghi trả kho một lần.",_=>"Chuyển sang đã xác nhận; COD vẫn chưa thu."})+(op.Reason is null?"":"\nLý do: "+op.Reason);
    public static string InvoiceText(AdminInvoice invoice)
    {
        var text=new StringBuilder().AppendLine(invoice.Title).AppendLine(invoice.Notice).AppendLine().AppendLine(Summary(invoice.Order)).AppendLine();
        foreach(var line in invoice.Order.Lines)text.AppendLine($"{line.Sku} · {line.Name} · {line.Description}").AppendLine($"  {line.Quantity} × {AdminFormatting.Money(line.Price)} = {AdminFormatting.Money(line.Total)}");
        return text.AppendLine().AppendLine("Tổng từ máy chủ: "+AdminFormatting.Money(invoice.Order.Total)).ToString();
    }
}
public sealed class OrdersWorkspace(IAdminApi api)
{
    public Task<PageResult<AdminOrder>> List(string? query,string? status,DateOnly? from,DateOnly? to,int page=1,int size=20,CancellationToken ct=default)
    {
        if(from>to)throw new AdminApiException("INVALID_INPUT",400);
        var path="/api/admin/orders"+AdminFormatting.ListQuery(query,page,size)+(status is null?"":"&status="+Uri.EscapeDataString(status))+(from.HasValue?"&from="+AdminFormatting.Date(from.Value):"")+(to.HasValue?"&to="+AdminFormatting.Date(to.Value):"");
        return api.GetAsync<PageResult<AdminOrder>>(path,ct);
    }
    public Task<AdminOrder> Read(long id,CancellationToken ct=default)=>api.GetAsync<AdminOrder>($"/api/admin/orders/{id}",ct);
    public Task<AdminInvoice> Invoice(long id,CancellationToken ct=default)=>api.GetAsync<AdminInvoice>($"/api/admin/orders/{id}/invoice",ct);
    public async Task<AdminOrder> Execute(OrderOperation operation,CancellationToken ct=default)
    {
        if(operation.Action is OrderAction.CustomerCancel or OrderAction.Invoice)throw new ArgumentException("Admin write action required");
        if(operation.Finished||operation.NeedsReadback)throw new AdminApiException("READBACK_REQUIRED");
        try{await api.CommandAsync($"/api/admin/orders/{operation.Snapshot.Id}/{OrderPresentation.Route(operation.Action)}",operation.Request(),operation.Intent,ct);operation.NeedsReadback=true;var read=await Read(operation.Snapshot.Id,ct);operation.NeedsReadback=false;operation.Finished=true;return read;}
        catch(AdminApiException error){if(api.NeedsReconciliation||error.Status==409)operation.NeedsReadback=true;throw;}
    }
    public async Task<AdminOrder> Resolve(OrderOperation operation,CancellationToken ct=default)
    {
        AdminOrder row=null!;await api.ReconcileAsync(async token=>row=await Read(operation.Snapshot.Id,token),ct);operation.NeedsReadback=false;operation.Finished=row.Version!=operation.Snapshot.Version||row.Status!=operation.Snapshot.Status||row.ReturnReceived!=operation.Snapshot.ReturnReceived;return row;
    }
}
