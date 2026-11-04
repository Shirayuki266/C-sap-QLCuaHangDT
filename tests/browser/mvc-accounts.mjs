// Local Windows browser evidence. Only a generated PhoneStore_QA_MvcAccounts_* DB is created/dropped.
// Run after the Release build: node tests/browser/mvc-accounts.mjs
import { createRequire } from 'node:module';
import { spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import net from 'node:net';
import fs from 'node:fs';
import path from 'node:path';
const require=createRequire(import.meta.url);
const {chromium}=require(process.env.PHONESTORE_PLAYWRIGHT_MODULE || 'playwright');
const root=process.cwd(),web=path.join(root,'src/PhoneStore.Web');
const name='PhoneStore_QA_MvcAccounts_'+randomUUID().replaceAll('-','');
if(!/^PhoneStore_QA_MvcAccounts_[a-f0-9]{32}$/.test(name))throw Error('Invalid owned QA identifier');
const server=process.env.PHONESTORE_BROWSER_QA_SERVER || 'localhost';
const connection=`Server=${server};Database=${name};Integrated Security=True;Encrypt=True;TrustServerCertificate=True`;
const evidence=path.join(root,'.artifacts/mvc-accounts/browser');fs.mkdirSync(evidence,{recursive:true});
const password='BrowserMvcQa@12345',replacement='BrowserMvcQa@98765';
const childEnv={...process.env,ConnectionStrings__Store:connection,ASPNETCORE_ENVIRONMENT:'Testing',DOTNET_ENVIRONMENT:'Testing'};
for(const key of ['PHONESTORE_ADMIN_USERNAME','PHONESTORE_ADMIN_PASSWORD','PHONESTORE_DEMO_ADMIN_PASSWORD','PHONESTORE_DEMO_CUSTOMER_PASSWORD'])delete childEnv[key];
function command(executable,args,options={}){return new Promise((resolve,reject)=>{
 const child=spawn(executable,args,{windowsHide:true,stdio:['ignore','pipe','pipe'],...options});let stdout='',stderr='';
 child.stdout.on('data',x=>stdout+=x);child.stderr.on('data',x=>stderr+=x);child.on('error',reject);
 child.on('close',code=>code===0?resolve(stdout):reject(Error(`${path.basename(executable)} exited ${code}; raw process output withheld`)));
});}
const sql=query=>command('sqlcmd',['-S',server,'-E','-C','-b','-h','-1','-W','-Q',query]);
const count=async table=>Number((await sql(`SET NOCOUNT ON;SELECT COUNT(*) FROM [${name}].dbo.[${table}];`)).trim());
const records=[],errors=[];let owned=false,host,browser,hostOutput='';
function assert(value,message){if(!value)throw Error(message);}
try{
 await sql(`CREATE DATABASE [${name}];`);owned=true;
 await command('dotnet',[path.join(web,'bin/Release/net10.0/PhoneStore.Web.dll'),'--migrate'],{cwd:web,env:childEnv});
 const lease=net.createServer();await new Promise(r=>lease.listen(0,'127.0.0.1',r));const port=lease.address().port;await new Promise(r=>lease.close(r));const url=`http://127.0.0.1:${port}`;
 host=spawn('dotnet',[path.join(web,'bin/Release/net10.0/PhoneStore.Web.dll'),'--urls',url],{cwd:web,env:childEnv,windowsHide:true,stdio:['ignore','pipe','pipe']});
 host.stdout.on('data',x=>hostOutput+=x);host.stderr.on('data',x=>hostOutput+=x);
 let ready=false;for(let i=0;i<150;i++){try{if((await fetch(url+'/health/ready')).ok){ready=true;break;}}catch{}await new Promise(r=>setTimeout(r,100));}assert(ready,'QA host did not become ready');
 browser=await chromium.launch({headless:true});const context=await browser.newContext({viewport:{width:1440,height:1000}});const page=await context.newPage();page.on('pageerror',e=>errors.push(e.message));
 async function capture(label,route,session=context){
  for(const viewport of [{name:'desktop',width:1440,height:1000},{name:'mobile',width:390,height:844}]){
   const view=await session.newPage({viewport});await view.setViewportSize(viewport);await view.goto(url+route,{waitUntil:'networkidle'});
   const metrics=await view.evaluate(()=>({width:innerWidth,document:document.documentElement.scrollWidth,unlabelled:[...document.querySelectorAll('input:not([type=hidden])')].filter(x=>!x.labels?.length).length}));
   assert(metrics.document<=metrics.width&&metrics.unlabelled===0,'Overflow or unlabelled field '+label+viewport.name);
   await view.screenshot({path:path.join(evidence,`${label}-${viewport.name}.png`),fullPage:true});records.push({screen:label,viewport:viewport.width,...metrics});await view.close();
  }
 }
 await capture('register','/Account/Register');await capture('login','/Account/Login');
 await page.goto(url+'/Account/Register');
 const fields={Username:'browser-account',Password:password,FullName:'Khách kiểm thử trình duyệt',Phone:'0939123401',Email:'',Address:'Địa chỉ QA'};
 for(const [key,value] of Object.entries(fields))await page.locator(`[name="${key}"]`).fill(value);
 let registrationPosts=0;page.on('request',request=>{if(request.method()==='POST'&&new URL(request.url()).pathname==='/Account/Register')registrationPosts++;});
 const registerBusy=await page.evaluate(()=>{const form=document.querySelector('.account-form');form.requestSubmit();form.requestSubmit();return{busy:form.getAttribute('aria-busy'),disabled:form.querySelector('button').disabled,label:form.querySelector('button').textContent};});await page.waitForURL('**/Account/Login');
 assert(registerBusy.busy==='true'&&registerBusy.disabled&&registerBusy.label==='Đang xử lý…','Registration processing state missing');
 assert(registrationPosts===1&&await count('TAI_KHOAN')===1&&await count('KHACH_HANG')===1&&await count('PHIEN')===0,'Register double submit or auto-login failed');
 const duplicate=await browser.newContext();const duplicatePage=await duplicate.newPage();await duplicatePage.goto(url+'/Account/Register');
 for(const [key,value] of Object.entries(fields))await duplicatePage.locator(`[name="${key}"]`).fill(value);
 await duplicatePage.locator('.account-form button[type=submit]').click();await duplicatePage.locator('.validation-summary-errors').waitFor();
 assert(await duplicatePage.locator('[name=Password]').inputValue()===''&&await duplicatePage.locator('[name=FullName]').inputValue()===fields.FullName,'Duplicate form lost safe data or echoed password');
 await duplicatePage.setViewportSize({width:390,height:844});await duplicatePage.screenshot({path:path.join(evidence,'register-duplicate-mobile.png'),fullPage:true});
 await page.goto(url+'/Account/Login?returnUrl=%2FAccount%2FProfile');await page.locator('[name=Username]').fill(fields.Username);await page.locator('[name=Password]').fill(password);
 let loginPosts=0;page.on('request',request=>{if(request.method()==='POST'&&new URL(request.url()).pathname==='/Account/Login')loginPosts++;});
 const loginBusy=await page.evaluate(()=>{const form=document.querySelector('.account-form');form.requestSubmit();form.requestSubmit();return{busy:form.getAttribute('aria-busy'),disabled:form.querySelector('button').disabled,label:form.querySelector('button').textContent};});await page.waitForURL('**/Account/Profile');
 assert(loginBusy.busy==='true'&&loginBusy.disabled&&loginBusy.label==='Đang xử lý…','Login processing state missing');
 assert(loginPosts===1&&await count('PHIEN')===1,'Login double submit created multiple sessions');const loginDoubleSubmitPosts=loginPosts;
 await capture('profile','/Account/Profile');await capture('password','/Account/Password');
 await page.goto(url+'/Account/Password');await page.locator('[name=CurrentPassword]').fill('WrongBrowserQa@123');await page.locator('[name=NewPassword]').fill(replacement);
 await page.locator('.account-form button[type=submit]').click();await page.locator('.field-validation-error').waitFor();
 assert(await page.locator('[name=CurrentPassword]').inputValue()===''&&await page.locator('[name=NewPassword]').inputValue()==='','Password validation echoed secrets');
 await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.join(evidence,'password-error-mobile.png'),fullPage:true});
 await page.locator('[name=CurrentPassword]').fill(password);await page.locator('[name=NewPassword]').fill(replacement);await page.locator('.account-form button[type=submit]').click();await page.waitForURL('**/Account/Login');assert(await count('PHIEN')===0,'Password did not revoke session');
 await page.locator('[name=Username]').fill(fields.Username);await page.locator('[name=Password]').fill(replacement);await page.locator('.account-form button[type=submit]').click();await page.waitForURL('**/Home/Index');
 await page.getByRole('button',{name:'Đăng xuất',exact:true}).click();await page.waitForURL('**/Home/Index');assert(await count('PHIEN')===0,'Real browser logout left session');
 await page.goto(url+'/Account/Login');await page.keyboard.press('Tab');assert(await page.locator('.skip-link').evaluate(x=>x===document.activeElement),'Keyboard skip link not first');
 await page.keyboard.press('Enter');let usernameFocused=false;
 for(let step=0;step<6;step++){await page.keyboard.press('Tab');if(await page.evaluate(()=>document.activeElement?.getAttribute('name')==='Username')){usernameFocused=true;break;}}
 assert(usernameFocused,'Account form not reachable by keyboard after in-content links');
 assert(errors.length===0,'Browser JavaScript error');assert(!hostOutput.includes(password)&&!hostOutput.includes(replacement)&&!hostOutput.includes('WrongBrowserQa@123'),'Host log exposed password');
 fs.writeFileSync(path.join(evidence,'results.json'),JSON.stringify({at:new Date().toISOString(),scope:'Account K01-K05 only; guest-cart merge and catalog/orders not covered',qaDatabase:'Generated PhoneStore_QA_MvcAccounts_GUID, removed in finally',registrationPosts,loginDoubleSubmitPosts,totalLoginPostsIncludingAfterPasswordChange:loginPosts,registerBusy,loginBusy,sql:{accounts:await count('TAI_KHOAN'),profiles:await count('KHACH_HANG'),sessionsAfterPasswordAndLogout:await count('PHIEN')},passwordsAbsentFromHostLogs:true,keyboardFormReachable:true,records,errors},null,2));
 console.log('PASS MVC account Chromium desktop/mobile, register/login double submit, form errors, password change, logout, keyboard, SQL counts and password-free host logs');
}finally{
 if(browser)await browser.close();
 if(host){host.kill();await new Promise(r=>host.once('exit',r));}
 if(owned)await sql(`DROP DATABASE [${name}];`);
}
