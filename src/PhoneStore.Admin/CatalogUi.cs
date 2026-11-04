namespace PhoneStore.Admin;

internal static class CatalogUi
{
    public static DataGridView Grid() => new()
    {
        Dock=DockStyle.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,AutoGenerateColumns=false,
        AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
        MultiSelect=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize,ScrollBars=ScrollBars.Both
    };
    public static void Column(DataGridView grid,string property,string title,bool readOnly=false,int width=100)
    {
        DataGridViewColumn column=property=="Active"?new DataGridViewCheckBoxColumn():new DataGridViewTextBoxColumn();
        column.DataPropertyName=column.Name=property;column.HeaderText=title;column.ReadOnly=readOnly;column.MinimumWidth=width; grid.Columns.Add(column);
        if(property is "Cost" or "Price"){column.DefaultCellStyle.Format="N0";column.DefaultCellStyle.FormatProvider=System.Globalization.CultureInfo.GetCultureInfo("vi-VN");column.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;column.DefaultCellStyle.NullValue="Sau khi lưu";}
    }
    public static FlowLayoutPanel Toolbar(params Control[] controls)
    {
        var bar=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Padding=new Padding(12)};
        bar.Controls.AddRange(controls);return bar;
    }
    public static TableLayoutPanel Layout(Form form,string title,Control toolbar,DataGridView grid,Control footer)
    {
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(16)};
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(AdminTheme.Label(title,22,true),0,0);layout.Controls.Add(toolbar,0,1);layout.Controls.Add(grid,0,2);layout.Controls.Add(footer,0,3);form.Controls.Add(layout);return layout;
    }
    public static bool Confirm(IWin32Window owner,string title,string description)
    {using var dialog=new ConfirmationDialog(title,description);return dialog.ShowDialog(owner)==DialogResult.OK;}
}
