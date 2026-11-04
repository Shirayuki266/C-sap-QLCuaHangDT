param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
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
 public static Rect Bounds(IntPtr h){Rect r;GetWindowRect(h,out r);return r;}
 public delegate bool EnumChildProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent,EnumChildProc callback,IntPtr p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,System.Text.StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,System.Text.StringBuilder text,int size);
 public static IntPtr[] Children(IntPtr parent){var list=new System.Collections.Generic.List<IntPtr>();EnumChildWindows(parent,(h,p)=>{list.Add(h);return true;},IntPtr.Zero);return list.ToArray();}
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SendMessageTimeout(IntPtr h,uint message,IntPtr wp,System.Text.StringBuilder text,uint flags,uint timeout,out IntPtr result);
 [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int index);
 public static string ReadText(IntPtr h){if(Class(h).Contains("EDIT")&&(GetWindowLong(h,-16)&0x20)!=0)return "[password control]";if(Class(h).Contains("EDIT")&&(GetWindowLong(h,-16)&0x20)==0){var s=new System.Text.StringBuilder(16000);IntPtr result;SendMessageTimeout(h,0x000D,new IntPtr(s.Capacity),s,2,500,out result);return s.ToString();}return Text(h);}
 public static string Text(IntPtr h){var s=new System.Text.StringBuilder(1024);GetWindowText(h,s,s.Capacity);return s.ToString().Replace("&","");}
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
 [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,uint message,IntPtr wp,IntPtr lp);
 public static IntPtr Labeled(IntPtr root,string name,bool combo){
  var all=Children(root);string label=name=="NCC hoạt động"?"Nhà cung cấp đang hoạt động":name=="Lý do xử lý đơn"?"Lý do":name;
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
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
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
function Window([string]$prefix){WaitUntil {foreach($w in (Windows)){if($w.Current.Name.StartsWith($prefix) -and [NativeWalk]::Class([IntPtr]$w.Current.NativeWindowHandle).StartsWith('WINDOWSFORMS10.WINDOW')){return $w}}} "window $prefix"}
function Nodes($root){([Windows.Automation.AutomationElement]::FromHandle([IntPtr]$root.Current.NativeWindowHandle)).FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.Condition]::TrueCondition)}
function SetValue($root,[string]$name,[string]$value){[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Set native $name`r`n");$h=WaitUntil {[NativeWalk]::Labeled([IntPtr]$root.Current.NativeWindowHandle,$name,$false)} "native input $name";WaitUntil {[NativeWalk]::IsWindowEnabled($h)} "enabled input $name"|Out-Null;$node=[Windows.Automation.AutomationElement]::FromHandle($h);($node.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).SetValue($value);[IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),"Updated $name`r`n")}
function TextContains($root,[string]$text){foreach($h in [NativeWalk]::Children([IntPtr]$root.Current.NativeWindowHandle)){if([NativeWalk]::ReadText($h).Contains($text)){return $true}};return $false}
function Close($window){[NativeWalk]::PostMessage([IntPtr]$window.Current.NativeWindowHandle,0x0010,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;Start-Sleep -Milliseconds 150}

# D3 uses the actual compiled .exe and a seeded full demo, not a form fixture or fake API.
$context=Get-Content -LiteralPath $env:PHONESTORE_DEMO_CONTEXT -Raw -Encoding UTF8|ConvertFrom-Json
if($context.database -notmatch '^PhoneStore_Demo_D3_' -or $context.url -ne $env:PHONESTORE_ADMIN_QA_URL){throw 'Dedicated full-demo context mismatch'}
if(@(Get-Process -Name PhoneStore.Admin -ErrorAction SilentlyContinue).Count -ne 0){throw 'Existing Admin process must not be disturbed'}
$stockSettingsPath=Join-Path (Split-Path $settingsPath) 'stock-settings.json'
$hadStock=Test-Path -LiteralPath $stockSettingsPath
$originalStock=if($hadStock){[IO.File]::ReadAllBytes($stockSettingsPath)}else{$null}
$measurements=[Collections.Generic.List[object]]::new()
$observations=[Collections.Generic.List[object]]::new()
$stage='setup'
function Money($value){$amount=[decimal]$value;$format=if([decimal]::Truncate($amount)-eq $amount){'N0'}else{'N2'};$amount.ToString($format,[Globalization.CultureInfo]::GetCultureInfo('vi-VN'))+' đ'}
function Click($root,[string]$name){
 [IO.File]::AppendAllText((Join-Path $output 'ui-trace.txt'),('Native button input '+$name+[Environment]::NewLine))
 $button=WaitUntil {foreach($h in [NativeWalk]::Children([IntPtr]$root.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h) -and [NativeWalk]::Class($h).Contains('BUTTON') -and [NativeWalk]::Text($h)-eq $name -and [NativeWalk]::IsWindowEnabled($h)){return $h}}} ('button '+$name)
 # Post the standard button input asynchronously: a synchronous UIA Invoke blocks
 # until ShowDialog closes, preventing the observer from inspecting that dialog.
 if(-not [NativeWalk]::PostMessage($button,0x00F5,[IntPtr]::Zero,[IntPtr]::Zero)){throw ('Native button input failed: '+$name)}
 Start-Sleep -Milliseconds 180
}
function WaitText($window,[string]$value){
 try{WaitUntil {if(TextContains $window $value){return $true};@(DisplayTexts $window)|Where-Object {$_.Contains($value)}} ('render '+$value)|Out-Null}
 catch{
  @{expected=$value;title=[NativeWalk]::Text([IntPtr]$window.Current.NativeWindowHandle);class=[NativeWalk]::Class([IntPtr]$window.Current.NativeWindowHandle);texts=@(DisplayTexts $window)}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $output 'render-wait-failure.json') -Encoding UTF8
  throw
 }
}
function DisplayTexts($window){
 foreach($h in [NativeWalk]::Children([IntPtr]$window.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h)){[NativeWalk]::ReadText($h)}}
 foreach($node in (Nodes $window)){
  if($node.Current.IsPassword -or $node.Current.IsOffscreen){continue}
  $node.Current.Name
  if($node.Current.ControlType -ne [Windows.Automation.ControlType]::Edit -and $node.GetCurrentPropertyValue([Windows.Automation.AutomationElement]::IsValuePatternAvailableProperty)){
   try{($node.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).Current.Value}catch{}
  }
 }
}
function Observe($window,[string]$name,[string[]]$expected){
 foreach($value in $expected){WaitUntil {if(TextContains $window $value){return $true};@(DisplayTexts $window)|Where-Object {$_.Contains($value)}} ('SQL value '+$name)|Out-Null}
 $texts=@(DisplayTexts $window)
 foreach($value in $expected){if(-not ($texts|Where-Object {$_.Contains($value)})){throw ('Missing rendered SQL value in '+$name)}}
 $observations.Add(@{name=$name;expected=$expected;texts=$texts})
 Capture $window $name
}
function Timed($timer,[string]$name){$milliseconds=$timer.Elapsed.TotalMilliseconds;$measurements.Add(@{name=$name;milliseconds=$milliseconds});if($milliseconds -gt $context.thresholds.nativeMilliseconds){throw ('Native open threshold exceeded: '+$name)}}
function ChooseReport($window,[int]$index){
 $combo=WaitUntil {foreach($h in [NativeWalk]::Children([IntPtr]$window.Current.NativeWindowHandle)){if([NativeWalk]::IsWindowVisible($h) -and [NativeWalk]::Class($h).Contains('COMBOBOX')){return $h}}} 'report kind combo'
 [NativeWalk]::SendMessage($combo,0x014E,[IntPtr]$index,[IntPtr]::Zero)|Out-Null
 $control=[NativeWalk]::GetDlgCtrlID($combo)
 [NativeWalk]::PostMessage([NativeWalk]::GetParent($combo),0x0111,[IntPtr](($control -band 0xffff) -bor 0x10000),$combo)|Out-Null
 Start-Sleep -Milliseconds 150
}
function SetReportDate($window,[string]$name,[string]$iso){
 $dateHandles=@([NativeWalk]::Children([IntPtr]$window.Current.NativeWindowHandle)|ForEach-Object {@{handle=$_.ToInt64();class=[NativeWalk]::Class($_);visible=[NativeWalk]::IsWindowVisible($_)}})
 @{stage='report-date';name=$name;root=([IntPtr]$window.Current.NativeWindowHandle).ToInt64();appExited=$app.HasExited;children=$dateHandles}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $output 'date-picker-diagnostics.json') -Encoding UTF8
 $inputs=@(WaitUntil {$found=@([NativeWalk]::Children([IntPtr]$window.Current.NativeWindowHandle)|Where-Object {[NativeWalk]::IsWindowVisible($_) -and [NativeWalk]::Class($_).Contains('SYSDATETIMEPICK32')}|Sort-Object {([NativeWalk]::Bounds($_)).Left});if($found.Count -eq 2){return $found}} ('two native report date inputs'))
 $handle=$inputs[$(if($name.StartsWith('Từ')){0}else{1})]
 $date=[DateTime]::ParseExact($iso,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture)
 $node=[Windows.Automation.AutomationElement]::FromHandle([IntPtr]$handle)
 $mode='MSAA date value'
 try {($node.GetCurrentPattern([Windows.Automation.LegacyIAccessiblePattern]::Pattern)).SetValue($iso)}
 catch {
  $mode='keyboard date fields'
  [NativeWalk]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle)|Out-Null;$node.SetFocus()
  # Actual keyboard input; never pass a local SYSTEMTIME pointer into another process.
  # Select the day segment explicitly. DateTimePicker retains its active segment
  # between visits; Home changes that segment's value rather than resetting focus.
  $bounds=[NativeWalk]::Bounds([IntPtr]$handle)
  $point=(([int](($bounds.Bottom-$bounds.Top)/2)) -shl 16) -bor [int](($bounds.Right-$bounds.Left)*0.21)
  [NativeWalk]::SendMessage([IntPtr]$handle,0x0201,[IntPtr]1,[IntPtr]$point)|Out-Null
  [NativeWalk]::SendMessage([IntPtr]$handle,0x0202,[IntPtr]::Zero,[IntPtr]$point)|Out-Null
  # Set a safe day before the month/year, then the requested day (including 31/12
  # and leap days). Typing 31 while the old month is November would clamp it.
  [Windows.Forms.SendKeys]::SendWait('01{RIGHT}'+$date.ToString('MM')+'{RIGHT}'+$date.ToString('yyyy')+'{LEFT}{LEFT}'+$date.ToString('dd')+'{TAB}')
 }
 $accessibleValue='';try{$accessibleValue=($node.GetCurrentPattern([Windows.Automation.LegacyIAccessiblePattern]::Pattern)).Current.Value}catch{}
 @{name=$name;desired=$iso;mode=$mode;accessibleValue=$accessibleValue;appExited=$app.HasExited}|ConvertTo-Json|Add-Content (Join-Path $output 'date-input-results.txt') -Encoding UTF8
 Capture $window ('date-input-'+$(if($name.StartsWith('Từ')){'from'}else{'to'}))
}
# Print only the application window; never fall back to an arbitrary desktop screenshot.
function Capture($window,[string]$name){
 [NativeWalk]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle)|Out-Null
 Start-Sleep -Milliseconds 150
 $rect=[NativeWalk]::Bounds([IntPtr]$window.Current.NativeWindowHandle)
 $bitmap=[Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top)
 $graphics=[Drawing.Graphics]::FromImage($bitmap);$hdc=$graphics.GetHdc()
 try{$ok=[NativeWalk]::PrintWindow([IntPtr]$window.Current.NativeWindowHandle,$hdc,2)}finally{$graphics.ReleaseHdc($hdc)}
 if(-not $ok){$graphics.Dispose();$bitmap.Dispose();throw 'Application-only capture failed'}
 $bitmap.Save((Join-Path $output ($name+'.png')),[Drawing.Imaging.ImageFormat]::Png)
 $captures.Add(@{name=$name;width=$bitmap.Width;height=$bitmap.Height;processId=$app.Id;actualReleaseProcess=$true;method='Win32 UI input, UIAutomation and application-only PrintWindow'})
 $graphics.Dispose();$bitmap.Dispose()
}
try {
 New-Item -ItemType Directory -Force $output|Out-Null
 New-Item -ItemType Directory -Force (Split-Path $settingsPath)|Out-Null
 [IO.File]::WriteAllText($settingsPath,(@{BaseUrl=$context.url;ExpectedEnvironment='Testing';ExpectedFingerprint=$null}|ConvertTo-Json))
 [IO.File]::WriteAllText($stockSettingsPath,'{"Below":5}')
 $app=Start-Process -FilePath $appPath -PassThru -WindowStyle Normal
 $stage='login';$login=Window 'Đăng nhập quản trị';WaitText $login 'Testing'
 SetValue $login 'Tên đăng nhập' $env:PHONESTORE_ADMIN_QA_USERNAME
 SetValue $login 'Mật khẩu' $env:PHONESTORE_ADMIN_QA_PASSWORD
 Click $login 'Đăng nhập'
 $main=Window 'Quản trị cửa hàng';WaitText $main 'UTC+7 · tải lúc'
 $stage='dashboard'
 Observe $main 'winforms-dashboard' @((Money $context.expected.periodRevenue),(Money $context.expected.todayRevenue),($context.expected.pending.ToString()+' · tải lúc'),($context.expected.returns.ToString()+' · tải lúc'),($context.expected.lowCount.ToString()+' · tải lúc'))
 $stage='orders';$timer=[Diagnostics.Stopwatch]::StartNew();Click $main 'Đơn hàng';$orders=Window 'Đơn hàng';WaitText $orders '601 đơn';Timed $timer 'orders-first-page'
 Observe $orders 'winforms-orders' @('Trang 1 · 601 đơn')
 $timer.Restart();Click $orders 'Trang sau';WaitText $orders 'Trang 2 · 601 đơn';Timed $timer 'orders-second-page'
 Observe $orders 'winforms-orders-page2' @('Trang 2 · 601 đơn')
 SetValue $orders 'Tìm mã đơn, người nhận, khách hoặc điện thoại' $context.completedOrder.code
 Click $orders 'Tìm kiếm';WaitText $orders '1 đơn';Click $orders 'Mở chi tiết';$detail=Window 'Chi tiết đơn hàng';WaitText $detail 'Thông tin và giá snapshot'
 Observe $detail 'winforms-completed-order' @($context.completedOrder.code,'Đã thu',(Money $context.completedOrder.total))
 $stage='invoice';Click $detail 'Xem hóa đơn';$invoice=Window 'Hóa đơn bán hàng';WaitText $invoice $context.completedOrder.code
 Observe $invoice 'winforms-invoice' @($context.completedOrder.code,(Money $context.completedOrder.total))
 Close $invoice;Close $detail;Close $orders
 $stage='revenue';$main=Window 'Quản trị cửa hàng';$timer.Restart();Click $main 'Báo cáo';$report=Window 'Báo cáo';WaitText $report (Money $context.expected.weekRevenue);Timed $timer 'report-revenue'
 Observe $report 'winforms-revenue' @((Money $context.expected.weekRevenue),($context.expected.weekOrders.ToString()+' đơn hoàn tất'))
 $sample=0
 foreach($quarter in $context.quarters){
  SetReportDate $report 'Từ ngày UTC+7' $quarter.from;SetReportDate $report 'Đến hết ngày UTC+7' $quarter.to
  $timer.Restart();Click $report 'Tải báo cáo';WaitText $report (Money $quarter.revenue);Timed $timer ('quarter-report-'+$sample)
  Observe $report ('quarter-report-'+$sample) @((Money $quarter.revenue));$sample++
 }
 SetReportDate $report 'Từ ngày UTC+7' ([DateTime]::ParseExact($context.today,'yyyy-MM-dd',[Globalization.CultureInfo]::InvariantCulture).AddDays(-6).ToString('yyyy-MM-dd'))
 SetReportDate $report 'Đến hết ngày UTC+7' $context.today
 $stage='bestsellers';ChooseReport $report 1;$timer.Restart();Click $report 'Tải báo cáo';WaitText $report 'đơn hoàn tất';Timed $timer 'report-bestsellers'
 Observe $report 'winforms-bestsellers' @((Money $context.expected.weekRevenue),'SKU biến thể')
 $stage='stock-report';ChooseReport $report 2;$timer.Restart();Click $report 'Tải báo cáo';WaitText $report '125 biến thể';Timed $timer 'report-stock'
 Observe $report 'winforms-stock-report' @('125 biến thể','Không áp dụng kỳ')
 Close $report
 $stage='stock';$main=Window 'Quản trị cửa hàng';$timer.Restart();Click $main 'Tồn kho';$stock=Window 'Tồn kho';WaitText $stock '125 biến thể';Timed $timer 'stock-list'
 Observe $stock 'winforms-stock' @('125 biến thể')
 Click $stock 'Nhật ký kho';$journal=Window 'Nhật ký kho';WaitText $journal 'Chứng từ';Observe $journal 'stock-journal' @('Người thực hiện');Close $journal
 Close $stock
 $stage='historical-purchases';Click (Window 'Quản trị cửa hàng') 'Nhập hàng';$purchases=Window 'Nhập hàng';WaitText $purchases '56 phiếu';$sample=0
 foreach($purchase in $context.historicalPurchases){
  SetValue $purchases 'Tìm mã phiếu hoặc tên NCC' $purchase.code;Click $purchases 'Tìm kiếm';WaitText $purchases '1 phiếu';Click $purchases 'Mở phiếu'
  $receipt=Window 'Phiếu nhập hàng';WaitText $receipt $purchase.code;Observe $receipt ('old-purchase-'+$sample) @($purchase.code,(Money $purchase.total));Close $receipt;$sample++
 }
 Close $purchases
 $stage='historical-invoices';Click (Window 'Quản trị cửa hàng') 'Đơn hàng';$orders=Window 'Đơn hàng';WaitText $orders '601 đơn'
 $sample=0
 foreach($old in $context.historicalInvoices){
  SetValue $orders 'Tìm mã đơn, người nhận, khách hoặc điện thoại' $old.code;Click $orders 'Tìm kiếm';WaitText $orders '1 đơn';Click $orders 'Mở chi tiết'
  $detail=Window 'Chi tiết đơn hàng';WaitText $detail $old.code
  Observe $detail ('old-order-'+$sample) @($old.code,$old.name,$old.sku)
  Click $detail 'Xem hóa đơn';$invoice=Window 'Hóa đơn bán hàng';WaitText $invoice $old.code
  Observe $invoice ('old-invoice-'+$sample) @($old.code,$old.name,$old.sku,(Money $old.total))
  if($sample -eq 0){Click $invoice 'PrintPreview · Xem trước in';$preview=Window 'Xem trước in';Capture $preview 'old-invoice-print-preview';Close $preview;WaitText $invoice 'Đã đóng xem trước'}
  Close $invoice;Close $detail;$sample++
 }
 Close $orders
 $stage='real-legacy-search';Click (Window 'Quản trị cửa hàng') 'Điện thoại';$catalog=Window 'Điện thoại';WaitText $catalog 'điện thoại'
 SetValue $catalog 'Tìm mã, tên hoặc SKU' 'Samsung';Click $catalog 'Tìm kiếm';WaitText $catalog 'Samsung';Capture $catalog 'real-name-search'
 foreach($query in @('Auvora','Lumetrix','DM001')){SetValue $catalog 'Tìm mã, tên hoặc SKU' $query;Click $catalog 'Tìm kiếm';WaitText $catalog 'Không có điện thoại phù hợp';Capture $catalog ('legacy-search-'+$query)}
 Close $catalog
 $stage='logout';$main=Window 'Quản trị cửa hàng';Click $main 'Đăng xuất';$login=Window 'Đăng nhập quản trị';Close $login
 if(-not $app.WaitForExit(10000)){throw 'Release application did not exit gracefully'}
 @{result='PASS';actualReleaseApplication=$true;fakeApi=$false;agentAssisted=$true;expectedSqlMatched=$true;captures=$captures.ToArray();measurements=$measurements.ToArray();observations=$observations.ToArray();thresholdMilliseconds=$context.thresholds.nativeMilliseconds}|ConvertTo-Json -Depth 9|Set-Content (Join-Path $output 'results.json') -Encoding UTF8
 Write-Output 'PASS actual Release WinForms full-demo dashboard, order pages, revenue, bestseller/stock reports and invoice; SQL values matched'
} catch {
 $safe=$_.Exception.Message
 foreach($key in @('PHONESTORE_ADMIN_QA_PASSWORD','PHONESTORE_DEMO_ADMIN_PASSWORD','PHONESTORE_DEMO_CUSTOMER_PASSWORD')){$secret=[Environment]::GetEnvironmentVariable($key);if($secret){$safe=$safe.Replace($secret,'[redacted]')}}
 $titles=@();try{$titles=@((Windows)|ForEach-Object {$_.Current.Name})}catch{}
 $visible=@();try{foreach($window in (Windows)){$visible+=@((Nodes $window)|Where-Object {-not $_.Current.IsPassword -and $_.Current.ControlType.ProgrammaticName -ne 'ControlType.Edit'}|ForEach-Object {$_.Current.Name})}}catch{}
 foreach($key in @('PHONESTORE_ADMIN_QA_PASSWORD','PHONESTORE_DEMO_ADMIN_PASSWORD','PHONESTORE_DEMO_CUSTOMER_PASSWORD')){$secret=[Environment]::GetEnvironmentVariable($key);if($secret){$visible=@($visible|ForEach-Object {$_.Replace($secret,'[redacted]')})}}
 @{stage=$stage;errorType=$_.Exception.GetType().Name;message=$safe;line=$_.InvocationInfo.ScriptLineNumber;windows=$titles;visible=$visible}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $output 'failure.json') -Encoding UTF8
 Write-Error ('D3 native smoke failed at '+$stage+'; credential values withheld')
 exit 1
} finally {
 [NativeWalk]::SetThreadDpiAwarenessContext($previousDpi)|Out-Null
 if($app -and -not $app.HasExited){try{foreach($window in (Windows)){Close $window}}catch{};if(-not $app.WaitForExit(3000)){$app.Kill()}}
 if($hadSettings){[IO.File]::WriteAllBytes($settingsPath,$originalSettings)}elseif(Test-Path -LiteralPath $settingsPath){Remove-Item -LiteralPath $settingsPath}
 if($hadStock){[IO.File]::WriteAllBytes($stockSettingsPath,$originalStock)}elseif(Test-Path -LiteralPath $stockSettingsPath){Remove-Item -LiteralPath $stockSettingsPath}
}
