// Invoked only by the isolated SQL QA runner, never by the shipping host.
import fs from 'node:fs';import path from 'node:path';import{createRequire}from'node:module';
const require=createRequire(import.meta.url),{chromium}=require(process.env.PHONESTORE_PLAYWRIGHT_MODULE||'playwright');
const {url,MainId:product,Black128:variant,Black256:second,Blue256:sold,HiddenProductId:hidden}=JSON.parse(fs.readFileSync(process.env.PHONESTORE_CART_BROWSER_CONTEXT,'utf8'));
if(!/^http:\/\/127\.0\.0\.1:\d+$/.test(url))throw Error('QA loopback required');
const output=path.join(process.cwd(),'.artifacts/mvc-cart/browser');fs.mkdirSync(output,{recursive:true});
const browser=await chromium.launch({headless:true}),records=[],errors=[],key='phonestore.guest-cart.v1';
const assert=(value,message)=>{if(!value)throw Error(message);};
try{
 const context=await browser.newContext();const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));
 const desktop={width:1440,height:1000},mobile={width:390,height:844};await page.setViewportSize(desktop);
 async function settled(){await page.waitForLoadState('networkidle');}
 async function capture(name,size){await page.setViewportSize(size);await settled();const metrics=await page.evaluate(()=>({viewport:innerWidth,width:document.documentElement.scrollWidth,columns:getComputedStyle(document.querySelector('.cart-layout')).gridTemplateColumns.split(' ').length,unlabelled:[...document.querySelectorAll('input:not([type=hidden])')].filter(x=>!x.labels?.length).length}));assert(metrics.viewport===metrics.width&&metrics.unlabelled===0,'Overflow or unlabelled control '+name);await page.screenshot({path:path.join(output,name+'.png'),fullPage:true});records.push({name,...metrics});}
 await page.goto(`${url}/Catalog/Details/${product}`,{waitUntil:'networkidle'});
 await page.locator('input[name=quantity]').fill('2');await page.locator('form[action="/Cart/Add"]').evaluate(form=>{form.requestSubmit();form.requestSubmit();});await page.waitForURL('**/Cart/Index');await settled();
 assert(await page.locator('#cart-total').innerText()==='24.000.000 đ','Guest money not server-priced');
 assert(await page.locator('#cart-badge').innerText()==='Giỏ hàng (2)','Guest badge incorrect');
 await page.reload({waitUntil:'networkidle'});assert(await page.locator('.cart-line').count()===1,'Guest reload lost cart');
 await capture('guest-cart-desktop',desktop);await capture('guest-cart-mobile',mobile);
 await page.locator('.cart-line input[type=number]').fill('3');await page.locator('.cart-line button[type=submit]').click();await settled();
 await page.waitForFunction(()=>document.querySelector('#cart-total')?.textContent==='36.000.000 đ');
 assert(await page.locator('#cart-total').innerText()==='36.000.000 đ','Guest update not quoted');
 await page.locator('.cart-line button[aria-label^="Xóa"]').click();
 await page.waitForFunction(()=>document.querySelector('.cart-empty')&&document.querySelector('#cart-badge')?.textContent==='Giỏ hàng (0)');
 assert(await page.evaluate(key=>JSON.parse(localStorage.getItem(key)).length===0,key),'Guest last removal not persisted');
 await page.evaluate(({key,variant})=>localStorage.setItem(key,JSON.stringify([{variantId:variant,quantity:2,price:1,total:1,userId:999,role:'ADMIN'}])),{key,variant});
 await page.reload({waitUntil:'networkidle'});assert(await page.locator('#cart-total').innerText()==='24.000.000 đ','localStorage money injection trusted');
 records.push({name:'guest-add-reload-update-tampered-price',result:'PASS'});
 // Build a stale guest cart: max(existing4,guest10), cap6; duplicate second rows max2; sold-out row dropped.
 const payload=[{variantId:variant,quantity:10},{variantId:second,quantity:2},{variantId:second,quantity:1},{variantId:sold,quantity:1}];
 await page.evaluate(({key,payload})=>localStorage.setItem(key,JSON.stringify(payload)),{key,payload});
 let failure=0;await page.route('**/Cart/Merge',async route=>{failure++;if(failure===1)await route.abort('failed');else if(failure===2)await route.fulfill({status:500,contentType:'text/html',body:'QA injected failure'});else await route.continue();});
 await page.getByRole('link',{name:'Đăng nhập để đặt hàng'}).click();await page.locator('input[name=Username]').fill('catalog-qa-customer');await page.locator('input[name=Password]').fill('CatalogFixtureQa@12345');
 await page.locator('form[action="/Account/Login"] button[type=submit]').click();await page.waitForURL('**/Cart/Index');await settled();
 assert(await page.evaluate(key=>localStorage.getItem(key)!==null,key),'Network merge failure erased guest');
 await page.getByRole('button',{name:'Thử lại',exact:true}).click();await settled();assert(await page.evaluate(key=>localStorage.getItem(key)!==null,key),'500 merge failure erased guest');
 await page.waitForFunction(()=>document.querySelector('#cart-feedback button')&&!document.querySelector('#cart-feedback button').disabled);
 await capture('merge-failure-mobile',mobile);
 await page.getByRole('button',{name:'Thử lại',exact:true}).click();await settled();
 await page.waitForFunction(key=>localStorage.getItem(key)===null,key);
 await page.waitForFunction(()=>document.querySelector('#cart-badge')?.textContent==='Giỏ hàng (8)'&&document.querySelectorAll('.cart-line').length===2);
 assert(await page.evaluate(key=>localStorage.getItem(key)===null,key),'Successful merge did not clear guest');
 assert(await page.locator('#cart-badge').innerText()==='Giỏ hàng (8)','Logged-in merged badge not refreshed');
 assert(await page.locator(`#quantity-${variant}`).inputValue()==='6'&&await page.locator(`#quantity-${second}`).inputValue()==='2','Max/cap/duplicate merge incorrect');
 assert((await page.locator('#cart-feedback').innerText()).includes('10 → 6')&&(await page.locator('#cart-feedback').innerText()).includes('1 → 0'),'Adjustments missing');
 await capture('merged-cart-desktop',desktop);await capture('merged-cart-mobile',mobile);
 const repeat=await page.evaluate(async payload=>{const token=document.querySelector('meta[name="csrf-token"]').content;const responses=await Promise.all([1,2].map(()=>fetch('/Cart/Merge',{method:'POST',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':token},body:JSON.stringify({lines:payload})}).then(async response=>({status:response.status,cart:(await response.json()).cart}))));return responses;},payload);
 assert(repeat.every(x=>x.status===200&&x.cart.lines.find(line=>line.variantId===variant).quantity===6&&x.cart.total===120000000),'Repeated merge multiplied quantities');
 records.push({name:'login-network500-retain-retry-max-cap-drop-replay-badge',result:'PASS'});
 await page.reload({waitUntil:'networkidle'});assert(await page.locator('#cart-badge').innerText()==='Giỏ hàng (8)','Authenticated F5 changed cart');
 // Real Razor update with server PRG and removal. Remaining row is retained for the SQL oracle.
 await page.locator(`#quantity-${variant}`).fill('5');await page.locator(`article[data-variant-id="${variant}"] form[action="/Cart/Update"] button`).click();await settled();
 assert(await page.locator('#cart-total').innerText()==='108.000.000 đ','Authenticated update total incorrect');
 await page.locator(`article[data-variant-id="${second}"] form[action="/Cart/Remove"] button`).click();await settled();assert(await page.locator('#cart-badge').innerText()==='Giỏ hàng (5)','Remove badge incorrect');
 const empty=await browser.newContext();const emptyPage=await empty.newPage();await emptyPage.goto(url+'/Cart/Index',{waitUntil:'networkidle'});assert((await emptyPage.locator('#cart-content').innerText()).includes('Giỏ hàng đang trống'),'Guest empty cart missing');await emptyPage.setViewportSize(mobile);await emptyPage.screenshot({path:path.join(output,'guest-empty-mobile.png'),fullPage:true});await empty.close();
 assert(errors.length===0,'Browser JavaScript error');
 fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({browser:'Playwright Chromium',qaOnly:true,records,javascriptErrors:errors,notes:['500/network failures are injected by Playwright only','SQL runner separately verifies stock, journal, orders and final cart','Checkout is deferred to step4b']},null,2));
 console.log('PASS real Chromium guest storage/quote/tamper; login merge network+500 retention/retry; max/cap/drop/duplicate+parallel replay; badge; update/remove; desktop/mobile UI05');
}finally{await browser.close();}
