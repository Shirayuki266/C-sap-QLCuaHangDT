namespace PhoneStore.Admin;
public sealed class ConnectionSettingsForm:Form
{
    public ConnectionSettingsForm(AdminSettings current){
        Text="Phone Store · Cài đặt kết nối";Size=new(650,430);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",10);
        var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new(20),AutoScroll=true};Controls.Add(panel);
        TextBox Field(string label,string value){panel.Controls.Add(new Label{Text=label,AutoSize=true});var input=new TextBox{Text=value,Width=570};panel.Controls.Add(input);return input;}
        var url=Field("Địa chỉ API",current.ApiBaseUrl);var environment=Field("Môi trường",current.Environment);var target=Field("Mã xác minh CSDL (từ thiết lập ban đầu)",current.TargetFingerprint);
        var local=new CheckBox{Text="Tự khởi động backend local đã publish",Checked=current.StartLocalBackend,AutoSize=true};panel.Controls.Add(local);
        var error=new Label{AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new(570,0)};var save=new Button{Text="Lưu kết nối",AutoSize=true};panel.Controls.Add(save);panel.Controls.Add(error);
        save.Click+=(_,_)=>{try{(current with{ApiBaseUrl=url.Text.Trim().TrimEnd('/'),Environment=environment.Text.Trim(),TargetFingerprint=target.Text.Trim(),StartLocalBackend=local.Checked}).Save();DialogResult=DialogResult.OK;Close();}catch(Exception ex){error.Text=ex.Message;}};
    }
}
