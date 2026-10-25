import fs from 'node:fs';import {spawnSync} from 'node:child_process';
fs.mkdirSync('.artifacts/qa-env',{recursive:true});
const clean={...process.env};for(const key of Object.keys(clean))if(key.startsWith('PHONESTORE_')||key.startsWith('QA__')||key==='ConnectionStrings__Store')delete clean[key];
const cases=[
 ['GUARD-LEGACY-API','dotnet',['run','--no-build','-c','Release','--project','tests/PhoneStore.Checks','--','--api'],clean,'Cần PHONESTORE_API_URL'],
 ['GUARD-LEGACY-SQL','dotnet',['run','--no-build','-c','Release','--project','tests/PhoneStore.Checks','--','--integration'],clean,'QA yêu cầu connection'],
 ['GUARD-BROWSER','node',['scripts/browser-checks.mjs'],clean,'Cần cấu hình URL'],
 ['GUARD-WEB-WRONG-DB','dotnet',['src/PhoneStore.Web/bin/Release/net10.0/PhoneStore.Web.dll'],{...clean,ASPNETCORE_ENVIRONMENT:'QA',ConnectionStrings__Store:'Server=localhost;Database=PhoneStore;Integrated Security=True',QA__Server:'localhost',QA__Database:'PhoneStore_QA_Invalid'},'Đích QA không khớp'],
 ['GUARD-NATIVE','dotnet',['src/PhoneStore.Admin/bin/Release/net10.0-windows/PhoneStore.Admin.dll','--smoke'],clean,'Cần URL QA rõ ràng']
];
const results=[];for(const [id,cmd,args,env,expected] of cases){const r=spawnSync(cmd,args,{env,encoding:'utf8',windowsHide:true,timeout:30000});const pass=r.status!==0&&!r.error&&(r.stdout+r.stderr).includes(expected);results.push({id,status:pass?'PASS':'FAIL',expected:'Từ chối trước kết nối/ghi dữ liệu khi thiếu/sai cấu hình QA',actual:pass?'Đã dừng với thông báo bảo vệ đúng; không công bố stdout/stderr thô':'Kết quả khác mong đợi; xem cục bộ',exitCode:r.status});console.log((pass?'PASS ':'FAIL ')+id);}
fs.writeFileSync('.artifacts/qa-env/safety-results.json',JSON.stringify(results,null,2));if(results.some(x=>x.status==='FAIL'))process.exitCode=1;
