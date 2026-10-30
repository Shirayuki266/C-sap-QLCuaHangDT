using System.Net;
using System.Text.Json;
using PhoneStore.Core.Commerce;
namespace PhoneStore.Checks;
internal static class ReturnReasonChecks
{
    public static async Task Run(Uri address,string connection)
    {
        using var c=new CommerceContext(connection,address,"return-reason");await c.Initialize();
        var data=await PurchaseConfirmChecks.Receive(c,await CommerceData.Create(c),100);var f=new StateMatrixFixture(c,data);
        var cases=new(string Name,string? Value,bool Omit,bool Valid)[]{
            ("missing",null,true,true),("null",null,false,true),("empty","",false,true),
            ("whitespace"," \t\r\n ",false,true),("long whitespace",new string(' ',1001),false,true),
            ("trimmed text","  Kho nhận đủ máy  ",false,true),("1000 characters",new string('x',1000),false,true),
            ("trim before length"," \t"+new string('x',1000)+" \t",false,true),("1001 rejected",new string('x',1001),false,false)};
        foreach(var item in cases)await Check.Run("A20.05 T9 optional return reason: "+item.Name,async()=>
        {
            var order=await f.CreateOrder("GIAO_THAT_BAI");var before=await f.Snapshot();var key=Guid.NewGuid();
            var body=new Dictionary<string,object?>{{"version",order.Version},{"operationKey",key}};if(!item.Omit)body["reason"]=item.Value;
            using var response=await c.AdminClient.Send($"/api/admin/orders/{order.Id}/receive-return",body);
            if(!item.Valid)
            {
                using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Check.True(response.StatusCode==HttpStatusCode.BadRequest&&json.RootElement.GetProperty("code").GetString()=="INVALID_INPUT","Oversized optional reason was accepted or returned wrong error.");await f.Unchanged(before);
                return;
            }
            Check.True(response.StatusCode==HttpStatusCode.NoContent,"Optional/trimmed reason rejected.");
            await f.AssertOrderEffect(before,order,OrderMatrixCase.All.Single(x=>x.Name=="receive-return"));
            var after=await f.Snapshot();var expected=item.Value?.Trim()??"";
            var movements=after.Movements.Where(x=>x.OrderId==order.Id&&x.Kind=="NHAN_LAI").ToList();
            var history=after.History.Where(x=>x.OrderId==order.Id&&x.From=="GIAO_THAT_BAI"&&x.To=="GIAO_THAT_BAI").ToList();
            Check.True(movements.Count==order.Lines.Count&&movements.All(x=>x.Reason==expected&&x.OperationKey==key)&&history.Count==1&&history[0].Reason==expected,"Optional reason was not normalized/persisted in both journals.");
            using var repeated=await c.AdminClient.Send($"/api/admin/orders/{order.Id}/receive-return",new VersionedCommand((await f.Read(order.Id)).Version,Guid.NewGuid()));
            Check.True(repeated.StatusCode==HttpStatusCode.Conflict,"Blank reason enabled duplicate return.");await f.Unchanged(after);
        });
    }
}
