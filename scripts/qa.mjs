import fs from 'node:fs';
import {spawn} from 'node:child_process';
const action=process.argv[2];
if(!['init','serve','test','legacy','cleanup','verify','demo'].includes(action))throw Error('Usage: node scripts/qa.mjs init|serve|test|legacy|cleanup|verify|demo');
const config=JSON.parse(fs.readFileSync('qa/settings.json','utf8'));
if(config.environment!=='QA'||config.databasePrefix!=='PhoneStore_QA_')throw Error('QA config không hợp lệ.');
async function run(command,args,env=process.env,log){await new Promise((resolve,reject)=>{if(log){fs.mkdirSync('.artifacts/qa',{recursive:true});fs.writeFileSync(log,'');}const p=spawn(command,args,{stdio:log?['inherit','pipe','pipe']:'inherit',env,windowsHide:true});if(log)for(const [stream,target] of [[p.stdout,process.stdout],[p.stderr,process.stderr]])stream.on('data',chunk=>{target.write(chunk);fs.appendFileSync(log,chunk);});p.on('error',reject);p.on('exit',code=>code===0?resolve():reject(Error(command+' failed; exit '+code)));});}
if(action==='test'){
 await run('dotnet',['restore','PhoneStore.slnx'],process.env,'.artifacts/qa/restore.txt');
 await run('dotnet',['build','PhoneStore.slnx','-c','Release','--no-restore'],process.env,'.artifacts/qa/build.txt');
 fs.writeFileSync('.artifacts/qa/results.json','[]');
 let failed;try{await run('dotnet',['run','--no-build','-c','Release','--project','tests/PhoneStore.Qa','--','--children']);}catch(error){failed=error;if(JSON.parse(fs.readFileSync('.artifacts/qa/results.json','utf8')).length===0)fs.writeFileSync('.artifacts/qa/results.json',JSON.stringify([{Id:'QA-STARTUP',Module:'QA',Title:'Runner startup',Expected:'Khởi động trên đích QA hợp lệ',Actual:'Runner từ chối/không khởi động; xem lỗi cục bộ, không dùng kết quả cũ.',Status:'FAIL',Kind:'Process runtime',Evidence:'Lệnh qa.mjs test trong phiên hiện tại'}]));}
 await run('node',['scripts/qa-static.mjs']);await run('node',['scripts/qa-report.mjs']);
 if(failed)throw failed;
}else if(action==='legacy'){
 const state=JSON.parse(fs.readFileSync('.artifacts/qa-env/private.json','utf8'));
 if(state.server!==config.server||!/^PhoneStore_QA_[A-Za-z0-9_]+$/.test(state.database)||!['localhost','127.0.0.1'].includes(new URL(state.url).hostname))throw Error('Manifest không khớp đích QA.');
 const env={...process.env,PHONESTORE_API_URL:state.url,PHONESTORE_TEST_CONNECTION:state.connectionString,PHONESTORE_QA_SERVER:state.server,PHONESTORE_QA_DATABASE:state.database,PHONESTORE_ADMIN_PASSWORD:state.adminPassword,PHONESTORE_CUSTOMER_PASSWORD:state.customerPassword};
 await run('dotnet',['run','--no-build','-c','Release','--project','tests/PhoneStore.Checks','--','--integration','--api'],env);
}else{
 if(action==='init'||action==='demo')await run('dotnet',['build','PhoneStore.slnx','-c','Release']);
 await run('dotnet',['run','--no-build','-c','Release','--project','tests/PhoneStore.Qa','--',action==='demo'?'--demo-data':'--'+action]);
}
