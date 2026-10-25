namespace PhoneStore.Core.Models;

public sealed record PhoneVariant(int Id, string Sku, string Name, string Brand,
    string Color, int StorageGb, decimal Price, string Specifications, bool Active = true);

public sealed class BusinessException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class Stock
{
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Available => OnHand - Reserved;
    public void Receive(int quantity)
    {
        Positive(quantity);
        OnHand = checked(OnHand + quantity);
    }
    public void Reserve(int quantity)
    {
        Positive(quantity);
        if (quantity > Available) throw new BusinessException("OUT_OF_STOCK", "Không đủ hàng có thể bán.");
        Reserved += quantity;
    }
    public void Release(int quantity)
    {
        Positive(quantity);
        if (quantity > Reserved) throw new BusinessException("INVALID_STATE", "Lượng giải phóng vượt lượng giữ.");
        Reserved -= quantity;
    }
    public void Dispatch(int quantity)
    {
        Release(quantity);
        OnHand -= quantity;
    }
    public void Adjust(int difference, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new BusinessException("INVALID_INPUT", "Cần lý do kiểm kê.");
        var next = checked(OnHand + difference);
        if (next < Reserved) throw new BusinessException("INVALID_INPUT", "Tồn thực tế không được thấp hơn lượng giữ.");
        OnHand = next;
    }
    private static void Positive(int quantity)
    {
        if (quantity <= 0) throw new BusinessException("INVALID_INPUT", "Số lượng phải lớn hơn 0.");
    }
}

public enum OrderStatus { Pending, Confirmed, Shipping, Completed, Cancelled, Failed }
public enum PaymentStatus { Unpaid, Paid }
public sealed record OrderLine(int VariantId, string Name, decimal UnitPrice, int Quantity)
{
    public decimal Total => UnitPrice * Quantity;
}

// Persistence adapters must save order, stock and audit records in one transaction.
public sealed class Order
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CustomerId { get; }
    public Guid RequestKey { get; }
    public string Address { get; }
    public IReadOnlyList<OrderLine> Lines { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentStatus Payment { get; private set; }
    public bool ReturnReceived { get; private set; }
    public int Version { get; private set; }
    public decimal Total => Lines.Sum(x => x.Total);
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public Order(Guid customerId, Guid requestKey, string address, IEnumerable<OrderLine> lines)
    {
        var snapshot = lines.ToArray();
        if (customerId == Guid.Empty || requestKey == Guid.Empty || string.IsNullOrWhiteSpace(address)
            || snapshot.Length == 0 || snapshot.Any(x => x.Quantity <= 0 || x.UnitPrice <= 0 || x.VariantId <= 0 || string.IsNullOrWhiteSpace(x.Name))
            || snapshot.Select(x => x.VariantId).Distinct().Count() != snapshot.Length)
            throw new BusinessException("INVALID_INPUT", "Thông tin đặt hàng không hợp lệ.");
        CustomerId = customerId; RequestKey = requestKey; Address = address.Trim();
        Lines = Array.AsReadOnly(snapshot);
    }
    public void Confirm(int version) => Change(version, OrderStatus.Pending, OrderStatus.Confirmed);
    public void Ship(int version) => Change(version, OrderStatus.Confirmed, OrderStatus.Shipping);
    public void CompleteAndCollectCod(int version)
    {
        Change(version, OrderStatus.Shipping, OrderStatus.Completed);
        Payment = PaymentStatus.Paid;
    }
    public void MarkFailed(int version) => Change(version, OrderStatus.Shipping, OrderStatus.Failed);
    public void Cancel(Guid actorId, bool isAdmin, int version)
    {
        if (!isAdmin && actorId != CustomerId) throw new BusinessException("FORBIDDEN", "Không có quyền truy cập đơn hàng.");
        CheckVersion(version);
        if (Status != OrderStatus.Pending && !(isAdmin && Status == OrderStatus.Confirmed))
            throw new BusinessException("INVALID_STATE", "Không thể hủy đơn ở trạng thái hiện tại.");
        Status = OrderStatus.Cancelled; Version++;
    }
    public void ReceiveReturn(int version)
    {
        CheckVersion(version);
        if (Status != OrderStatus.Failed || ReturnReceived) throw new BusinessException("INVALID_STATE", "Không thể nhận lại hàng.");
        ReturnReceived = true; Version++;
    }
    private void Change(int version, OrderStatus expected, OrderStatus next)
    {
        CheckVersion(version);
        if (Status != expected) throw new BusinessException("INVALID_STATE", "Trạng thái đơn hàng không hợp lệ.");
        Status = next; Version++;
    }
    private void CheckVersion(int version)
    {
        if (version != Version) throw new BusinessException("CONCURRENT_UPDATE", "Đơn hàng đã thay đổi. Vui lòng tải lại.");
    }
}
