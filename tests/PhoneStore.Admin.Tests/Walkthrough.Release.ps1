param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class NativeWalk {
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumChildProc callback,IntPtr p);
 [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 public struct Rect {public int Left,Top,Right,Bottom;}
 public static IntPtr[] TopWindows(int pid){var list=new System.Collections.Generic.List<IntPtr>();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==pid&&IsWindowVisible(h))list.Add(h);return true;},IntPtr.Zero);return list.ToArray();}
 public static Rect Bounds(IntPtr h){SetThreadDpiAwarenessContext(new IntPtr(-4));Rect r;GetWindowRect(h,out r);return r;}
 // Post the standard button notification, so a modal can open without blocking a UIA RPC.
 public static void ClickButton(IntPtr h){PostMessage(GetParent(h),0x0111,new IntPtr(GetDlgCtrlID(h)&0xffff),h);}
 public delegate bool EnumChildProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent,EnumChildProc callback,IntPtr p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,System.Text.StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,System.Text.StringBuilder text,int size);
 public static IntPtr[] Children(IntPtr parent){var list=new System.Collections.Generic.List<IntPtr>();EnumChildWindows(parent,(h,p)=>{list.Add(h);return true;},IntPtr.Zero);return list.ToArray();}
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SendMessageTimeout(IntPtr h,uint message,IntPtr wp,System.Text.StringBuilder text,uint flags,uint timeout,out IntPtr result);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageTimeoutW")] private static extern IntPtr SetTextMessage(IntPtr h,uint message,IntPtr wp,string text,uint flags,uint timeout,out IntPtr result);
 public static void SetText(IntPtr h,string value){if(!IsWindowEnabled(h)||(GetWindowLong(h,-16)&0x0800)!=0)throw new InvalidOperationException("Input not editable");IntPtr result;if(SetTextMessage(h,0x000C,IntPtr.Zero,value,2,2000,out result)==IntPtr.Zero)throw new InvalidOperationException("Input message timed out");}
 [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
 public static string ReadText(IntPtr h){if(Class(h).Contains("EDIT")&&(GetWindowLong(h,-16)&0x20)==0){var s=new System.Text.StringBuilder(16000);IntPtr result;SendMessageTimeout(h,0x000D,new IntPtr(s.Capacity),s,2,500,out result);return s.ToString();}return Text(h);}
 public static string Text(IntPtr h){var s=new System.Text.StringBuilder(1024);GetWindowText(h,s,s.Capacity);return s.ToString().Replace("&","");}
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h,uint flags);
 [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,uint message,IntPtr wp,IntPtr lp);
 public static IntPtr Labeled(IntPtr root,string name,bool combo){
  var all=Children(root);string label=name=="NCC hoạt động"?"Nhà cung cấp đang hoạt động":(name=="Lý do xử lý đơn"||name=="Lý do kiểm kê")?"Lý do":name;
  IntPtr target=IntPtr.Zero;int score=int.MaxValue;
  foreach(var h in all){if(!IsWindowVisible(h)||Class(h).Contains("EDIT")||!Text(h).StartsWith(label,StringComparison.Ordinal))continue;var r=Bounds(h);
   foreach(var input in all){var type=Class(input);if(!IsWindowVisible(input)||GetParent(input)!=GetParent(h)||(combo?!type.Contains("COMBOBOX"):!type.Contains("EDIT")))continue;var b=Bounds(input);if(b.Top<r.Bottom-4||Math.Abs(b.Left-r.Left)>35)continue;int s=b.Top-r.Bottom+Math.Abs(b.Left-r.Left);if(s<score){score=s;target=input;}}
  }
  if(target!=IntPtr.Zero)return target;
  if(name.StartsWith("Tìm",StringComparison.Ordinal)){foreach(var input in all)if(IsWindowVisible(input)&&Class(input).Contains("EDIT"))return input;}
  return IntPtr.Zero;
 }
 public static string Class(IntPtr h){var s=new System.Text.StringBuilder(200);GetClassName(h,s,s.Capacity);return s.ToString().ToUpperInvariant();}
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd,uint msg,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd,IntPtr hdc,uint flags);
 [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from,uint to,bool attach);
 [DllImport("user32.dll")] public static extern IntPtr SetActiveWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr h);
 public static void Activate(IntPtr parent,IntPtr control){uint pid;var target=GetWindowThreadProcessId(parent,out pid);var current=GetCurrentThreadId();var attached=current!=target&&AttachThreadInput(current,target,true);try{SetForegroundWindow(parent);SetActiveWindow(parent);SetFocus(control);}finally{if(attached)AttachThreadInput(current,target,false);}}
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
}
'@
$previousDpi=[NativeWalk]::SetThreadDpiAwarenessContext([IntPtr](-4))
$appPath=$env:PHONESTORE_WALK_APP
$signals=$env:PHONESTORE_WALK_SIGNALS
$output=$env:PHONESTORE_WALK_OUTPUT
if(-not $appPath -or -not $signals -or -not $output -or $env:PHONESTORE_ADMIN_QA_URL -notmatch '^http://127\.0\.0\.1:\d+$'){throw 'Explicit QA walkthrough configuration required'}
$settingsPath=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'PhoneStore.Admin/settings.json'
$hadSettings=Test-Path -LiteralPath $settingsPath
$originalSettings=if($hadSettings){[IO.File]::ReadAllBytes($settingsPath)}else{$null}
$app=$null;$captures=[Collections.Generic.List[object]]::new()
function WaitUntil([scriptblock]$action,[string]$tag){$until=[DateTime]::UtcNow.AddSeconds(35);while([DateTime]::UtcNow -lt $until){try{$value=&$action;if($value -is [IntPtr]){if($value -ne [IntPtr]::Zero){return $value}}elseif($value){return $value}}catch{};Start-Sleep -Milliseconds 100};throw "UI wait failed: $tag"}
function Windows(){foreach($h in [NativeWalk]::TopWindows($app.Id)){$r=[NativeWalk]::Bounds($h);[pscustomobject]@{Current=[pscustomobject]@{Name=[NativeWalk]::Text($h);NativeWindowHandle=$h;BoundingRectangle=[pscustomobject]@{X=$r.Left;Y=$r.Top;Width=$r.Right-$r.Left;Height=$r.Bottom-$r.Top}}}}}
function Window([string]$prefix){WaitUntil {foreach($w in (Windows)){if($w.Current.Name.StartsWith($prefix)){return $w};foreach($h in [NativeWalk]::Children([IntPtr]$w.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h) -and [NativeWalk]::Class($h).StartsWith("WINDOWSFORMS10.WINDOW") -and [NativeWalk]::Text($h).StartsWith($prefix)){return [pscustomobject]@{Current=[pscustomobject]@{Name=[NativeWalk]::Text($h);NativeWindowHandle=$h}}}}}} "page or dialog $prefix"}
function Nodes($root){([Windows.Automation.AutomationElement]::FromHandle([IntPtr]$root.Current.NativeWindowHandle)).FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.Condition]::TrueCondition)}
function Find($root,[string]$name,[string]$type=''){WaitUntil {foreach($h in [NativeWalk]::Children([IntPtr]$root.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h) -and [NativeWalk]::Class($h) -match 'SysTabControl32'){return ([Windows.Automation.AutomationElement]::FromHandle($h)).FindFirst([Windows.Automation.TreeScope]::Children,[Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,$name))}}} "tab $name"}
function Click($root,[string]$name){[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"NativeClick $name start`r`n");$parent=[IntPtr]$root.Current.NativeWindowHandle;[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Native parent $parent`r`n");$handle=WaitUntil {foreach($h in [NativeWalk]::Children($parent)){if([NativeWalk]::Class($h) -match 'BUTTON' -and [NativeWalk]::Text($h) -eq $name){return $h}}} "native button $name";[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Native found $name handle=$handle`r`n");WaitUntil {[NativeWalk]::IsWindowEnabled($handle)} "enabled $name"|Out-Null; [NativeWalk]::ClickButton($handle);Start-Sleep -Milliseconds 180}
function SetValue($root,[string]$name,[string]$value){$node=WaitUntil {foreach($node in (Nodes $root)){if($node.Current.Name -eq $name -and $node.Current.ControlType -eq [Windows.Automation.ControlType]::Edit){return $node}}} "input $name";$h=[IntPtr]$node.Current.NativeWindowHandle;WaitUntil {[NativeWalk]::IsWindowEnabled($h)} "editable $name"|Out-Null;[NativeWalk]::SetText($h,$value);[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Updated $name`r`n")}
function ChooseFirst($root,[string]$name){$node=WaitUntil {foreach($node in (Nodes $root)){if($node.Current.Name -eq $name -and $node.Current.ControlType -eq [Windows.Automation.ControlType]::ComboBox){return $node}}} "enabled combo $name";$h=[IntPtr]$node.Current.NativeWindowHandle;WaitUntil {[NativeWalk]::SendMessage($h,0x0146,[IntPtr]::Zero,[IntPtr]::Zero).ToInt64() -gt 0} "choices $name"|Out-Null;[NativeWalk]::SendMessage($h,0x014E,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;$control=[NativeWalk]::GetDlgCtrlID($h);[NativeWalk]::PostMessage([NativeWalk]::GetParent($h),0x0111,[IntPtr](($control -band 0xffff) -bor 0x10000),$h)|Out-Null;Start-Sleep -Milliseconds 120}
function Tab($root,[string]$name){$tab=Find $root $name 'TabItem';($tab.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)).Select();Start-Sleep -Milliseconds 100}
function TextContains($root,[string]$text){foreach($h in [NativeWalk]::Children([IntPtr]$root.Current.NativeWindowHandle)){if([NativeWalk]::ReadText($h).Contains($text)){return $true}};return $false}
function GridCell($root,[string]$header,[int]$row=0){WaitUntil {foreach($node in (Nodes $root)){if($node.Current.IsOffscreen){continue};if($node.Current.Name.StartsWith($header+' ') -and $node.Current.ControlType.ProgrammaticName -ne 'ControlType.HeaderItem'){try{$pattern=$node.GetCurrentPattern([Windows.Automation.GridItemPattern]::Pattern);if($pattern.Current.Row -eq $row){return $node}}catch{}}}} "grid cell $header row$row"}
function SetCell($root,[string]$header,[string]$value){$cell=GridCell $root $header;try{($cell.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).SetValue($value)}catch{($cell.GetCurrentPattern([Windows.Automation.LegacyIAccessiblePattern]::Pattern)).SetValue($value)}}
function Close($window){$h=[IntPtr]$window.Current.NativeWindowHandle;if([NativeWalk]::GetAncestor($h,2) -ne $h){WaitUntil {if(-not [NativeWalk]::IsWindowVisible($h)){return $true};$main=Window "Quản trị cửa hàng";foreach($button in [NativeWalk]::Children([IntPtr]$main.Current.NativeWindowHandle)){if([NativeWalk]::Text($button) -eq "Quay lại" -and [NativeWalk]::Class($button) -match "BUTTON" -and [NativeWalk]::IsWindowEnabled($button)){[NativeWalk]::ClickButton($button);break}};Start-Sleep -Milliseconds 150;return (-not [NativeWalk]::IsWindowVisible($h))} "Back completed for original page"|Out-Null;return};[NativeWalk]::PostMessage($h,0x0010,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;WaitUntil {-not [NativeWalk]::IsWindowVisible($h)} "modal closed"|Out-Null}
function SetNumber($root,[string]$name,[double]$value){$node=WaitUntil {foreach($node in (Nodes $root)){if($node.Current.Name -eq $name -and $node.Current.ControlType -eq [Windows.Automation.ControlType]::Edit){return $node}}} "number edit $name";[NativeWalk]::SetText([IntPtr]$node.Current.NativeWindowHandle,[string]$value)}
function Capture($window,[string]$name){[NativeWalk]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle)|Out-Null;Start-Sleep -Milliseconds 180;$live=[NativeWalk]::Bounds([IntPtr]$window.Current.NativeWindowHandle);$rect=[pscustomobject]@{X=$live.Left;Y=$live.Top;Width=$live.Right-$live.Left;Height=$live.Bottom-$live.Top};$bitmap=[Drawing.Bitmap]::new([int]$rect.Width,[int]$rect.Height);$graphics=[Drawing.Graphics]::FromImage($bitmap);$hdc=$graphics.GetHdc();try{$ok=[NativeWalk]::PrintWindow([IntPtr]$window.Current.NativeWindowHandle,$hdc,2)}finally{$graphics.ReleaseHdc($hdc)};if(-not $ok){$graphics.CopyFromScreen([int]$rect.X,[int]$rect.Y,0,0,$bitmap.Size)};$bitmap.Save((Join-Path $output ($name+'.png')),[Drawing.Imaging.ImageFormat]::Png);$captures.Add(@{name=$name;width=$bitmap.Width;height=$bitmap.Height;actualReleaseProcess=$true;method='Win32 button/text messages + UIAutomation selectors/grid + PMv2 PrintWindow';processId=$app.Id});$graphics.Dispose();$bitmap.Dispose()}
function Signal([string]$stage,$window){[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Signal $stage`r`n");[IO.File]::WriteAllText((Join-Path $signals ($stage+'.ready')),'');WaitUntil {Test-Path -LiteralPath (Join-Path $signals ($stage+'.continue'))} "SQL checkpoint $stage"|Out-Null;if($stage.StartsWith('customer-order')){Click $window 'Tải tổng quan'};foreach($h in [NativeWalk]::Children([IntPtr]$window.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h) -and [NativeWalk]::Class($h) -match 'BUTTON' -and [NativeWalk]::Text($h) -in @('Tải lại / đối soát','Tải tổng quan','Tải báo cáo')){WaitUntil {[NativeWalk]::IsWindowEnabled($h)} 'readback rendered'|Out-Null}};Capture $window $stage;Write-Output "PASS actual Release UI checkpoint $stage"}
function Approve([string]$title){$dialog=Window $title;Click $dialog 'Xác nhận'}
function OpenOrder([string]$query){$main=Window 'Quản trị cửa hàng';Click $main 'Đơn hàng';$list=Window 'Đơn hàng';GridCell $list 'Mã đơn'|Out-Null;SetValue $list 'Tìm mã đơn, người nhận, khách hoặc điện thoại' $query;Click $list 'Tìm kiếm';Click $list 'Mở chi tiết';return (Window 'Chi tiết đơn hàng')}
try {
 New-Item -ItemType Directory -Force $output|Out-Null
 New-Item -ItemType Directory -Force (Split-Path $settingsPath)|Out-Null
 [IO.File]::WriteAllText($settingsPath,(@{BaseUrl=$env:PHONESTORE_ADMIN_QA_URL;ExpectedEnvironment='Testing';ExpectedFingerprint=$null}|ConvertTo-Json))
 $app=Start-Process -FilePath $appPath -PassThru -WindowStyle Normal
 $login=Window 'Đăng nhập quản trị';WaitUntil {TextContains $login 'Testing'} 'QA readiness'|Out-Null;Capture $login 'login-ready';SetValue $login 'Tên đăng nhập' $env:PHONESTORE_ADMIN_QA_USERNAME;SetValue $login 'Mật khẩu' $env:PHONESTORE_ADMIN_QA_PASSWORD;Click $login 'Đăng nhập';$main=Window 'Quản trị cửa hàng';Signal 'login' $main
 Click $main 'Điện thoại';$list=Window 'Điện thoại';WaitUntil {TextContains $list 'Không có điện thoại phù hợp.'} 'initial products loaded'|Out-Null;Click $list 'Thêm điện thoại';$product=Window 'Thêm điện thoại';WaitUntil {TextContains $product 'Nhập thông tin và ít nhất một biến thể.'} 'new product loaded'|Out-Null;SetValue $product 'Mã sản phẩm' 'QA-W6-RELEASE';SetValue $product 'Tên điện thoại' 'Điện thoại walkthrough QA';ChooseFirst $product 'Hãng';ChooseFirst $product 'Danh mục';Tab $product 'Biến thể & giá từ server';SetCell $product 'SKU *' 'QA-W6-RELEASE-A';SetCell $product 'Màu *' 'Đen';SetCell $product 'GB *' '128';SetCell $product 'Giá vốn (đ) *' '10000000';SetCell $product 'Lợi nhuận % *' '20';Click $product 'Lưu sản phẩm';WaitUntil {TextContains $product 'Đã lưu.'} 'product saved'|Out-Null;Signal 'product-created' $product
 Click $product 'Lập phiếu nhập';$purchase=Window 'Phiếu nhập hàng';ChooseFirst $purchase 'NCC hoạt động';SetCell $purchase 'Số lượng *' '10';SetCell $purchase 'Giá vốn nhập (đ) *' '8000000';Click $purchase 'Lưu nháp';WaitUntil {TextContains $purchase 'Đã lưu nháp.'} 'draft saved'|Out-Null;Signal 'draft-saved' $purchase
 Click $purchase 'Xác nhận phiếu';Approve 'Xác nhận nhập hàng';WaitUntil {TextContains $purchase 'Đã xác nhận.'} 'purchase confirmed'|Out-Null;Tab $purchase 'Tồn và giá từ máy chủ';Signal 'receipt-confirmed' $purchase;Close $purchase;Close $product;Close $list
 $main=Window 'Quản trị cửa hàng';Signal 'customer-order1' $main;$order=OpenOrder 'Walkthrough FLOW01';Click $order 'Xác nhận đơn';Approve 'Xác nhận đơn';WaitUntil {TextContains $order 'Đã xử lý.'} 'order confirmed'|Out-Null;Signal 'order1-confirmed' $order
 Click $order 'Xuất giao';Approve 'Xuất giao';WaitUntil {TextContains $order 'Đã xử lý.'} 'order dispatched'|Out-Null;Signal 'order1-dispatched' $order
 Click $order 'Hoàn tất + thu COD';Approve 'Hoàn tất + thu COD';WaitUntil {TextContains $order 'Đã xử lý.'} 'order COD collected'|Out-Null;Signal 'order1-completed' $order;Close $order;Close (Window 'Đơn hàng')
 $main=Window 'Quản trị cửa hàng';Click $main 'Báo cáo';$report=Window 'Báo cáo';WaitUntil {TextContains $report '19.200.000 đ'} 'revenue report'|Out-Null;Signal 'revenue-seen' $report;Close $report
 $main=Window 'Quản trị cửa hàng';Signal 'customer-order2' $main;$order=OpenOrder 'Walkthrough FLOW02';Click $order 'Xác nhận đơn';Approve 'Xác nhận đơn';Signal 'order2-confirmed' $order;Click $order 'Xuất giao';Approve 'Xuất giao';Signal 'order2-dispatched' $order;SetValue $order 'Lý do xử lý đơn' 'Khách không nhận hàng walkthrough QA';Click $order 'Giao thất bại';Approve 'Giao thất bại';Signal 'order2-failed' $order;SetValue $order 'Lý do xử lý đơn' '';Click $order 'Nhận lại hàng';Approve 'Nhận lại hàng';WaitUntil {TextContains $order 'Đã xử lý.'} 'return received'|Out-Null;Signal 'order2-returned' $order;Close $order;Close (Window 'Đơn hàng')
 $main=Window 'Quản trị cửa hàng';Signal 'customer-order3' $main;$order=OpenOrder 'Walkthrough CANCEL';Click $order 'Xác nhận đơn';Approve 'Xác nhận đơn';Signal 'order3-confirmed' $order;Close $order;Close (Window 'Đơn hàng');Click (Window 'Quản trị cửa hàng') 'Tổng quan';$order=OpenOrder 'Walkthrough CANCEL';Signal 'other-admin-cancelled' $order;Click $order 'Xuất giao';Approve 'Xuất giao';WaitUntil {TextContains $order 'vui lòng tải lại'} 'peer conflict reload'|Out-Null;Signal 'order3-cancelled' $order;Close $order;Close (Window 'Đơn hàng')
 $main=Window 'Quản trị cửa hàng';Click $main 'Tồn kho';$stock=Window 'Tồn kho';GridCell $stock 'SKU'|Out-Null;Click $stock 'Kiểm kê';$count=Window 'Kiểm kê tồn kho';SetNumber $count 'Số thực tế tuyệt đối' 9;SetValue $count 'Lý do kiểm kê' 'Kiểm kê FLOW05 sau điều hướng QA';Click $count 'Ghi nhận kiểm kê';Approve 'Xác nhận kiểm kê';WaitUntil {TextContains $count 'Đã ghi nhận.'} 'count readback'|Out-Null;Signal 'counted-with-navigation' $count;Close $count;Close $stock;Click (Window 'Quản trị cửa hàng') 'Tổng quan';$main=Window 'Quản trị cửa hàng';Click $main 'Tồn kho';$stock=Window 'Tồn kho';GridCell $stock 'SKU'|Out-Null;SetNumber $stock 'Ngưỡng sắp hết tường minh' 10;Click $stock 'Chỉ sắp hết, khả dụng ≤';Click $stock 'Tìm kiếm';WaitUntil {TextContains $stock 'biến thể'} 'low stock after home'|Out-Null;Signal 'low-stock-after-home' $stock;Close $stock;Click (Window 'Quản trị cửa hàng') 'Tổng quan'
 $main=Window 'Quản trị cửa hàng';Click $main 'Nhập hàng';$purchases=Window 'Nhập hàng';Click $purchases 'Lập phiếu nhập';$purchase=Window 'Phiếu nhập hàng';ChooseFirst $purchase 'NCC hoạt động';Click $purchase 'Thêm biến thể';$picker=Window 'Chọn biến thể nhập';Click $picker 'Chọn biến thể';SetCell $purchase 'Số lượng *' '7';Signal 'expire-before-save' $purchase;Click $purchase 'Lưu nháp';$warning=Window 'Nháp chưa lưu';Capture $warning 'expired-draft-warning';Click $warning 'OK';$login=Window 'Đăng nhập quản trị';Signal 'expired-returned-login' $login;Close $login
 $captures|ConvertTo-Json -Depth 5|Set-Content (Join-Path $output 'captures.json') -Encoding utf8
} catch {
 [IO.File]::WriteAllText((Join-Path $output 'failure-diagnostic.txt'),($_|Out-String).Replace($env:PHONESTORE_ADMIN_QA_PASSWORD,'[redacted]'))
 try{foreach($window in (Windows)){if($true){Capture $window 'failure';$nodes=Nodes $window;$nodes|Where-Object {-not $_.Current.IsPassword}|ForEach-Object {[pscustomobject]@{type=$_.Current.ControlType.ProgrammaticName;name=$_.Current.Name;id=$_.Current.AutomationId}}|ConvertTo-Json -Depth 3|Set-Content (Join-Path $output 'failure-tree.json') -Encoding utf8;break}}}catch{}
 Write-Error 'Actual Release UI walkthrough failed; see QA-only failure capture/tree.'
 exit 1
} finally {
 [NativeWalk]::SetThreadDpiAwarenessContext($previousDpi)|Out-Null
 if($app -and -not $app.HasExited){try{foreach($window in (Windows)){Close $window}}catch{};if(-not $app.WaitForExit(2000)){$app.Kill()}}
 if($hadSettings){[IO.File]::WriteAllBytes($settingsPath,$originalSettings)}elseif(Test-Path -LiteralPath $settingsPath){Remove-Item -LiteralPath $settingsPath}
}
