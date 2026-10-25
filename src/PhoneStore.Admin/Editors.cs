using System.Globalization;
using System.Text.Json;
using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
public sealed class ProductEditor:Form
{
    public Dictionary<string,int> InitialQuantities {get;private set;}=[];
    public long SavedId {get;private set;}
    public ProductEditor(ApiClient api,ProductDto? product,List<LookupDto> brands,List<LookupDto> categories) {
        Text=product==null?"Thêm điện thoại mới":"Cập nhật điện thoại và biến thể";Size=new(1080,780);MinimumSize=new(900,650);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var fields=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=4,AutoSize=true,Padding=new(14)};
        TextBox Input(string label,string? value,bool multiline=false){fields.Controls.Add(new Label{Text=label,AutoSize=true,Padding=new(0,7,0,0)});var input=new TextBox{Text=value??"",Width=330,Multiline=multiline,Height=multiline?60:28,MaxLength=50000};fields.Controls.Add(input);return input;}
        var code=Input("Mã sản phẩm",product?.Code);var name=Input("Tên điện thoại",product?.Name);
        ComboBox Lookup(string label,List<LookupDto> list,int? id){fields.Controls.Add(new Label{Text=label,AutoSize=true});var combo=new ComboBox{Name=label=="Hãng"?"BrandId":"CategoryId",Width=330,DropDownStyle=ComboBoxStyle.DropDownList,DataSource=list,DisplayMember="Name",ValueMember="Id"};if(id.HasValue)combo.SelectedValue=id.Value;fields.Controls.Add(combo);return combo;}
        var brand=Lookup("Hãng",brands,product?.BrandId);var category=Lookup("Danh mục",categories,product?.CategoryId);Shown+=(_,_)=>{if(product!=null){brand.SelectedValue=product.BrandId;category.SelectedValue=product.CategoryId;}};var specs=Input("Cấu hình",product?.Specifications,true);var description=Input("Mô tả",product?.Description,true);var image=Input("URL ảnh HTTPS / /images/",product?.ImageUrl);fields.Controls.Add(new Label{Text="Đang kinh doanh",AutoSize=true});var active=new CheckBox{Checked=product?.Active??true,Text="Có"};fields.Controls.Add(active);
        var extraImages=Input("Ảnh bổ sung HTTPS (mỗi dòng một URL)",string.Join(Environment.NewLine,product?.Images?.OrderBy(x=>x.SortOrder).Select(x=>x.Url)??[]),true);
        var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,RowHeadersWidth=30};
        Ui.MoneyCells(grid);grid.Columns.Add("Id","ID");grid.Columns["Id"]!.Visible=false;grid.Columns.Add("Version","Version");grid.Columns["Version"]!.Visible=false;
        grid.Columns.Add("Sku","SKU");grid.Columns.Add("Color","Màu sắc");grid.Columns.Add("StorageGb","Dung lượng GB");grid.Columns.Add("Price","Giá bán");grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Active",HeaderText="Đang bán"});grid.Columns.Add("InitialQuantity","Lượng cần nhập");
        if(product!=null)foreach(var v in product.Variants)grid.Rows.Add(v.Id,v.Version,v.Sku,v.Color,v.StorageGb,v.Price,v.Active,0);
        grid.DefaultValuesNeeded+=(_,e)=>{e.Row.Cells["Id"].Value=0L;e.Row.Cells["Active"].Value=true;e.Row.Cells["InitialQuantity"].Value=0;};
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100,Padding=new(14),FlowDirection=FlowDirection.LeftToRight,WrapContents=true};
        var save=new Button{Text="Lưu sản phẩm và lập phiếu nhập nếu có",AutoSize=true};var error=new Label{AutoSize=true,MaximumSize=new(950,0),ForeColor=Color.Firebrick};bottom.Controls.Add(save);bottom.Controls.Add(error);
        var note=new Label{Dock=DockStyle.Bottom,Height=44,Text="Giá bán tách biệt giá vốn. Thêm sản phẩm tạo tồn = 0. Lượng cần nhập chỉ chuyển sang phiếu nháp. Không xóa biến thể cũ: bỏ chọn Đang bán.",Padding=new(14,4,14,4)};
        Controls.Add(grid);Controls.Add(note);Controls.Add(bottom);Controls.Add(fields);
        save.Click+=async(_,_)=>{save.Enabled=false;error.Text="Đang lưu…";try{
            grid.EndEdit();var variants=new List<VariantInput>();var quantities=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow)continue;string Cell(string key)=>Convert.ToString(row.Cells[key].Value,CultureInfo.CurrentCulture)??"";
                if(!int.TryParse(Cell("StorageGb"),out var storage)||!decimal.TryParse(Cell("Price"),out var price))throw new InvalidOperationException("Dung lượng và giá bán phải là số hợp lệ.");
                var id=long.TryParse(Cell("Id"),out var parsed)?parsed:0;variants.Add(new(id,Cell("Sku"),Cell("Color"),storage,price,row.Cells["Active"].Value is true,Cell("Version")));
                var amount=string.IsNullOrWhiteSpace(Cell("InitialQuantity"))?0:int.Parse(Cell("InitialQuantity"));if(amount<0||amount>10000)throw new InvalidOperationException("Lượng cần nhập phải từ 0 đến 10.000.");if(amount>0)quantities.Add(Cell("Sku").Trim(),amount);
            }
            if(brand.SelectedValue==null||category.SelectedValue==null)throw new InvalidOperationException("Cần hãng và danh mục.");
            var input=new ProductInput(code.Text,name.Text,(int)brand.SelectedValue,(int)category.SelectedValue,specs.Text,description.Text,image.Text,active.Checked,variants,extraImages.Lines.Where(x=>!string.IsNullOrWhiteSpace(x)).Select((x,i)=>new ProductImageInput(x.Trim(),i)).ToList());
            var result=await api.Send<JsonElement>(product==null?"/api/admin/products":$"/api/admin/products/{product.Id}",input,product==null?HttpMethod.Post:HttpMethod.Put);
            SavedId=result.GetProperty("id").GetInt64();InitialQuantities=quantities;DialogResult=DialogResult.OK;Close();
        }catch(Exception e){error.Text=e.Message;}finally{save.Enabled=true;}};
    }
}
public sealed class PurchaseEditor:Form
{
    public PurchaseEditor(ApiClient api,List<SupplierDto> suppliers,List<StockDto> variants,PurchaseDto? receipt=null,Dictionary<long,int>? initial=null) {
        Text=receipt==null?"Lập phiếu nhập nháp":"Chi tiết phiếu nhập "+receipt.Code;Size=new(1100,700);MinimumSize=new(850,550);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=85,Padding=new(14)};header.Controls.Add(new Label{Text="Nhà cung cấp",AutoSize=true});
        var supplier=new ComboBox{Width=420,DropDownStyle=ComboBoxStyle.DropDownList,DataSource=suppliers.Where(x=>x.Active||x.Id==receipt?.SupplierId).ToList(),DisplayMember="Name",ValueMember="Id"};if(receipt!=null)supplier.SelectedValue=receipt.SupplierId;Shown+=(_,_)=>{if(receipt!=null)supplier.SelectedValue=receipt.SupplierId;};header.Controls.Add(supplier);
        header.Controls.Add(new Label{Text=receipt==null?"Phiếu nháp chưa tăng tồn.":$"{Labels.Status(receipt.Status)} · Lập: {receipt.Creator} · Xác nhận: {receipt.Confirmer}",AutoSize=true});
        
        var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White};
        Ui.MoneyCells(grid);grid.Columns.Add(new DataGridViewComboBoxColumn{Name="VariantId",HeaderText="Biến thể / SKU",DataSource=variants.Select(x=>new{Id=x.VariantId,Name=$"{x.Sku} · {x.Name} · {x.Color} · {x.StorageGb} GB"}).ToList(),ValueMember="Id",DisplayMember="Name",FillWeight=250});
        grid.Columns.Add("Quantity","Số lượng nhập");grid.Columns.Add("Cost","Đơn giá vốn");grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Total",HeaderText="Thành tiền",ReadOnly=true});grid.Columns["Total"]!.DefaultCellStyle.Format="N0";
        if(receipt!=null)foreach(var line in receipt.Lines)grid.Rows.Add(line.VariantId,line.Quantity,line.Cost,line.Total);
        if(initial!=null)foreach(var line in initial)grid.Rows.Add(line.Key,line.Value,0m,0m);
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=100,Padding=new(14),WrapContents=true};var total=new Label{Text="Tổng phiếu: 0 ₫",AutoSize=true,Padding=new(8)};var save=new Button{Text="Lưu nháp",AutoSize=true};var cancel=new Button{Text="Đóng",AutoSize=true};var error=new Label{AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new(1000,0)};bottom.Controls.AddRange([total,save,cancel,error]);cancel.Click+=(_,_)=>Close();
        void Recalculate(){decimal sum=0;foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow)continue;if(int.TryParse(row.Cells["Quantity"].Value?.ToString(),out var q)&&decimal.TryParse(row.Cells["Cost"].Value?.ToString(),out var c)){row.Cells["Total"].Value=q*c;sum+=q*c;}}total.Text=$"Tổng phiếu: {sum.FormatMoney()} ₫";}
        grid.CellEndEdit+=(_,_)=>Recalculate();grid.RowsRemoved+=(_,_)=>Recalculate();grid.DataError+=(_,e)=>{e.ThrowException=false;error.Text="Hãy chọn biến thể và nhập số hợp lệ.";};
        if(receipt!=null&&receipt.Status!="NHAP"){grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;supplier.Enabled=false;save.Enabled=false;}
        save.Click+=async(_,_)=>{save.Enabled=false;try{grid.EndEdit();var lines=new List<PurchaseLineInput>();foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow)continue;if(!long.TryParse(row.Cells["VariantId"].Value?.ToString(),out var v)||!int.TryParse(row.Cells["Quantity"].Value?.ToString(),out var q)||!decimal.TryParse(row.Cells["Cost"].Value?.ToString(),out var c))throw new InvalidOperationException("Biến thể, số lượng hoặc giá vốn không hợp lệ.");lines.Add(new(v,q,c));}
            if(supplier.SelectedValue==null)throw new InvalidOperationException("Cần chọn nhà cung cấp.");
            await api.Send<System.Text.Json.JsonElement>(receipt==null?"/api/admin/purchases":$"/api/admin/purchases/{receipt.Id}",new PurchaseInput((long)supplier.SelectedValue,lines,receipt?.Version),receipt==null?HttpMethod.Post:HttpMethod.Put);DialogResult=DialogResult.OK;Close();
        }catch(Exception e){error.Text=e.Message;}finally{save.Enabled=true;}};
        Controls.Add(grid);Controls.Add(bottom);Controls.Add(header);Recalculate();
    }
}




