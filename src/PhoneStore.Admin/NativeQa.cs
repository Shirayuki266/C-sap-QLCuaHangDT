using System.Text.Json;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
internal static class NativeQa
{
    static IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in All(c))yield return child;}}
    public static async Task Run(MainForm main,ApiClient api,string output){
        Directory.CreateDirectory(output);var results=new List<object>();
        async Task Check(string id,string title,Func<Task> body){try{await body();results.Add(new{id,title,status="PASS",kind="WinForms controls + HTTP runtime",actual="Assertions đạt"});}catch(Exception e){results.Add(new{id,title,status="FAIL",kind="WinForms controls + HTTP runtime",actual=e is HttpRequestException?"Lỗi kết nối HTTP":e.Message});}await File.WriteAllTextAsync(Path.Combine(output,"native-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));}
        await Check("NATIVE-SCREENS","Tải/render 11 màn hình và editor",async()=>await main.Smoke(Path.Combine(output,"screens")));
        await Check("NATIVE-ADD-PURCHASE","Bấm Thêm sản phẩm → Lưu → phiếu điền sẵn → Lưu nháp",async()=>{
            var code="UIQA"+DateTime.UtcNow.ToString("HHmmss");var tab=All(main).OfType<TabControl>().First();tab.SelectedIndex=1;var list=tab.SelectedTab!.Controls.OfType<ListScreen<ProductDto>>().Single();await list.RefreshData();
            var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);int stage=0;bool ticking=false;var start=DateTime.UtcNow;
            using var timer=new System.Windows.Forms.Timer{Interval=100};timer.Tick+=async(_,_)=>{if(ticking)return;ticking=true;try{
                if(DateTime.UtcNow-start>TimeSpan.FromSeconds(30))throw new Exception("Timeout khi tương tác editor; chưa đạt.");
                if(stage==0&&Application.OpenForms.OfType<ProductEditor>().FirstOrDefault() is { } product){var text=All(product).OfType<TextBox>().ToArray();text[0].Text=code;text[1].Text="Điện thoại UI QA";text[2].Text="Cấu hình QA";text[3].Text="Mô tả QA";var grid=All(product).OfType<DataGridView>().Single();grid.Rows.Add(0L,"",code+"-SKU","Đen",128,100.50m,true,2);stage=1;var save=All(product).OfType<Button>().Single(x=>x.Text.StartsWith("Lưu sản phẩm"));save.PerformClick();save.PerformClick();}
                else if(stage==1&&Application.OpenForms.OfType<PurchaseEditor>().FirstOrDefault() is { } receipt){var grid=All(receipt).OfType<DataGridView>().Single();var rows=grid.Rows.Cast<DataGridViewRow>().Where(x=>!x.IsNewRow).ToList();if(rows.Count!=1||Convert.ToInt32(rows[0].Cells["Quantity"].Value)!=2)throw new Exception("Phiếu không được điền sẵn đúng hàng/lượng");rows[0].Cells["Cost"].Value=80.25m;stage=2;All(receipt).OfType<Button>().Single(x=>x.Text=="Lưu nháp").PerformClick();}
                else if(stage==2&&!Application.OpenForms.OfType<PurchaseEditor>().Any()&&!Application.OpenForms.OfType<ProductEditor>().Any()){
                    var products=await api.Get<Page<ProductDto>>("/api/admin/products?Query="+code);if(products.Total!=1)throw new Exception("Bấm lưu lặp tạo sản phẩm trùng");var p=products.Items.Single();if(p.Variants.Single().OnHand!=0)throw new Exception("Tạo sản phẩm/nháp làm tăng kho");var receipts=await api.Get<Page<PurchaseDto>>("/api/admin/purchases");var draft=receipts.Items.Single(x=>x.Lines.Any(l=>l.VariantId==p.Variants.Single().Id));if(draft.Status!="NHAP"||draft.Total!=160.50m)throw new Exception("Sai phiếu nháp/giá vốn/tổng tiền");complete.TrySetResult();timer.Stop();
                }
            }catch(Exception ex){complete.TrySetException(ex);timer.Stop();foreach(var f in Application.OpenForms.Cast<Form>().Where(x=>x is ProductEditor or PurchaseEditor).ToArray())f.Close();}finally{ticking=false;}};
            timer.Start();list.Tools.Controls.OfType<Button>().Single(x=>x.Text=="Thêm điện thoại").PerformClick();await complete.Task.WaitAsync(TimeSpan.FromSeconds(40));
        });
        await Check("NATIVE-ASYNC","UI vẫn xử lý timer trong lúc tác vụ tải còn chờ",async()=>{
            var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var pulse=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var screen=new ListScreen<ProductDto>{Loader=async(_,_)=>{entered.TrySetResult();await release.Task;return new([],0,1,20);}};main.Controls.Add(screen);
            using var timer=new System.Windows.Forms.Timer{Interval=50};timer.Tick+=(_,_)=>pulse.TrySetResult();
            try{var loading=screen.RefreshData();await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));timer.Start();await pulse.Task.WaitAsync(TimeSpan.FromSeconds(5));if(loading.IsCompleted)throw new Exception("Tác vụ tải phải còn chờ khi timer UI chạy");release.TrySetResult();await loading;}finally{release.TrySetResult();timer.Stop();main.Controls.Remove(screen);}
        });
        await Check("NATIVE-NETWORK","WinForms xử lý kết nối lỗi và mở lại form",async()=>{using var broken=new ApiClient();broken.Connect("http://127.0.0.1:1");using var screen=new ListScreen<ProductDto>{Loader=(_,_)=>broken.Get<Page<ProductDto>>("/api/admin/products")};main.Controls.Add(screen);await screen.RefreshData();if(string.IsNullOrWhiteSpace(screen.LastError))throw new Exception("Không hiển thị trạng thái lỗi kết nối");if(!screen.LastError.Contains("Không thể kết nối"))throw new Exception("BUG-QA-006: Thông báo lỗi mạng WinForms chưa được Việt hóa.");main.Controls.Remove(screen);});
    }
}


