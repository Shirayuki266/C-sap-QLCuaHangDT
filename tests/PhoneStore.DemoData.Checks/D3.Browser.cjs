const fs=require('node:fs'),path=require('node:path'),{performance}=require('node:perf_hooks');
const {chromium}=require(process.env.PHONESTORE_PLAYWRIGHT_MODULE||path.join(process.cwd(),'.artifacts/mvc-browser/node_modules/playwright'));
const contextPath=process.env.PHONESTORE_DEMO_CONTEXT;
const data=JSON.parse(fs.readFileSync(contextPath,'utf8').replace(/^\uFEFF/,''));
if(!/^http:\/\/127\.0\.0\.1:\d+$/.test(data.url)||!data.database.startsWith('PhoneStore_Demo_D3_'))throw Error('D3 dedicated loopback target required');
const output=path.join(path.dirname(contextPath),'browser');fs.mkdirSync(output,{recursive:true});
let stage='launch';const checks=[],timings=[],screens=[],pageErrors=[];
const assert=(value,message)=>{if(!value)throw Error(message);};
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
  const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage();
  page.on('pageerror',()=>pageErrors.push(stage));
  async function go(route,label){stage=label;const start=performance.now();const response=await page.goto(data.url+route,{waitUntil:'domcontentloaded'});assert(response.ok(),'Navigation failed');await page.locator('main').waitFor();await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));const milliseconds=performance.now()-start;timings.push({label,route,milliseconds});assert(milliseconds<data.thresholds.browserMilliseconds,'Page opening threshold exceeded');}
  async function imageReady(image){await image.scrollIntoViewIfNeeded();await image.evaluate(el=>el.complete&&el.naturalWidth>0?true:new Promise((resolve,reject)=>{el.addEventListener('load',resolve,{once:true});el.addEventListener('error',reject,{once:true});}));assert(await image.evaluate(el=>el.naturalWidth>0),'Image did not render');}
  async function capture(name){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(output,name+'.png'),fullPage:true});screens.push(name+'.png');}
  await go('/Home/Index','home');assert(await page.locator('#bestsellers .product-card').count()===8,'Home bestseller count');
  for(const image of await page.locator('#bestsellers img').all())await imageReady(image);
  await capture('mvc-home');checks.push({name:'current-month home bestsellers',products:8});
  const productIds=new Set();
  for(let number=1;;number++){
   await go('/Catalog/Index?page='+number,'catalog-page-'+number);
   const total=await page.locator('[data-result-total]').getAttribute('data-result-total');assert(Number(total)===data.expected.activeProducts,'Catalog total differs from SQL');
   const cards=page.locator('.product-card');const count=await cards.count();assert(count>0,'Missing catalog page');
   for(const card of await cards.all()){
    const href=await card.locator('a[href*="/Catalog/Details/"]').first().getAttribute('href');const id=Number(new URL(href,data.url).pathname.split('/').at(-1));
    assert(data.products.some(x=>x.id===id),'Unexpected/inactive product');productIds.add(id);await imageReady(card.locator('img').first());
   }
   if(number===1)await capture('mvc-catalog');
   if(productIds.size===data.expected.activeProducts)break;
   assert(number<10,'Unexpected catalog pagination');
  }
  assert(productIds.size===46,'Full active catalog coverage');
  for(const product of data.products){
   await go('/Catalog/Details/'+product.id,'product-'+product.id);
   assert((await page.locator('h1').innerText()).trim()===product.name,'Product name differs');
   assert((await page.locator('.product-information p.catalog-preline').innerText()).trim()===product.description,'Vietnamese description differs');
   assert((await page.locator('.product-information dd.catalog-preline').innerText()).trim().replace(/\r\n/g,'\n')===product.specifications.replace(/\r\n/g,'\n'),'Specifications differ');
   await imageReady(page.locator('img[src^="/images/demo/"]').first());
   if(product===data.products[0])await capture('mvc-product');
  }
  checks.push({name:'all active products, actual photos/descriptions/specifications',products:productIds.size});
  for(const query of ['Samsung','Auvora','Lumetrix','DM001']){
   await go('/Catalog/Index?Q='+encodeURIComponent(query),'search-'+query);
   const count=Number(await page.locator('[data-result-total]').getAttribute('data-result-total'));
   assert(query==='Samsung'?count>0:count===0,'Current/legacy search mismatch');
   checks.push({name:'catalog name search',query,count});
  }
  for(let sample=0;sample<5;sample++)await go('/Catalog/Index','catalog-timing-'+sample);
  await go('/Account/Login','customer-login');
  await page.locator('[name=Username]').fill(data.customerUsername);await page.locator('[name=Password]').fill(process.env.PHONESTORE_DEMO_CUSTOMER_PASSWORD);
  await page.locator('form[action="/Account/Login"] button[type=submit]').click();await page.waitForLoadState('networkidle');
  assert(await page.locator('form[action="/Account/Logout"]').count()===1,'Customer login failed');
  await go('/Orders/Index','customer-history');
  const history=await context.request.get(data.url+'/api/orders?page=1&pageSize=20');assert(history.ok(),'Owned history HTTP failed');
  const owned=await history.json();assert(owned.total===data.customerOrders,'History total differs from SQL');
  assert(owned.items.length>0&&owned.items.every(x=>data.ownedOrders.some(o=>o.id===x.id)),'Ownership mismatch');
  await capture('mvc-customer-history');
  const first=owned.items[0];await go('/Orders/Details/'+first.id,'customer-order');
  assert((await page.locator('main').innerText()).includes(first.code),'Owned order detail missing');await capture('mvc-customer-order');
  checks.push({name:'real MVC login and owned history',orders:owned.total,visible:owned.items.length});
  for(const order of data.customerQuarterOrders){
   await go('/Orders/Details/'+order.id,'customer-history-'+order.quarter);
   assert((await page.locator('main').innerText()).includes(order.code),'Historical owned order missing');
   await capture('mvc-owned-'+order.quarter);
  }
  await go('/Cart/Index','customer-current-cart');await capture('mvc-cart');
  await go('/Cart/Checkout','customer-read-only-checkout');
  assert(page.url().includes('/Cart/Checkout'),'Checkout was not rendered');await capture('mvc-checkout');
  checks.push({name:'read-only historical quarters/cart/checkout',quarters:data.customerQuarterOrders.length,ordersPlaced:0});
  await page.locator('form[action="/Account/Logout"] button').click();await page.waitForLoadState('networkidle');
  assert(await page.locator('form[action="/Account/Logout"]').count()===0,'Customer logout failed');
  assert(pageErrors.length===0,'Browser script errors');
  fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({result:'PASS',realBrowser:true,realHost:true,fakeApi:false,checks,timings,screens,pageErrors,thresholdMilliseconds:data.thresholds.browserMilliseconds},null,2));
  await context.close();console.log('PASS actual MVC full demo: 46 products/images/descriptions, eight bestsellers, real customer history, screenshots and timings');
 }finally{await browser.close();}
})().catch(error=>{fs.writeFileSync(path.join(output,'failure.json'),JSON.stringify({stage,errorType:error.name}));console.error('D3 browser failed at '+stage+'; details withheld to protect credentials');process.exitCode=1;});
