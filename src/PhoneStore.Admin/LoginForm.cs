using PhoneStore.Core.Contracts;
namespace PhoneStore.Admin;
public sealed class LoginForm:Form
{
    public ApiClient Api {get;}=new();
    public UserDto? User {get;private set;}
    public LoginForm() {
        Text="Phone Store · Đăng nhập quản trị";Size=new(560,430);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;Font=new("Segoe UI",11);
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28),ColumnCount=1,RowCount=8};Controls.Add(table);
        var username=new TextBox{Dock=DockStyle.Top,MaxLength=100};var password=new TextBox{Dock=DockStyle.Top,UseSystemPasswordChar=true,MaxLength=128};
        var error=new Label{AutoSize=true,ForeColor=Color.Firebrick,MaximumSize=new(470,0)};var button=new Button{Text="Đăng nhập",AutoSize=true,Padding=new(12,6,12,6)};
        var settings=new LinkLabel{Text="Cài đặt kết nối",AutoSize=true};
        foreach(var control in new Control[]{new Label{Text="Tên đăng nhập",AutoSize=true},username,new Label{Text="Mật khẩu",AutoSize=true},password,button,error,settings})table.Controls.Add(control);
        settings.LinkClicked+=(_,_)=>{try{using var dialog=new ConnectionSettingsForm(AdminSettings.Load());if(dialog.ShowDialog(this)==DialogResult.OK)error.Text="Đã lưu cấu hình kết nối.";}catch(Exception){error.Text="Không đọc được cấu hình. Chạy thiết lập ban đầu hoặc cài lại ứng dụng.";}};
        AcceptButton=button;button.Click+=async(_,_)=>{button.Enabled=false;settings.Enabled=false;error.Text="Đang kiểm tra kết nối…";try{
            var connection=AdminSettings.Load();await BackendLauncher.EnsureReady(connection);Api.Connect(connection.ApiBaseUrl);
            error.Text="Đang xác thực…";User=await Api.Send<UserDto>("/api/auth/login?admin=true",new LoginRequest(username.Text,password.Text));DialogResult=DialogResult.OK;Close();
        }catch(Exception ex){error.ForeColor=Color.Firebrick;error.Text=ex is IOException or System.Text.Json.JsonException?"Không đọc được cấu hình kết nối. Hãy chạy thiết lập ban đầu.":ex.Message;password.Clear();}finally{button.Enabled=true;settings.Enabled=true;}};
        Shown+=async(_,_)=>{button.Enabled=false;try{error.Text="Đang kiểm tra máy chủ…";await BackendLauncher.EnsureReady(AdminSettings.Load());error.Text="Máy chủ sẵn sàng.";error.ForeColor=Color.DarkGreen;}catch(Exception ex){error.Text=ex is IOException or System.Text.Json.JsonException?"Chưa có cấu hình hợp lệ. Hãy chạy thiết lập ban đầu.":ex.Message;error.ForeColor=Color.Firebrick;}finally{button.Enabled=true;username.Focus();}};
    }
}
