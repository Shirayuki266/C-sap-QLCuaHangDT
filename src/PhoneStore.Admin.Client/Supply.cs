using System.Text.Json.Serialization;

namespace PhoneStore.Admin.Client;

public sealed record PurchaseLineRequest(long VariantId,int Quantity,decimal Cost);
public sealed record PurchaseRequest(long SupplierId,IReadOnlyList<PurchaseLineRequest> Lines,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Version=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] Guid RequestKey=default);
public sealed record PurchaseLineRecord(long VariantId,string Sku,string Name,int Quantity,decimal Cost,decimal Total);
public sealed record PurchaseRecord(long Id,string Code,long SupplierId,string Supplier,string Status,decimal Total,long CreatorId,long? ConfirmerId,
    DateTimeOffset CreatedAt,DateTimeOffset? ConfirmedAt,string Version,List<PurchaseLineRecord> Lines);
public sealed record PurchaseTransition(string Version,Guid OperationKey);
public sealed record StockRecord(long VariantId,long ProductId,string Name,string Sku,string Brand,string Color,int StorageGb,int OnHand,int Reserved,int Available,string Version);
public sealed record MovementRecord(long Id,long VariantId,string Sku,string Kind,int DeltaOnHand,int DeltaReserved,DateTimeOffset At,long ActorId,string Actor,long? OrderId,long? PurchaseId,string? Reason,Guid OperationKey);
public sealed record StockAdjustment(int Counted,string Reason,string Version,Guid OperationKey);
public sealed record SupplyChoice(long VariantId,string Sku,string Name,decimal Cost);
public sealed record StockPrice(StockRecord Stock,decimal Cost,decimal Price);
public sealed record PurchaseReadback(PurchaseRecord Purchase,List<StockPrice> CurrentStock);
public sealed record CountReadback(StockRecord Stock,bool Applied,bool RetryAllowed);

public sealed class PurchaseLineDraft
{
    public long VariantId { get; init; }
    public string Sku { get; init; }="";
    public string Name { get; init; }="";
    public int Quantity { get; set; }=1;
    public decimal Cost { get; set; }=1;
    public PurchaseLineRequest Request()=>new(VariantId,Quantity,Cost);
}
public static class SupplyPresentation
{
    public static bool Draft(PurchaseRecord? p)=>p?.Status=="NHAP";
    public static string State(string value)=>value switch{"NHAP"=>"Nháp","DA_XAC_NHAN"=>"Đã xác nhận","DA_HUY"=>"Đã hủy",_=>value};
    public static string Failure(Exception error)=>error is AdminApiException api?api.PublicValidation??api.Message:"Chưa thể hoàn tất thao tác. Vui lòng tải lại.";
    public static string? LinesError(long supplier,IReadOnlyList<PurchaseLineRequest> lines)
    {
        if(supplier<=0)return "Chọn nhà cung cấp đang hoạt động.";
        if(lines.Count>100||lines.Select(x=>x.VariantId).Distinct().Count()!=lines.Count)return "Tối đa 100 dòng, không trùng biến thể.";
        if(lines.Any(x=>x.VariantId<=0||x.Quantity<=0||x.Cost<=0||decimal.Round(x.Cost,2)!=x.Cost))return "Mỗi dòng cần biến thể hợp lệ, số lượng nguyên dương, giá vốn dương với tối đa 2 chữ số thập phân.";
        return null;
    }
    public static string? CountError(StockRecord stock,int counted,string reason)
    {
        if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length>1000)return "Nhập lý do kiểm kê, tối đa 1000 ký tự.";
        if(counted<0||counted<stock.Reserved)return "Số thực tế không được âm hoặc thấp hơn số đang giữ trên bản đang tải. Máy chủ sẽ kiểm tra lại khi lưu.";
        return counted==stock.OnHand?"Số thực tế bằng tồn hiện tại, không có điều chỉnh để lưu.":null;
    }
    public static string CountSummary(StockRecord stock,int counted)=>$"{stock.Sku}: hiện có {stock.OnHand}, đang giữ {stock.Reserved}, số thực tế {counted}; chênh lệch dự kiến {(long)counted-stock.OnHand:+0;-0;0}.";
    public static string PurchaseSummary(PurchaseRecord p)=>$"{p.Code} · {p.Supplier}\nTổng tiền từ máy chủ: {AdminFormatting.Money(p.Total)}\nLượng nhập: {p.Lines.Sum(l=>(long)l.Quantity)} · {p.Lines.Count} dòng.";
    public static string Movement(string kind)=>kind switch{"NHAP"=>"Nhập hàng","DIEU_CHINH"=>"Kiểm kê","GIU"=>"Giữ hàng","HUY_GIU"=>"Trả giữ hàng","XUAT_GIAO"=>"Xuất giao","NHAN_LAI"=>"Nhận lại",_=>kind};
}
public sealed class PurchaseOperation(PurchaseRecord snapshot,string action)
{
    public PurchaseRecord Snapshot { get; }=snapshot;
    public string Action { get; }=action is "confirm" or "cancel"?action:throw new ArgumentException("Invalid purchase action");
    public CommandIntent Intent { get; }=new();
    public bool NeedsReadback { get; internal set; }
    public bool Finished { get; internal set; }
    public PurchaseTransition Request()=>new(Snapshot.Version,Intent.Key);
}
public sealed class CountOperation(StockRecord snapshot,int counted,string reason)
{
    public StockRecord Snapshot { get; }=snapshot;
    public int Counted { get; }=counted;
    public string Reason { get; }=reason.Trim();
    public CommandIntent Intent { get; }=new();
    public bool NeedsReadback { get; internal set; }
    public bool Finished { get; internal set; }
    public StockAdjustment Request()=>new(Counted,Reason,Snapshot.Version,Intent.Key);
}

public sealed class SupplyWorkspace(IAdminApi api)
{
    private static string Page(int page,int size){if(page<1||size is <1 or >100)throw new AdminApiException("INVALID_INPUT",400);return $"page={page}&pageSize={size}";}
    public Task<PageResult<PurchaseRecord>> Purchases(string? query,string? status,int page=1,int size=20,CancellationToken ct=default)
        =>api.GetAsync<PageResult<PurchaseRecord>>("/api/admin/purchases"+AdminFormatting.ListQuery(query,page,size)+(status is null?"":"&status="+Uri.EscapeDataString(status)),ct);
    public Task<PurchaseRecord> Purchase(long id,CancellationToken ct=default)=>api.GetAsync<PurchaseRecord>($"/api/admin/purchases/{id}",ct);
    public Task<PageResult<StockRecord>> Stock(string? query,int? brand,int? below,int page=1,int size=20,CancellationToken ct=default)
        =>api.GetAsync<PageResult<StockRecord>>("/api/admin/stock"+AdminFormatting.ListQuery(query,page,size)+(brand.HasValue?$"&brandId={brand}":"")+(below.HasValue?$"&below={below}":""),ct);
    public Task<PageResult<MovementRecord>> Movements(long? variant,Guid? key=null,int page=1,int size=20,CancellationToken ct=default)
        =>api.GetAsync<PageResult<MovementRecord>>("/api/admin/stock/movements?"+Page(page,size)+(variant.HasValue?$"&variantId={variant}":"")+(key.HasValue?$"&operationKey={key}":""),ct);
    public async Task<StockRecord> StockRow(long id,string sku,CancellationToken ct=default)
    {
        async Task<StockRecord?> Scan(string? query){for(var page=1;;page++){var batch=await Stock(query,null,null,page,100,ct);var row=batch.Items.SingleOrDefault(x=>x.VariantId==id);if(row is not null)return row;if(page*100>=batch.Total)return null;if(batch.Items.Count==0)throw new AdminApiException("INVALID_RESPONSE");}}
        return await Scan(sku)??await Scan(null)??throw new AdminApiException("NOT_FOUND",404);
    }
    public async Task<PurchaseReadback> Readback(long id,CancellationToken ct=default)
    {
        var p=await Purchase(id,ct);var rows=new List<StockPrice>();
        if(p.Status=="DA_XAC_NHAN")
        {
            var products=new Dictionary<long,ProductRecord>();var catalog=new CatalogWorkspace(api);
            foreach(var line in p.Lines){var stock=await StockRow(line.VariantId,line.Sku,ct);if(!products.TryGetValue(stock.ProductId,out var product)){product=await catalog.Product(stock.ProductId,ct);products.Add(stock.ProductId,product);}var v=product.Variants.Single(x=>x.Id==line.VariantId);rows.Add(new(stock,v.Cost,v.Price));}
        }
        return new(p,rows);
    }
    public async Task<List<PurchaseLineDraft>> FromProduct(PurchaseHandoff handoff,CancellationToken ct=default)
    {
        if(handoff.ProductId<=0||handoff.Lines.Count==0||handoff.Lines.Any(l=>l.Quantity<=0)||handoff.Lines.Select(l=>l.VariantId).Distinct().Count()!=handoff.Lines.Count)throw new AdminApiException("INVALID_INPUT",400);
        var product=await new CatalogWorkspace(api).Product(handoff.ProductId,ct);var rows=new List<PurchaseLineDraft>();
        foreach(var hint in handoff.Lines){var variant=product.Variants.SingleOrDefault(v=>v.Id==hint.VariantId)??throw new AdminApiException("NOT_FOUND",404);rows.Add(new(){VariantId=variant.Id,Sku=variant.Sku,Name=product.Name,Quantity=hint.Quantity,Cost=variant.Cost});}return rows;
    }
    public async Task<PurchaseReadback> Execute(PurchaseOperation operation,CancellationToken ct=default)
    {
        if(operation.Finished||operation.NeedsReadback)throw new AdminApiException("READBACK_REQUIRED");
        try{await api.CommandAsync($"/api/admin/purchases/{operation.Snapshot.Id}/{operation.Action}",operation.Request(),operation.Intent,ct);operation.NeedsReadback=true;var read=await Readback(operation.Snapshot.Id,ct);operation.NeedsReadback=false;operation.Finished=true;return read;}
        catch(AdminApiException){if(api.NeedsReconciliation)operation.NeedsReadback=true;throw;}
    }
    public async Task<PurchaseReadback> Resolve(PurchaseOperation operation,CancellationToken ct=default)
    {
        PurchaseReadback read=null!;await api.ReconcileAsync(async token=>read=await Readback(operation.Snapshot.Id,token),ct);
        operation.NeedsReadback=false;operation.Finished=read.Purchase.Status!="NHAP"||read.Purchase.Version!=operation.Snapshot.Version;return read;
    }
    public async Task<StockRecord> Adjust(CountOperation operation,CancellationToken ct=default)
    {
        if(operation.Finished||operation.NeedsReadback)throw new AdminApiException("READBACK_REQUIRED");
        try{await api.CommandAsync($"/api/admin/stock/{operation.Snapshot.VariantId}/adjust",operation.Request(),operation.Intent,ct);operation.NeedsReadback=true;var row=await StockRow(operation.Snapshot.VariantId,operation.Snapshot.Sku,ct);operation.NeedsReadback=false;operation.Finished=true;return row;}
        catch(AdminApiException){if(api.NeedsReconciliation)operation.NeedsReadback=true;throw;}
    }
    public async Task<CountReadback> Resolve(CountOperation operation,CancellationToken ct=default)
    {
        CountReadback result=null!;
        await api.ReconcileAsync(async token=>
        {
            var log=await Movements(null,operation.Intent.Key,size:100,ct:token);
            if(log.Items.Any(m=>m.VariantId!=operation.Snapshot.VariantId||m.Kind!="DIEU_CHINH"))throw new AdminApiException("INVALID_STATE",409);
            var stock=await StockRow(operation.Snapshot.VariantId,operation.Snapshot.Sku,token);var applied=log.Total>0;var unchanged=stock.Version==operation.Snapshot.Version;
            result=new(stock,applied,!applied&&unchanged);
        },ct);
        operation.NeedsReadback=false;operation.Finished=!result.RetryAllowed;return result;
    }
}

public sealed class PurchaseDraftSession(IAdminApi api,long id=0)
{
    private readonly SupplyWorkspace workspace=new(api);
    public Guid RequestKey { get; }=Guid.NewGuid();
    public long Id { get; private set; }=id;
    public PurchaseRecord? Current { get; private set; }
    public PurchaseRequest? PendingCreation { get; private set; }
    public bool NeedsReadback { get; private set; }
    public async Task<PurchaseReadback> Load(CancellationToken ct=default)
    {
        if(Id<=0)throw new AdminApiException("UNCERTAIN");PurchaseReadback result=null!;
        if(api.NeedsReconciliation)await api.ReconcileAsync(async token=>result=await workspace.Readback(Id,token),ct);else result=await workspace.Readback(Id,ct);
        Current=result.Purchase;NeedsReadback=false;PendingCreation=null;return result;
    }
    public async Task<PurchaseReadback> Save(long supplier,IReadOnlyList<PurchaseLineRequest> lines,CancellationToken ct=default)
    {
        if(NeedsReadback||PendingCreation is not null||api.NeedsReconciliation)throw new AdminApiException("READBACK_REQUIRED");
        var input=new PurchaseRequest(supplier,lines.ToArray(),Id==0?null:Current?.Version,Id==0?RequestKey:Guid.Empty);
        if(Id==0)PendingCreation=input;
        try{if(Id==0)Id=await api.CreateAsync("/api/admin/purchases",input,ct);else await api.PutAsync($"/api/admin/purchases/{Id}",input,ct);NeedsReadback=true;return await Load(ct);}
        catch(AdminApiException){if(!api.NeedsReconciliation&&Id==0)PendingCreation=null;throw;}
    }
    public async Task<PurchaseReadback> ResolveCreation(CancellationToken ct=default)
    {
        if(Id!=0||PendingCreation is null)throw new AdminApiException("INVALID_STATE",409);
        Id=await api.RetryPurchaseCreateAsync(PendingCreation,ct);NeedsReadback=true;return await Load(ct);
    }
    public void Adopt(PurchaseReadback read){Id=read.Purchase.Id;Current=read.Purchase;NeedsReadback=false;PendingCreation=null;}
    public bool Matches(long supplier,IReadOnlyList<PurchaseLineRequest> lines)=>Current is not null&&Current.SupplierId==supplier&&Current.Lines.Select(l=>new PurchaseLineRequest(l.VariantId,l.Quantity,l.Cost)).OrderBy(l=>l.VariantId).SequenceEqual(lines.OrderBy(l=>l.VariantId));
}
