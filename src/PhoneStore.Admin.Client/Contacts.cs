namespace PhoneStore.Admin.Client;

public sealed record CustomerRecord(long Id,string Username,string FullName,string Phone,string? Email,string Address,bool Active,DateTimeOffset CreatedAt);
public sealed record CustomerProfileRequest(string FullName,string Phone,string? Email,string Address);
public sealed record CustomerStateRequest(bool Active,bool ExpectedActive);
public sealed record SupplierRecord(long Id,string Code,string Name,string Phone,string Address,bool Active)
{
    public override string ToString()=>Code+" · "+Name+(Active?"":" · Đã ngừng");
}
public sealed record SupplierRequest(string Code,string Name,string Phone,string Address,bool Active);
// Read-only W3 projection. Full fulfillment/history/invoice commands belong to W5.
public sealed record OrderPeekLine(long VariantId,string Name,string Sku,string Description,int Quantity,decimal Price,decimal Total);
public sealed record CustomerOrder(long Id,string Code,long CustomerId,string Recipient,string Phone,string Address,string Status,string Payment,decimal Total,DateTimeOffset CreatedAt,List<OrderPeekLine> Lines);
public sealed record OrderHandoff(long OrderId);

public static class ContactForms
{
    public static string? ProfileError(CustomerProfileRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.FullName)||request.FullName.Trim().Length>120)return "Nhập họ tên, tối đa 120 ký tự.";
        if(!PhoneAllowed(request.Phone))return "Số điện thoại cần 9–15 chữ số, có thể bắt đầu bằng +.";
        if(request.Email is {Length:>254}||(!string.IsNullOrWhiteSpace(request.Email)&&(!System.Net.Mail.MailAddress.TryCreate(request.Email.Trim(),out var email)||email.Address!=request.Email.Trim())))return "Email chưa hợp lệ.";
        return request.Address.Length>500?"Địa chỉ tối đa 500 ký tự.":null;
    }
    public static bool PhoneAllowed(string phone)=>System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(),@"\A\+?[0-9]{9,15}\z",System.Text.RegularExpressions.RegexOptions.CultureInvariant|System.Text.RegularExpressions.RegexOptions.NonBacktracking);
    public static string? SupplierError(SupplierRequest request)
    {
        if(string.IsNullOrWhiteSpace(request.Code)||request.Code.Trim().Length>30||string.IsNullOrWhiteSpace(request.Name)||request.Name.Trim().Length>200)return "Nhập mã (tối đa 30 ký tự) và tên NCC (tối đa 200 ký tự).";
        if(!PhoneAllowed(request.Phone))return "Số điện thoại cần 9–15 chữ số, có thể bắt đầu bằng +.";
        return string.IsNullOrWhiteSpace(request.Address)||request.Address.Trim().Length>500?"Nhập địa chỉ NCC, tối đa 500 ký tự.":null;
    }
    public static string OrderState(string state)=>state switch
    {
        "CHO_XAC_NHAN"=>"Chờ xác nhận","DA_XAC_NHAN"=>"Đã xác nhận","DANG_GIAO"=>"Đang giao","HOAN_TAT"=>"Hoàn tất","DA_HUY"=>"Đã hủy","GIAO_THAT_BAI"=>"Giao thất bại",_=>state
    };
    public static string Payment(string status)=>status=="DA_THU"?"Đã thu":status=="CHUA_THU"?"Chưa thu":status;
}

public sealed class ContactsWorkspace(IAdminApi api)
{
    public long? PendingCustomerId { get; private set; }
    public (long Id,string Code)? PendingSupplier { get; private set; }
    private static string StateQuery(bool? active)=>active.HasValue?"&active="+active.Value.ToString().ToLowerInvariant():"";
    public Task<PageResult<CustomerRecord>> Customers(string? query,bool? active,int page=1,int pageSize=20,CancellationToken ct=default)
        =>api.GetAsync<PageResult<CustomerRecord>>("/api/admin/customers"+AdminFormatting.ListQuery(query,page,pageSize)+StateQuery(active),ct);
    public async Task<CustomerRecord> Customer(long id,CancellationToken ct=default)
    {var result=await api.GetAsync<CustomerRecord>($"/api/admin/customers/{id}",ct);if(PendingCustomerId==id)PendingCustomerId=null;return result;}
    public async Task<CustomerRecord> SaveProfile(long id,CustomerProfileRequest input,CancellationToken ct=default)
    {
        if(PendingCustomerId.HasValue)throw new AdminApiException("READBACK_REQUIRED");
        await api.PutAsync($"/api/admin/customers/{id}",input,ct);PendingCustomerId=id;return await ReadBackCustomer(id,ct);
    }
    public async Task<CustomerRecord> SetCustomerState(CustomerRecord snapshot,bool active,CancellationToken ct=default)
    {
        if(PendingCustomerId.HasValue)throw new AdminApiException("READBACK_REQUIRED");
        await api.CommandAsync($"/api/admin/customers/{snapshot.Id}/state",new CustomerStateRequest(active,snapshot.Active),new CommandIntent(),ct);
        PendingCustomerId=snapshot.Id;return await ReadBackCustomer(snapshot.Id,ct);
    }
    private async Task<CustomerRecord> ReadBackCustomer(long id,CancellationToken ct)
    {try{return await Customer(id,ct);}catch(AdminApiException){throw new AdminApiException("READBACK_REQUIRED");}}
    public async Task<CustomerRecord> ReconcileCustomer(long id,CancellationToken ct=default)
    {CustomerRecord row=null!;await api.ReconcileAsync(async token=>row=await Customer(id,token),ct);return row;}
    public Task<PageResult<CustomerOrder>> Orders(long customerId,int page=1,int pageSize=20,CancellationToken ct=default)
    {
        if(page<1||pageSize is <1 or >100)throw new AdminApiException("INVALID_INPUT",400);
        return api.GetAsync<PageResult<CustomerOrder>>($"/api/admin/customers/{customerId}/orders?page={page}&pageSize={pageSize}",ct);
    }
    public Task<CustomerOrder> Order(long id,CancellationToken ct=default)=>api.GetAsync<CustomerOrder>($"/api/admin/orders/{id}",ct);
    public Task<PageResult<SupplierRecord>> Suppliers(string? query,bool? active=null,int page=1,int pageSize=20,CancellationToken ct=default)
        =>api.GetAsync<PageResult<SupplierRecord>>("/api/admin/suppliers"+AdminFormatting.ListQuery(query,page,pageSize)+StateQuery(active),ct);
    public async Task<List<SupplierRecord>> ActiveSuppliers(CancellationToken ct=default)
    {
        var rows=new List<SupplierRecord>();
        for(var page=1;;page++)
        {
            var batch=await Suppliers(null,true,page,100,ct);
            if(batch.Items.Any(s=>!s.Active))throw new AdminApiException("INVALID_RESPONSE");
            rows.AddRange(batch.Items);if(rows.Count>=batch.Total)return rows;if(batch.Items.Count==0)throw new AdminApiException("INVALID_RESPONSE");
        }
    }
    public async Task<SupplierRecord?> FindSupplier(long id,string code,CancellationToken ct=default)
    {
        async Task<SupplierRecord?> Scan(string? query)
        {
            for(var page=1;;page++)
            {
                var batch=await Suppliers(query,null,page,100,ct);
                var found=batch.Items.SingleOrDefault(s=>id>0?s.Id==id:string.Equals(s.Code,code.Trim(),StringComparison.OrdinalIgnoreCase));
                if(found is not null)return found;if(page*100>=batch.Total)return null;if(batch.Items.Count==0)throw new AdminApiException("INVALID_RESPONSE");
            }
        }
        var result=await Scan(code);if(result is null&&id>0)result=await Scan(null);
        if(result is not null&&PendingSupplier?.Id==result.Id)PendingSupplier=null;return result;
    }
    public async Task<SupplierRecord> SaveSupplier(long id,SupplierRequest input,CancellationToken ct=default)
    {
        if(PendingSupplier.HasValue)throw new AdminApiException("READBACK_REQUIRED");
        if(id==0)id=await api.CreateAsync("/api/admin/suppliers",input,ct);else await api.PutAsync($"/api/admin/suppliers/{id}",input,ct);
        PendingSupplier=(id,input.Code);
        try{return await FindSupplier(id,input.Code,ct)??throw new AdminApiException("NOT_FOUND",404);}catch(AdminApiException){throw new AdminApiException("READBACK_REQUIRED");}
    }
    public async Task<SupplierRecord?> ReconcileSupplier(long id,string code,CancellationToken ct=default)
    {SupplierRecord? row=null;await api.ReconcileAsync(async token=>row=await FindSupplier(id,code,token),ct);return row;}
}
