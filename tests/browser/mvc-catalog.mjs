// Launch through --mvc-catalog-browser: the .NET fixture owns guarded SQL QA and the real Kestrel host.
import fs from 'node:fs';import path from 'node:path';import {createRequire} from 'node:module';
const require=createRequire(import.meta.url),{chromium}=require(process.env.PHONESTORE_PLAYWRIGHT_MODULE||'playwright');
const context=JSON.parse(fs.readFileSync(process.env.PHONESTORE_CATALOG_BROWSER_CONTEXT,'utf8'));
const {url,MainId:main,Black128:black128,Black256:black256,Blue256:blue256,BrandId:brand}=context;
if(!/^http:\/\/127\.0\.0\.1:\d+$/.test(url))throw Error('Browser must use fixture loopback host');
const out=path.join(process.cwd(),'.artifacts/mvc-catalog/browser');fs.mkdirSync(out,{recursive:true});
const browser=await chromium.launch({headless:true});const records=[],errors=[];
function assert(value,message){if(!value)throw Error(message);}
try{
 const session=await browser.newContext();
 async function capture(name,route,viewport){
  const page=await session.newPage();page.on('pageerror',e=>errors.push(e.message));await page.setViewportSize(viewport);
  const response=await page.goto(url+route,{waitUntil:'networkidle'});assert(response.ok(),'Screen did not load '+name);
  const metrics=await page.evaluate(()=>({viewport:innerWidth,document:document.documentElement.scrollWidth,cards:document.querySelectorAll('.product-card').length,
   columns:document.querySelector('.product-grid')?getComputedStyle(document.querySelector('.product-grid')).gridTemplateColumns.split(' ').length:0,
   unlabelled:[...document.querySelectorAll('input:not([type=hidden]),select')].filter(input=>!input.labels?.length).length}));
  assert(metrics.viewport===metrics.document&&metrics.unlabelled===0,'Overflow/unlabelled control '+name);
  await page.screenshot({path:path.join(out,name+'.png'),fullPage:true});records.push({name,...metrics});await page.close();
 }
 const desktop={width:1440,height:1000},mobile={width:390,height:844};
 for(const [name,route]of [['home','/Home/Index'],['catalog','/Catalog/Index'],['details',`/Catalog/Details/${main}`]]){
  await capture(name+'-desktop',route,desktop);await capture(name+'-mobile',route,mobile);
 }
 await capture('catalog-tablet','/Catalog/Index',{width:768,height:1000});
 const page=await session.newPage();page.on('pageerror',e=>errors.push(e.message));await page.setViewportSize(mobile);
 await page.goto(url+'/Catalog/Index',{waitUntil:'networkidle'});
 assert(await page.locator(`.product-card[data-product-id="${main}"] img`).evaluate(image=>image.complete&&image.naturalWidth>0&&!image.hidden),'Valid fixture product image did not load');
 assert(!await page.locator('.catalog-filter').evaluate(el=>el.open),'Mobile filter did not become a collapsible panel');
 await page.locator('.catalog-filter summary').click();assert(await page.locator('.catalog-filter').evaluate(el=>el.open),'Filter panel cannot open');
 await page.locator('#filter-q').fill('Aurora');await page.locator('#filter-brand').selectOption(String(brand));
 await page.locator('#filter-storage').fill('256');await page.locator('#filter-color').fill('Đen');await page.locator('#filter-min').fill('20000000');await page.locator('#filter-max').fill('25000000');await page.locator('#filter-sort').selectOption('price-desc');await page.locator('#filter-size').fill('1');
 await page.getByRole('button',{name:'Áp dụng',exact:true}).click();await page.waitForLoadState('networkidle');
 assert(await page.locator('.product-card').count()===1&&await page.locator('.product-price').innerText()==='24.000.000 đ','Combined filter did not use matching variant price');
 await page.getByRole('link',{name:'Trang sau',exact:true}).click();await page.waitForLoadState('networkidle');assert(await page.locator('.product-price').innerText()==='22.000.000 đ'&&new URL(page.url()).searchParams.get('color')==='Đen','Paging lost query or sorting');
 await page.goto(url+'/Catalog/Index?q=no-qa-match',{waitUntil:'networkidle'});await page.screenshot({path:path.join(out,'catalog-empty-mobile.png'),fullPage:true});
 await page.goto(url+'/Catalog/Index?minPrice=20&maxPrice=10',{waitUntil:'networkidle'});assert(await page.locator('.validation-summary-errors').isVisible()&&await page.locator('.catalog-filter').evaluate(el=>el.open),'Filter errors hidden behind closed panel');await page.screenshot({path:path.join(out,'catalog-error-mobile.png'),fullPage:true});
 await page.goto(url+`/Catalog/Details/${main}`,{waitUntil:'networkidle'});
 assert(await page.locator('.variant-controls').isVisible()&&!await page.locator('.variant-fallback').isVisible(),'Progressive variant controls not initialized');
 assert(await page.locator('#selected-variant').inputValue()===String(black128)&&await page.locator('#product-quantity').getAttribute('max')==='6','Default variant wrong');
 await page.locator('input[name=storageChoice][value="256"]').check();assert(await page.locator('#selected-variant').inputValue()===String(black256)&&await page.locator('#selected-price').innerText()==='24.000.000 đ'&&await page.locator('#product-quantity').getAttribute('max')==='7','Storage selection did not update server-supplied variant');
 await page.locator('#product-quantity').fill('8');assert(!await page.locator('#product-quantity').evaluate(el=>el.checkValidity()),'Quantity above available is not invalid in UI');await page.locator('#product-quantity').fill('1.5');assert(!await page.locator('#product-quantity').evaluate(el=>el.checkValidity()),'Fractional quantity is not invalid in UI');await page.locator('#product-quantity').fill('1');
 await page.screenshot({path:path.join(out,'details-selected-mobile.png'),fullPage:true});
 await page.locator('input[name=colorChoice][value="Xanh"]').check();assert(await page.locator('#selected-variant').inputValue()===String(blue256)&&await page.locator('#selected-price').innerText()==='18.000.000 đ'&&await page.locator('#add-to-cart').isDisabled()&&await page.locator('#add-to-cart').innerText()==='Hết hàng'&&await page.locator('#product-quantity').isDisabled(),'Out-of-stock state failed');
 await page.screenshot({path:path.join(out,'details-sold-out-mobile.png'),fullPage:true});
 await page.locator('input[name=storageChoice][value="128"]').check();assert(await page.locator('input[name=colorChoice][value="Xanh"]').isDisabled()&&await page.locator('#selected-variant').inputValue()===String(black128),'Invalid color/storage cross-pair selectable');
 const form=page.locator('.product-add-form');assert(await form.getAttribute('action')==='/Cart/Add'&&await form.getAttribute('method')==='post'&&await form.locator('input[name=__RequestVerificationToken]').count()===1,'Future Add form contract/token wrong');
 // Intentionally never submit Cart/Add: action and every POST assertion remain step4 NOT RUN.
 await page.locator('.product-thumbnails button').nth(1).click();assert((await page.locator('#main-product-image').getAttribute('src')).endsWith('phone-b.svg'),'Gallery did not switch image');
 await page.goto(url+'/Catalog/Index',{waitUntil:'networkidle'});assert(await page.locator(`.product-card[data-product-id="${context.OutOfStockId}"] .image-placeholder`).isVisible(),'Broken image fallback missing');
 await page.goto(url+'/Catalog/Index',{waitUntil:'networkidle'});await page.keyboard.press('Tab');assert(await page.locator('.skip-link').evaluate(el=>el===document.activeElement),'Skip link not reachable');await page.keyboard.press('Enter');let filterFocused=false;
 for(let i=0;i<12;i++){await page.keyboard.press('Tab');if(await page.locator('.catalog-filter summary').evaluate(el=>el===document.activeElement)){filterFocused=true;break;}}assert(filterFocused,'Filter panel not reachable by keyboard');
 assert(records.find(r=>r.name==='catalog-desktop').columns===3&&records.find(r=>r.name==='catalog-tablet').columns===2&&records.find(r=>r.name==='catalog-mobile').columns===1,'Grid did not follow3/2/1 responsive columns');
 const noJs=await browser.newContext({javaScriptEnabled:false});const plain=await noJs.newPage();await plain.goto(url+`/Catalog/Details/${main}`);await plain.locator('#variant-choice').selectOption(String(blue256));await plain.getByRole('button',{name:'Xem phiên bản',exact:true}).click();await plain.waitForLoadState('networkidle');assert(await plain.locator('#selected-variant').inputValue()===String(blue256)&&await plain.locator('#add-to-cart').isDisabled(),'No-JS selection did not re-render correct server state');await noJs.close();
 assert(errors.length===0,'JavaScript exception');fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({at:new Date().toISOString(),scope:'Step3 public read/selection only; Cart/Add action and K09 POST assertions explicitly NOT RUN until step4',fixtureImages:'Synthetic QA image files mounted in runner-owned webroot, never shipped as production product assets',gridColumns:[3,2,1],filterPanel:true,combinedFilterAndPaging:true,variantSelection:true,quantityBoundsUi:true,outOfStockUi:true,noJsSelection:true,gallery:true,brokenImageFallback:true,keyboard:true,cartPostSubmitted:false,records,errors},null,2));
 console.log('PASS Chromium UI03 UI04 UI13 desktop/mobile,3/2/1 grids, filters, variants, availability, no-JS fallback and keyboard; Cart/Add not submitted');
}finally{await browser.close();}
