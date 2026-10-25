import {spawnSync} from 'node:child_process';import fs from 'node:fs';
const script=String.raw`
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,WindowsBase
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class AdminWindowMessages {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,string l);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);
}
'@
$state=Get-Content -Raw -LiteralPath '.artifacts/qa-env/private.json' | ConvertFrom-Json
$release=[IO.Path]::GetFullPath('dist/Phone Store Admin/PhoneStore.Admin.exe')
$shortcut=Join-Path ([Environment]::GetFolderPath('Desktop')) 'Phone Store Admin.lnk'
$testDir=[IO.Path]::GetFullPath('.artifacts/Thử mở ứng dụng');New-Item -ItemType Directory -Force -Path $testDir | Out-Null
$results=New-Object System.Collections.Generic.List[object]
function Record($id,$pass){$results.Add([pscustomobject]@{id=$id;status=$(if($pass){'PASS'}else{'FAIL'})});Write-Output (($(if($pass){'PASS'}else{'FAIL'}))+' '+$id);$results | ConvertTo-Json | Set-Content -LiteralPath '.artifacts/admin-package/ui-results.json' -Encoding utf8;if(-not $pass){throw ('UI check failed: '+$id)}}
Start-Process -FilePath $shortcut -WorkingDirectory $testDir
$app=$null
for($i=0;$i -lt 80;$i++){ $app=Get-Process PhoneStore.Admin -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $release -and $_.MainWindowHandle -ne 0} | Select-Object -First 1;if($app){break};Start-Sleep -Milliseconds 250 }
Record 'UI-DESKTOP-SHORTCUT' ($null -ne $app)
$root=[System.Windows.Automation.AutomationElement]::FromHandle($app.MainWindowHandle)
$editCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Edit)
$edits=$root.FindAll([System.Windows.Automation.TreeScope]::Descendants,$editCondition)
Record 'UI-LOGIN-ONLY-TWO-INPUTS' ($edits.Count -eq 2)
$userHandle=[IntPtr]$edits[0].Current.NativeWindowHandle;$passwordHandle=[IntPtr]$edits[1].Current.NativeWindowHandle
$buttonCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Đăng nhập')
$button=$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$buttonCondition)
function TextNames { $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text))) | ForEach-Object {$_.Current.Name} }
for($i=0;$i -lt 60;$i++){if($button.Current.IsEnabled){break};Start-Sleep -Milliseconds 250}
[void][AdminWindowMessages]::SendMessage($userHandle,12,[IntPtr]::Zero,$state.adminUsername)
[void][AdminWindowMessages]::SendMessage($passwordHandle,12,[IntPtr]::Zero,'invalid-test-password')
[void][AdminWindowMessages]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,245,[IntPtr]::Zero,[IntPtr]::Zero)
$invalid=$false;for($i=0;$i -lt 80;$i++){if((TextNames) -match 'Tên đăng nhập hoặc mật khẩu không đúng'){$invalid=$true;break};Start-Sleep -Milliseconds 250}
Record 'UI-WRONG-PASSWORD-MESSAGE' $invalid
[void][AdminWindowMessages]::SendMessage($passwordHandle,12,[IntPtr]::Zero,$state.adminPassword)
[void][AdminWindowMessages]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,245,[IntPtr]::Zero,[IntPtr]::Zero)
$logged=$false;for($i=0;$i -lt 100;$i++){$app.Refresh();if($app.MainWindowTitle -like '*Quản trị cửa hàng*'){$logged=$true;break};Start-Sleep -Milliseconds 250}
Record 'UI-QA-ADMIN-LOGIN' $logged
Start-Process -FilePath $shortcut -WorkingDirectory $testDir;Start-Process -FilePath $shortcut -WorkingDirectory $testDir;Start-Sleep -Seconds 2
$instances=@(Get-Process PhoneStore.Admin -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $release})
Record 'UI-REPEATED-SHORTCUT-ONE-INSTANCE' ($instances.Count -eq 1)
[void]$app.CloseMainWindow();Start-Sleep -Seconds 2
$health=Invoke-RestMethod -Uri ($state.url+'/health/ready') -TimeoutSec 10
Record 'UI-CLOSE-KEEPS-SHARED-BACKEND' $health.ready
Start-Process -FilePath $shortcut -WorkingDirectory $testDir
Write-Output 'PASS UI-REOPEN-FOR-USER'
`;
fs.mkdirSync('.artifacts/admin-package',{recursive:true});
const r=spawnSync('powershell',['-NoProfile','-Command',script],{encoding:'utf8',windowsHide:true,timeout:120000});
for(const line of (r.stdout||'').split(/\r?\n/))if(/^(PASS|FAIL) /.test(line))console.log(line);
if(r.status!==0){console.log('FAIL UI automation; inspect locally without publishing raw credentials.');process.exitCode=1;}
