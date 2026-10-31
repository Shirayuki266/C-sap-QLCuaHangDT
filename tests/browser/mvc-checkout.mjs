// Explicit QA runner only; fixture commands go to the parent process, never to a shipping route.
import fs from 'node:fs';import path from 'node:path';import{createRequire}from'node:module';
const require=createRequire(import.meta.url),{chromium}=require(process.env.PHONESTORE_PLAYWRIGHT_MODULE||'playwright');
const {url,MainId:product,Black128:variant}=JSON.parse(fs.readFileSync(process.env.PHONESTORE_CHECKOUT_BROWSER_CONTEXT,'utf8'));
if(!/^http:\/\/127\.0\.0\.1:\d+$/.test(url))throw Error('QA loopback required');
const root=path.join(process.cwd(),'.artifacts/mvc-checkout'),output=path.join(root,'browser');fs.mkdirSync(output,{recursive:true});
const browser=await chromium.launch({headless:true}),records=[],errors=[];
const assert=(value,message)=>{if(!value)throw Error(message);};
async function command(kind){const nonce=kind+'-'+Date.now();fs.writeFileSync(path.join(root,'browser-command.json'),JSON.stringify({kind,nonce}));for(let i=0;i<200;i++){try{const ack=JSON.parse(fs.readFileSync(path.join(root,'browser-ack.json'),'utf8'));if(ack.nonce===nonce&&ack.ok)return;}catch{}await new Promise(resolve=>setTimeout(resolve,50));}throw Error('QA parent command timeout: '+kind);}
try{
 const session=await browser.newContext();const page=await session.newPage();page.on('pageerror',error=>errors.push(error.message));const desktop={width:1440,height:1000},mobile={width:390,height:844};
 let checkoutPosts=0;page.on('request',request=>{if(request.method()==='POST'&&new URL(request.url()).pathname==='/Cart/Checkout')checkoutPosts++;});
 async function login(){await page.locator('[name=Username]').fill('catalog-qa-customer');await page.locator('[name=Password]').fill('CatalogFixtureQa@12345');await page.locator('form[action="/Account/Login"] button[type=submit]').click();await page.waitForURL('**/Cart/Checkout');await page.waitForLoadState('networkidle');}
 async function form(){return page.locator('.checkout-form').evaluate(node=>Object.fromEntries(new FormData(node)));}
 async function fill(){await page.locator('[name=Recipient]').fill('Nguyễn An QA');await page.locator('[name=Phone]').fill('0938123501');await page.locator('[name=Address]').fill('24 Đường Kiểm Thử, Phường QA, TP. Hồ Chí Minh');}
 async function capture(name,size){await page.setViewportSize(size);await page.waitForLoadState('networkidle');const metrics=await page.evaluate(()=>{const shipping=document.querySelector('.shipping-panel')?.getBoundingClientRect(),summary=document.querySelector('.checkout-summary')?.getBoundingClientRect();return{viewport:innerWidth,width:document.documentElement.scrollWidth,unlabelled:[...document.querySelectorAll('input:not([type=hidden]),textarea')].filter(x=>!x.labels?.length).length,summaryBelowShipping:shipping&&summary?summary.top>=shipping.bottom:null};});assert(metrics.viewport===metrics.width&&metrics.unlabelled===0,'Overflow/unlabelled input '+name);if(name.startsWith('checkout')&&size.width<=480)assert(metrics.summaryBelowShipping,'Mobile summary must follow shipping');await page.screenshot({path:path.join(output,name+'.png'),fullPage:true});records.push({name,...metrics});}
 await page.setViewportSize(desktop);await page.goto(url+'/Cart/Checkout',{waitUntil:'networkidle'});await login();await fill();
 const firstKey=await page.locator('[name=RequestKey]').inputValue();assert(/^[a-f0-9-]{36}$/i.test(firstKey),'Missing GUID');await page.reload({waitUntil:'networkidle'});assert(await page.locator('[name=RequestKey]').inputValue()!==firstKey,'GET reused requestKey');await fill();
 await capture('checkout-desktop',desktop);await capture('checkout-mobile',mobile);
 const original=await form();await command('change-price');
 await page.locator('.checkout-form button[type=submit]').click();await page.waitForLoadState('networkidle');await page.getByRole('button',{name:/Xác nhận giá mới/}).waitFor();
 assert((await page.locator('#checkout-total').innerText())==='26.000.000 đ','Changed price not shown');assert((await page.locator('.validation-summary-errors').innerText()).includes('Chưa tạo đơn'),'No explicit re-confirmation');
 assert(await page.locator('[name=Recipient]').inputValue()==='Nguyễn An QA','Reprice lost shipping');
 await command('assert-before-first');
 await capture('checkout-price-change-desktop',desktop);await capture('checkout-price-change-mobile',mobile);
 const confirmed=await form();assert(confirmed.RequestKey===original.RequestKey&&confirmed.Confirmation!==original.Confirmation,'Reprice token/key wrong');
 // Dispatch two valid browser submit events before navigation. Server replay is separately exercised below.
 const postsBefore=checkoutPosts;await page.locator('.checkout-form').evaluate(form=>{form.requestSubmit();form.requestSubmit();});await page.waitForURL('**/Orders/Details/*');await page.waitForLoadState('networkidle');
 const receiptUrl=page.url();assert(checkoutPosts-postsBefore===1,'Browser duplicate guard emitted two POSTs');assert(await page.locator('#order-total').innerText()==='26.000.000 đ','Receipt has unconfirmed price');
 const shownTime=await page.locator('.order-status time').evaluate(time=>{const d=new Date(new Date(time.dateTime).getTime()+7*3600000);const pad=n=>String(n).padStart(2,'0');return{text:time.textContent,expected:`${pad(d.getUTCDate())}/${pad(d.getUTCMonth()+1)}/${d.getUTCFullYear()} ${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}`};});assert(shownTime.text===shownTime.expected,'Receipt not UTC+7');
 await capture('receipt-desktop',desktop);await capture('receipt-mobile',mobile);await command('first-order');
 await page.reload({waitUntil:'networkidle'});assert(page.url()===receiptUrl,'Receipt F5 changed location');
 let backNavigation='loaded';try{await page.goBack({waitUntil:'networkidle'});}catch(error){if(!error.message.includes('ERR_CACHE_MISS'))throw error;backNavigation='ERR_CACHE_MISS for no-store POST';}
 const restoredForm=await page.locator('.checkout-form').count();
 // A no-store Back may reload and resolve the already-created order instead of restoring a stale form.
 if(restoredForm){await page.locator('.checkout-form').evaluate((form,values)=>{for(const[name,value]of Object.entries(values)){const input=form.elements.namedItem(name);if(input)input.value=value;}form.requestSubmit();},confirmed);await page.waitForURL(receiptUrl);await page.waitForLoadState('networkidle');}
 else {const response=await session.request.post(url+'/Cart/Checkout',{form:confirmed,maxRedirects:0});assert(response.status()===302&&new URL(response.headers().location,url).href===receiptUrl,'Back replay did not resolve existing order');await page.goto(receiptUrl,{waitUntil:'networkidle'});}
 await command('first-order');
 const replay=await session.request.post(url+'/Cart/Checkout',{form:confirmed,maxRedirects:0});assert(replay.status()===302&&new URL(replay.headers().location,url).href===receiptUrl,'Same-key HTTP replay did not resolve existing receipt');await command('first-order');
 records.push({name:'fresh-guid-price-reconfirm-double-submit-F5-Back-original-key-replay',result:'PASS',guardedBrowserPosts:1,serverOrderCount:1,backNavigation,backRestoredForm:Boolean(restoredForm),backReplayTransport:restoredForm?'DOM form':'browser HTTP replay of captured form after no-store Back; stale DOM form was not restored'});
 // Add a new server cart line using the real details form, then expire this customer's SQL sessions in the parent QA fixture.
 await page.goto(`${url}/Catalog/Details/${product}?variantId=${variant}`,{waitUntil:'networkidle'});await page.locator('form[action="/Cart/Add"] button[type=submit]').click();await page.waitForURL('**/Cart/Index');await page.waitForLoadState('networkidle');
 await page.getByRole('link',{name:'Tiếp tục đặt hàng',exact:true}).click();await page.waitForURL('**/Cart/Checkout');await fill();
 const expiredForm=await form(),expiredCookies=await session.cookies();await command('expire-session');
 await page.locator('.checkout-form button[type=submit]').click();await page.waitForURL('**/Account/Login?*');await page.waitForLoadState('networkidle');assert(new URL(page.url()).searchParams.get('returnUrl')==='/Cart/Checkout','Expired flow returnUrl wrong');
 await command('assert-expired-no-order');await login();await fill();assert(await page.locator('.checkout-line').count()===1,'Relogin lost server cart');assert(await page.locator('[name=RequestKey]').inputValue()!==expiredForm.RequestKey,'Relogin did not open fresh checkout');
 const stale=await browser.newContext();await stale.addCookies(expiredCookies);const denied=await stale.request.post(url+'/Cart/Checkout',{form:expiredForm,maxRedirects:0});assert(denied.status()===302&&denied.headers().location.includes('/Account/Login'),'Old cookie created order after relogin');await stale.close();await command('assert-expired-no-order');
 await capture('checkout-resumed-mobile',mobile);await page.locator('.checkout-form button[type=submit]').click();await page.waitForURL('**/Orders/Details/*');await page.waitForLoadState('networkidle');assert(await page.locator('#order-total').innerText()==='13.000.000 đ','New session order total wrong');
 records.push({name:'expire-mid-checkout-login-return-cart-retained-old-cookie-denied-new-session-order',result:'PASS'});
 assert(errors.length===0,'Browser JavaScript error');fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({browser:'Playwright Chromium',qaOnly:true,records,javascriptErrors:errors,notes:['Parent QA fixture alone changes price/expires sessions; no shipping QA endpoint','Captured form tokens/cookies stay in memory','SQL oracle verifies first order after every replay and final new-session order']},null,2));
 console.log('PASS real Chromium fresh GUID; reprice no order until confirmation; duplicate guard; F5/Back/original-key replay; expiry/login/cart recovery; old-cookie denial; desktop/mobile UI06/receipt');
}finally{await browser.close();}
