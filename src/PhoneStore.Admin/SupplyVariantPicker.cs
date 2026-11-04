using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class SupplyVariantPicker:Form
{
    private readonly CatalogWorkspace catalog;private int page=1,total;private bool busy;
    public SupplyChoice? Selected { get; private set; }
    public DataGridView Grid { get; }=CatalogUi.Grid();public TextBox QueryBox { get; }=AdminTheme.Input("Tìm điện thoại hoặc SKU");
    public Button SearchButton { get; }=AdminTheme.Button("Tìm kiếm");public Button ChooseButton { get; }=AdminTheme.Button("Chọn biến thể");public Button PreviousButton { get; }=AdminTheme.Button("Trang trước",false);public Button NextButton { get; }=AdminTheme.Button("Trang sau",false);public Label MessageLabel { get; }=AdminTheme.Label("");
    public SupplyVariantPicker(IAdminApi api)
    {
        catalog=new(api);AdminTheme.Frame(this,"Chọn biến thể nhập");QueryBox.Dock=DockStyle.None;QueryBox.Width=300;Grid.ReadOnly=true;foreach(var(p,t)in new[]{("Sku","SKU"),("Name","Điện thoại"),("Variant","Phiên bản"),("Cost","Giá vốn hiện tại (đ)"),("State","Kinh doanh")})CatalogUi.Column(Grid,p,t,true,180);
        CatalogUi.Layout(this,"Chọn biến thể nhập",CatalogUi.Toolbar(QueryBox,SearchButton,ChooseButton),Grid,CatalogUi.Toolbar(PreviousButton,NextButton,MessageLabel));Shown+=async(_,_)=>await LoadAsync();SearchButton.Click+=async(_,_)=>{if(busy)return;page=1;await LoadAsync();};PreviousButton.Click+=async(_,_)=>{if(!busy&&page>1){page--;await LoadAsync();}};NextButton.Click+=async(_,_)=>{if(!busy&&page*20<total){page++;await LoadAsync();}};ChooseButton.Click+=(_,_)=>{if(Grid.CurrentRow?.Tag is SupplyChoice row){Selected=row;DialogResult=DialogResult.OK;Close();}};Grid.SelectionChanged+=(_,_)=>ChooseButton.Enabled=!busy&&Grid.CurrentRow?.Tag is SupplyChoice;
    }
    public async Task LoadAsync()
    {
        if(busy)return;busy=true;SearchButton.Enabled=ChooseButton.Enabled=PreviousButton.Enabled=NextButton.Enabled=QueryBox.Enabled=Grid.Enabled=false;MessageLabel.Text="Đang tải biến thể…";
        try{var batch=await catalog.Products(QueryBox.Text,null,null,page);total=batch.Total;Grid.Rows.Clear();foreach(var p in batch.Items)foreach(var v in p.Variants){var i=Grid.Rows.Add(v.Sku,p.Name,$"{v.Color} · {v.StorageGb} GB",v.Cost,p.Active&&v.Active?"Đang kinh doanh":"Đã ngừng bán");Grid.Rows[i].Tag=new SupplyChoice(v.Id,v.Sku,p.Name,v.Cost);}MessageLabel.Text=total==0?"Không có sản phẩm phù hợp.":$"Trang {page} · {total} điện thoại · Chọn một biến thể. Giá vốn nhập có thể sửa trên phiếu.";}
        catch(Exception error){Grid.Rows.Clear();MessageLabel.Text=SupplyPresentation.Failure(error);}finally{busy=false;SearchButton.Enabled=QueryBox.Enabled=Grid.Enabled=true;ChooseButton.Enabled=Grid.CurrentRow?.Tag is SupplyChoice;PreviousButton.Enabled=page>1;NextButton.Enabled=page*20<total;}
    }
}
