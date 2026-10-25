import fs from 'node:fs';import {spawnSync} from 'node:child_process';import path from 'node:path';
const root=path.resolve('dist/Phone Store Admin');fs.mkdirSync(root,{recursive:true});
for(const [project,folder] of [['src/PhoneStore.Admin',root],['src/PhoneStore.Web',path.join(root,'backend')]]){
 const r=spawnSync('dotnet',['publish',project,'-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=false','-o',folder],{stdio:'inherit',windowsHide:true});if(r.status!==0)process.exit(r.status||1);
}
// QA source configuration and credentials are never release assets.
for(const folder of [root,path.join(root,'backend')])for(const name of fs.readdirSync(folder))if(/private\.json|appsettings\.Development\.json|appsettings\.QA\.json/.test(name))fs.unlinkSync(path.join(folder,name));
fs.copyFileSync('src/PhoneStore.Admin/Assets/phone-store.ico',path.join(root,'phone-store.ico'));
fs.copyFileSync('scripts/install-admin.mjs',path.join(root,'install-admin.mjs'));
fs.copyFileSync('docs/ADMIN_WINDOWS.md',path.join(root,'HUONG_DAN.md'));
console.log('Portable release: '+root);
