using System.ComponentModel;
using PhoneStore.Admin.Client;

namespace PhoneStore.Admin;

public sealed class ProductEditor : Form
{
    private readonly CatalogWorkspace workspace;
    private readonly IAdminApi api;
    private readonly Func<string,string,bool> confirm;
    private bool busy;
    private ProductRecord? original;
    private List<LookupRow> brands=[],categories=[];
    public ProductRecord? Saved { get; private set; }
    public BindingList<VariantDraft> Variants { get; private set; }=new();
    public TextBox CodeBox { get; }=AdminTheme.Input("Mã sản phẩm");
    public TextBox NameBox { get; }=AdminTheme.Input("Tên điện thoại");
    public TextBox SpecificationsBox { get; }=AdminTheme.Input("Thông số");
    public TextBox DescriptionBox { get; }=AdminTheme.Input("Mô tả");
    public TextBox ImageBox { get; }=AdminTheme.Input("Ảnh chính tùy chọn");
    public TextBox ImagesBox { get; }=new(){Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,AccessibleName="Ảnh bổ sung, mỗi dòng một URL",Height=260,MaxLength=13000};
    public ComboBox BrandBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName="Hãng"};
    public ComboBox CategoryBox { get; }=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName="Danh mục"};
    public CheckBox ActiveBox { get; }=new(){Text="Đang kinh doanh",Checked=true,AutoSize=true};
    public DataGridView VariantGrid { get; }=CatalogUi.Grid();
    public Button AddVariantButton { get; }=AdminTheme.Button("Thêm biến thể",false);
    public Button SaveButton { get; }=AdminTheme.Button("Lưu sản phẩm");
    public Button ReloadButton { get; }=AdminTheme.Button("Tải lại / đối soát",false);
    public Button PurchaseButton { get; }=AdminTheme.Button("Lập phiếu nhập · W4",false);
    public Label MessageLabel { get; }=AdminTheme.Label("");
    public event Action<PurchaseHandoff>? PurchaseRequested;
    public long ProductId { get; }
    public ProductEditor(IAdminApi api,long id=0,Func<string,string,bool>? confirmation=null)
    {
        this.api=api;workspace=new(api);ProductId=id;confirm=confirmation??((t,m)=>CatalogUi.Confirm(this,t,m));AdminTheme.Frame(this,id==0?"Thêm điện thoại":"Sửa điện thoại");
        CodeBox.MaxLength=30;NameBox.MaxLength=200;SpecificationsBox.MaxLength=20000;DescriptionBox.MaxLength=50000;ImageBox.MaxLength=1000;
        SpecificationsBox.Multiline=DescriptionBox.Multiline=true;SpecificationsBox.Height=75;DescriptionBox.Height=110;
        var tabs=new TabControl{Dock=DockStyle.Fill};var general=new TabPage("Thông tin chung");var variantTab=new TabPage("Biến thể & giá từ server");var imageTab=new TabPage("Ảnh");tabs.TabPages.AddRange([general,variantTab,imageTab]);
        var form=AdminTheme.Stack(24);form.Dock=DockStyle.Top;general.AutoScroll=true;general.Controls.Add(form);
        AdminTheme.Field(form,"Mã sản phẩm *",CodeBox);AdminTheme.Field(form,"Tên điện thoại *",NameBox);AdminTheme.Add(form,AdminTheme.Label("Hãng *"));AdminTheme.Add(form,BrandBox);AdminTheme.Add(form,AdminTheme.Label("Danh mục *"));AdminTheme.Add(form,CategoryBox);AdminTheme.Field(form,"Thông số",SpecificationsBox);AdminTheme.Field(form,"Mô tả",DescriptionBox);AdminTheme.Add(form,ActiveBox);
        var vg=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1};vg.RowStyles.Add(new RowStyle(SizeType.AutoSize));vg.RowStyles.Add(new RowStyle(SizeType.Percent,100));vg.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        vg.Controls.Add(CatalogUi.Toolbar(AddVariantButton,AdminTheme.Label("Giá bán từ máy chủ · Không xóa biến thể; dùng cột Kinh doanh")),0,0);vg.Controls.Add(VariantGrid,0,1);vg.Controls.Add(AdminTheme.Label("Giá vốn cũ chỉ đọc. Lượng gợi ý chỉ dùng cho bước W4; chưa lưu phiếu, chưa tăng tồn."),0,2);variantTab.Controls.Add(vg);
        foreach(var (property,title,read) in new[]{("Sku","SKU *",false),("Color","Màu *",false),("StorageGb","GB *",false),("Cost","Giá vốn (đ) *",false),("ProfitPercent","Lợi nhuận % *",false),("Price","Giá bán server (đ)",true),("Active","Kinh doanh",false),("OnHand","Tồn thực",true),("Reserved","Đang giữ",true),("Available","Có thể bán",true),("SuggestedQuantity","Lượng gợi ý *",false),("Version","Version",true)})CatalogUi.Column(VariantGrid,property,title,read,property=="Version"?140:100);
        VariantGrid.CellBeginEdit+=(_,e)=>{if(e.ColumnIndex==VariantGrid.Columns["Cost"]!.Index&&VariantGrid.Rows[e.RowIndex].DataBoundItem is VariantDraft{Id:>0})e.Cancel=true;};
        foreach(DataGridViewColumn column in VariantGrid.Columns)column.MinimumWidth=column.Name is "Price" or "ProfitPercent" or "Cost" or "Version" or "Available"?150:120;
        VariantGrid.CellFormatting+=(_,e)=>{if(e.RowIndex>=0&&VariantGrid.Columns[e.ColumnIndex].Name=="Cost"&&VariantGrid.Rows[e.RowIndex].DataBoundItem is VariantDraft{Id:>0})VariantGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly=true;};
        VariantGrid.DataError+=(_,e)=>{e.ThrowException=false;MessageLabel.Text="Dữ liệu biến thể chưa hợp lệ. Dung lượng/lượng gợi ý là số nguyên; giá vốn/tỷ lệ là số.";};
        var imageForm=AdminTheme.Stack(24);imageForm.Dock=DockStyle.Top;imageTab.Controls.Add(imageForm);AdminTheme.Field(imageForm,"Ảnh chính · HTTPS hoặc /images/ · có thể để trống",ImageBox);AdminTheme.Add(imageForm,AdminTheme.Label("Ảnh bổ sung · tối đa 12 URL, mỗi dòng một ảnh; thứ tự từ trên xuống"));AdminTheme.Add(imageForm,ImagesBox);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(20)};root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.Controls.Add(AdminTheme.Label(id==0?"Thêm điện thoại":"Sửa điện thoại",22,true),0,0);root.Controls.Add(tabs,0,1);root.Controls.Add(MessageLabel,0,2);root.Controls.Add(CatalogUi.Toolbar(SaveButton,ReloadButton,PurchaseButton),0,3);Controls.Add(root);PurchaseButton.Enabled=false;
        AddVariantButton.Click+=(_,_)=>Variants.Add(new());SaveButton.Click+=async(_,_)=>await SaveAsync();ReloadButton.Click+=async(_,_)=>await LoadAsync();Shown+=async(_,_)=>await LoadAsync();
        PurchaseButton.Click+=(_,_)=>{if(Saved is not null)PurchaseRequested?.Invoke(Handoff());};
    }
    public PurchaseHandoff Handoff()=>new(Saved?.Id??0,Variants.Where(v=>v.Id>0).Select(v=>new PurchaseSuggestion(v.Id,v.Sku,v.SuggestedQuantity)).ToArray());
    public ProductRequest Input()=>new(CodeBox.Text,NameBox.Text,(BrandBox.SelectedItem as LookupRow)?.Id??0,(CategoryBox.SelectedItem as LookupRow)?.Id??0,SpecificationsBox.Text,DescriptionBox.Text,ImageBox.Text,ActiveBox.Checked,Variants.Select(v=>v.Request()).ToList(),
        ImagesBox.Text.Length==0?[]:ImagesBox.Text.Replace("\r","").Split('\n').Select((url,i)=>new ProductImage(url,i)).ToList());
    public async Task LoadAsync()
    {
        if(busy)return;if(original is not null&&!api.NeedsReconciliation&&!confirm("Tải lại dữ liệu","Các thay đổi chưa lưu sẽ được thay bằng dữ liệu máy chủ. Tiếp tục?"))return;SetBusy(true);
        try
        {
            brands=await workspace.Lookups("brands");categories=await workspace.Lookups("categories");
            ProductRecord? product=original;
            if(api.NeedsReconciliation)
            {
                product=await workspace.ReconcileProduct(original?.Id??ProductId,CodeBox.Text);
                if(product is null){MessageLabel.Text="Đã đối soát: chưa tìm thấy sản phẩm theo mã. Giữ nguyên bản nhập; bạn có thể kiểm tra rồi lưu lại.";return;}
            }
            else if((workspace.PendingProductId??original?.Id??ProductId)>0)product=await workspace.Product(workspace.PendingProductId??original?.Id??ProductId);
            Bind(product);MessageLabel.Text=product is null?"Nhập thông tin và ít nhất một biến thể. Giá bán sẽ có sau khi máy chủ lưu.":"Đã tải dữ liệu và version từ máy chủ.";
        }
        catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    private void Bind(ProductRecord? product)
    {
        original=Saved=product;BrandBox.DataSource=brands.Where(b=>b.Active||b.Id==product?.BrandId).ToList();CategoryBox.DataSource=categories.Where(c=>c.Active||c.Id==product?.CategoryId).ToList();
        if(product is not null){CodeBox.Text=product.Code;NameBox.Text=product.Name;BrandBox.SelectedItem=brands.SingleOrDefault(b=>b.Id==product.BrandId);CategoryBox.SelectedItem=categories.SingleOrDefault(c=>c.Id==product.CategoryId);SpecificationsBox.Text=product.Specifications;DescriptionBox.Text=product.Description;ImageBox.Text=product.ImageUrl;ImagesBox.Text=string.Join(Environment.NewLine,product.Images.OrderBy(i=>i.SortOrder).Select(i=>i.Url));ActiveBox.Checked=product.Active;}
        Variants=new(CatalogForms.Drafts(product));VariantGrid.DataSource=Variants;
    }
    public async Task SaveAsync()
    {
        if(busy)return;if(!VariantGrid.EndEdit()){MessageLabel.Text="Hoàn tất sửa ô biến thể trước khi lưu.";return;}
        var input=Input();var invalid=CatalogForms.Validate(input,Variants.Select(v=>v.SuggestedQuantity));if(invalid is not null){MessageLabel.Text=invalid;return;}
        var variantStateChanged=original is not null&&original.Variants.Any(v=>input.Variants.Any(i=>i.Id==v.Id&&i.Active!=v.Active));
        if(original is not null&&(original.Active!=input.Active||variantStateChanged)&&!confirm("Thay đổi kinh doanh","Xác nhận thay đổi trạng thái sản phẩm hoặc biến thể? Dữ liệu và lịch sử được giữ nguyên."))return;
        SetBusy(true);try{var product=await workspace.Save(original?.Id??ProductId,input);Bind(product);MessageLabel.Text="Đã lưu. Giá bán, tồn và version đã tải lại từ máy chủ.";}catch(Exception error){MessageLabel.Text=AdminTheme.Failure(error);}finally{SetBusy(false);}
    }
    private void SetBusy(bool value){busy=value;SaveButton.Enabled=ReloadButton.Enabled=AddVariantButton.Enabled=VariantGrid.Enabled=CodeBox.Enabled=NameBox.Enabled=BrandBox.Enabled=CategoryBox.Enabled=SpecificationsBox.Enabled=DescriptionBox.Enabled=ImageBox.Enabled=ImagesBox.Enabled=ActiveBox.Enabled=!value;PurchaseButton.Enabled=false;if(value)MessageLabel.Text="Đang xử lý…";}
}
