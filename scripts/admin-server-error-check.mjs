import fs from 'node:fs';import path from 'node:path';import {spawn} from 'node:child_process';import {createServer} from 'node:http';
const target='a'.repeat(64);const server=createServer((req,res)=>{res.setHeader('Content-Type','application/json');if(req.url==='/health/ready')res.end(JSON.stringify({application:'PhoneStore',environment:'QA',target,ready:true}));else if(req.url==='/api/security/token')res.end(JSON.stringify({token:'test-only-no-real-credential'}));else{res.statusCode=500;res.end(JSON.stringify({code:'SERVER_ERROR',message:'Máy chủ gặp lỗi thử nghiệm. Vui lòng thử lại sau.'}));}});
await new Promise(r=>server.listen(0,'127.0.0.1',r));const work=path.resolve('.artifacts/admin-package/error-ui');fs.mkdirSync(work,{recursive:true});
fs.writeFileSync(work+'/connection.json',JSON.stringify({ApiBaseUrl:'http://127.0.0.1:'+server.address().port,Environment:'QA',TargetFingerprint:target,StartLocalBackend:false,BackendExecutable:'',BackendConfiguration:''}));
const script=String.raw`
$ErrorActionPreference='Stop';Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,WindowsBase
Add-Type @'
using System;using System.Runtime.InteropServices;public static class ErrorUiMessages{[DllImport("user32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,string l);[DllImport("user32.dll")]public static extern bool PostMessage(IntPtr h,int m,IntPtr w,IntPtr l);}
'@
$exe=[IO.Path]::GetFullPath('dist/Phone Store Admin/PhoneStore.Admin.exe');$app=$null;$pass=$false
try{
 $existing=Get-Process PhoneStore.Admin -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe}
 foreach($p in $existing){if($p.MainWindowTitle -notlike '*Đăng nhập*'){throw 'Existing user session is active; do not close'};[void]$p.CloseMainWindow()};Start-Sleep -Seconds 1
 Start-Process -FilePath $exe
 for($i=0;$i -lt 80;$i++){$app=Get-Process PhoneStore.Admin -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe -and $_.MainWindowHandle -ne 0} | Select-Object -First 1;if($app){break};Start-Sleep -Milliseconds 250}
 if(-not $app){throw 'No test window'};$root=[System.Windows.Automation.AutomationElement]::FromHandle($app.MainWindowHandle)
 $edits=$root.FindAll([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Edit)))
 $button=$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Đăng nhập')))
 for($i=0;$i -lt 40;$i++){if($button.Current.IsEnabled){break};Start-Sleep -Milliseconds 250}
 [void][ErrorUiMessages]::SendMessage([IntPtr]$edits[0].Current.NativeWindowHandle,12,[IntPtr]::Zero,'test-user');[void][ErrorUiMessages]::SendMessage([IntPtr]$edits[1].Current.NativeWindowHandle,12,[IntPtr]::Zero,'test-password')
 [void][ErrorUiMessages]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,245,[IntPtr]::Zero,[IntPtr]::Zero)
 for($i=0;$i -lt 60;$i++){$names=$root.FindAll([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Text))) | ForEach-Object {$_.Current.Name};if($names -match 'Máy chủ gặp lỗi thử nghiệm'){$pass=$true;break};Start-Sleep -Milliseconds 250}
 if(-not $pass){throw 'Expected server error label not shown'};Write-Output 'PASS UI-SERVER-500-DISTINCT'
}finally{if($app){[void]$app.CloseMainWindow();Start-Sleep -Seconds 1};Remove-Item Env:PHONESTORE_ADMIN_DATA -ErrorAction SilentlyContinue;Start-Process -FilePath (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Phone Store Admin.lnk')}
if(-not $pass){exit 1}
`;
const code=await new Promise((resolve,reject)=>{const p=spawn('powershell',['-NoProfile','-Command',script],{env:{...process.env,PHONESTORE_ADMIN_DATA:work},stdio:['ignore','pipe','ignore'],windowsHide:true});p.stdout.on('data',b=>{for(const line of b.toString().split(/\r?\n/))if(line.startsWith('PASS '))console.log(line);});p.on('error',reject);p.on('exit',resolve);});
server.closeAllConnections();await new Promise(r=>server.close(r));fs.writeFileSync('.artifacts/admin-package/server-error-ui.json',JSON.stringify({id:'UI-SERVER-500-DISTINCT',status:code===0?'PASS':'FAIL',actual:'Test HTTP500 with dummy credentials; real QA config unmodified'}));if(code!==0)process.exitCode=1;
