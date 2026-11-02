namespace PhoneStore.Admin.Client;

public sealed record ReportDay(DateOnly Day,int Orders,decimal Revenue);
public sealed record ReportBestseller(long VariantId,string Name,string Sku,long Quantity,decimal Revenue);
public sealed record ReportStock(long VariantId,string Name,string Sku,int OnHand,int Reserved,int Available);
public sealed record AdminReport(string Kind,string TimeZone,string? From,string? To,decimal Revenue,long Quantity,int Orders,List<ReportDay> Daily,List<ReportBestseller> Bestsellers,List<ReportStock> Stock);
public sealed record ReportRow(string Key,string Name,long Quantity,int Orders,decimal Revenue,int OnHand=0,int Reserved=0,int Available=0);
public sealed record ReportPoint(string Label,decimal Value,string Display);
public sealed record ReportProjection(AdminReport Source,List<ReportRow> Rows,List<ReportPoint> Points);
public sealed record LoadedMetric<T>(T Value,DateTimeOffset LoadedAt);
public static class ReportPresentation
{
    public static DateOnly Today(DateTimeOffset? now=null)=>DateOnly.FromDateTime((now??DateTimeOffset.UtcNow).ToOffset(TimeSpan.FromHours(7)).DateTime);
    public static string? DateError(string kind,DateOnly? from,DateOnly? to)=>kind=="stock"?null:!from.HasValue||!to.HasValue?"Chọn đủ từ ngày và đến ngày (UTC+7).":from>to?"Từ ngày không được sau đến ngày.":null;
    public static string Kind(string kind)=>kind switch{"revenue"=>"Doanh thu","bestsellers"=>"Bán chạy theo biến thể","stock"=>"Tồn hiện tại",_=>kind};
    public static string Summary(AdminReport report)=>report.Kind=="stock"?$"Tồn thực từ máy chủ: {report.Quantity:N0} · {report.Stock.Count} biến thể · Không áp dụng kỳ":$"Doanh thu: {AdminFormatting.Money(report.Revenue)} · {report.Orders} đơn hoàn tất · {report.Quantity} sản phẩm · {report.TimeZone}";
    // Group only the daily projection; the displayed overall total remains Source.Revenue.
    public static ReportProjection Project(AdminReport source,bool monthly=false)
    {
        List<ReportRow> rows=source.Kind switch
        {
            "revenue"=>monthly?source.Daily.GroupBy(d=>d.Day.ToString("yyyy-MM",System.Globalization.CultureInfo.InvariantCulture)).OrderBy(g=>g.Key).Select(g=>new ReportRow(g.Key,"",0,g.Sum(d=>d.Orders),g.Sum(d=>d.Revenue))).ToList():source.Daily.OrderBy(d=>d.Day).Select(d=>new ReportRow(AdminFormatting.Date(d.Day),"",0,d.Orders,d.Revenue)).ToList(),
            "bestsellers"=>source.Bestsellers.Select(d=>new ReportRow(d.Sku,d.Name,d.Quantity,0,d.Revenue)).ToList(),
            "stock"=>source.Stock.Select(d=>new ReportRow(d.Sku,d.Name,0,0,0,d.OnHand,d.Reserved,d.Available)).ToList(),
            _=>throw new AdminApiException("INVALID_RESPONSE")
        };
        var points=rows.Select(r=>source.Kind=="revenue"?new ReportPoint(r.Key,r.Revenue,AdminFormatting.Money(r.Revenue)):source.Kind=="bestsellers"?new ReportPoint(r.Key,r.Quantity,$"{r.Quantity} sản phẩm · {AdminFormatting.Money(r.Revenue)}"):new ReportPoint(r.Key,r.Available,$"{r.Available} có thể bán · tồn {r.OnHand} · giữ {r.Reserved}")).ToList();return new(source,rows,points);
    }
    public static List<ReportPoint> SevenDays(AdminReport report,DateOnly through)=>Enumerable.Range(0,7).Select(i=>{var day=through.AddDays(i-6);var value=report.Daily.SingleOrDefault(d=>d.Day==day)?.Revenue??0;return new ReportPoint(AdminFormatting.Date(day),value,AdminFormatting.Money(value));}).ToList();
}
public sealed class ReportsWorkspace(IAdminApi api,Func<DateTimeOffset>? clock=null)
{
    private DateTimeOffset Now()=>clock?.Invoke()??DateTimeOffset.UtcNow;
    public Task<AdminReport> Read(string kind,DateOnly? from=null,DateOnly? to=null,CancellationToken ct=default)
    {
        if(kind is not("revenue" or "bestsellers" or "stock")||ReportPresentation.DateError(kind,from,to) is not null)throw new AdminApiException("INVALID_INPUT",400);
        var path="/api/admin/reports?kind="+kind+(kind=="stock"?"":"&from="+AdminFormatting.Date(from!.Value)+"&to="+AdminFormatting.Date(to!.Value));return api.GetAsync<AdminReport>(path,ct);
    }
    private async Task<LoadedMetric<T>> Load<T>(Func<Task<T>> read)=>new(await read(),Now());
    public Task<LoadedMetric<int>> Orders(string status,CancellationToken ct=default)=>Load(async()=> (await new OrdersWorkspace(api).List(null,status,null,null,1,1,ct)).Total);
    public Task<LoadedMetric<int>> Unreceived(CancellationToken ct=default)=>Load(async()=>
    {
        var seen=new HashSet<long>();var result=0;
        for(var page=1;;page++)
        {
            var batch=await new OrdersWorkspace(api).List(null,"GIAO_THAT_BAI",null,null,page,100,ct);
            if(batch.Items.Any(r=>r.Status!="GIAO_THAT_BAI"))throw new AdminApiException("INVALID_RESPONSE");
            foreach(var row in batch.Items)if(seen.Add(row.Id)&&!row.ReturnReceived)result++;
            if(page*100>=batch.Total)return result;if(batch.Items.Count==0)throw new AdminApiException("INVALID_RESPONSE");
        }
    });
    public Task<LoadedMetric<AdminReport>> Revenue(DateOnly from,DateOnly to,CancellationToken ct=default)=>Load(()=>Read("revenue",from,to,ct));
    public Task<LoadedMetric<int>> LowStock(int threshold,CancellationToken ct=default)=>Load(async()=>{if(threshold<0)throw new AdminApiException("INVALID_INPUT",400);return (await new SupplyWorkspace(api).Stock(null,null,threshold,1,1,ct)).Total;});
}
