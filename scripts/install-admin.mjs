import fs from 'node:fs';import path from 'node:path';import os from 'node:os';import crypto from 'node:crypto';import {fileURLToPath} from 'node:url';import {spawnSync} from 'node:child_process';
const scriptFolder=path.dirname(fileURLToPath(import.meta.url));
const release=fs.existsSync(path.join(scriptFolder,'PhoneStore.Admin.exe'))?scriptFolder:path.resolve('dist/Phone Store Admin');
const executable=path.join(release,'PhoneStore.Admin.exe');if(!fs.existsSync(executable))throw Error('Chưa có bản publish. Chạy publish-admin.mjs trước.');
const data=path.join(process.env.LOCALAPPDATA||path.join(os.homedir(),'AppData/Local'),'PhoneStore','Admin');fs.mkdirSync(data,{recursive:true});
const args=process.argv.slice(2);const value=key=>{const n=args.indexOf(key);return n>=0?args[n+1]:null;};
let connection;
if(args.includes('--qa')){
 const privateFile=value('--qa-file')||path.resolve('.artifacts/qa-env/private.json');const state=JSON.parse(fs.readFileSync(privateFile,'utf8').replace(/^\uFEFF/,''));
 if(!/^PhoneStore_QA_[A-Za-z0-9_]+$/.test(state.database)||!['localhost','127.0.0.1'].includes(new URL(state.url).hostname))throw Error('Manifest không phải QA local hợp lệ.');
 const backend=path.join(data,'backend.json');fs.writeFileSync(backend,JSON.stringify({ConnectionStrings__Store:state.connectionString,QA__Server:state.server,QA__Database:state.database},null,2));
 connection={ApiBaseUrl:state.url,Environment:'QA',TargetFingerprint:crypto.createHash('sha256').update((state.server+'|'+state.database).toLowerCase()).digest('hex'),StartLocalBackend:true,BackendExecutable:path.join(release,'backend','PhoneStore.Web.exe'),BackendConfiguration:backend};
}else if(value('--connection-file')){
 connection=JSON.parse(fs.readFileSync(value('--connection-file'),'utf8').replace(/^\uFEFF/,''));
}else if(!fs.existsSync(path.join(data,'connection.json')))throw Error('Cần --qa --qa-file <manifest cục bộ> hoặc --connection-file <cấu hình đã chuẩn bị>; không có credential mặc định.');
if(connection)fs.writeFileSync(path.join(data,'connection.json'),JSON.stringify(connection,null,2));
// COM creates a normal Windows shortcut; arguments are passed through environment, not interpolated shell code.
const result=spawnSync('powershell',['-NoProfile','-Command',"$ErrorActionPreference='Stop'; $w=New-Object -ComObject WScript.Shell; $desktop=[Environment]::GetFolderPath('Desktop'); if(-not (Test-Path -LiteralPath $desktop)){New-Item -ItemType Directory -Path $desktop | Out-Null}; $s=$w.CreateShortcut((Join-Path $desktop 'Phone Store Admin.lnk')); $s.TargetPath=$env:PHONE_ADMIN_EXE; $s.WorkingDirectory=$env:PHONE_ADMIN_DIR; $s.IconLocation=(Join-Path $env:PHONE_ADMIN_DIR 'phone-store.ico'); $s.Description='Phone Store Admin'; $s.Save(); Write-Output (Join-Path $desktop 'Phone Store Admin.lnk')"],{env:{...process.env,PHONE_ADMIN_EXE:executable,PHONE_ADMIN_DIR:release},encoding:'utf8',windowsHide:true});
if(result.status!==0)throw Error('Không tạo được shortcut; không công bố lỗi cấu hình thô.');console.log('Shortcut: '+result.stdout.trim());console.log('Cấu hình người dùng: '+path.join(data,'connection.json')+' (không lưu mật khẩu đăng nhập).');
