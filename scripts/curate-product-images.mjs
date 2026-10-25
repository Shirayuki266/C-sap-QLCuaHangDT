import fs from 'node:fs';
const models=['iPhone 13','iPhone 14','iPhone 15','iPhone 16','Galaxy A15','Galaxy A25','Galaxy A55','Galaxy S24','Redmi Note 13','Redmi Note 14','Xiaomi 14','POCO X6','OPPO A60','OPPO A78','OPPO Reno11','OPPO Reno12','vivo Y28','vivo Y36','vivo V30','vivo V40','realme C55','realme C67','realme 12','realme GT6','Galaxy S25'];
const results=[];
for(const model of models){
 const entry={model,images:[],fallbackReason:''};
 try{
  const url=new URL('https://commons.wikimedia.org/w/api.php');for(const [key,value] of Object.entries({action:'query',format:'json',generator:'search',gsrnamespace:'6',gsrsearch:'"'+model+'"',gsrlimit:'8',prop:'imageinfo',iiprop:'url|extmetadata',iiurlwidth:'600'}))url.searchParams.set(key,value);
  if(model==='Galaxy S24'){for(const key of ['generator','gsrnamespace','gsrsearch','gsrlimit'])url.searchParams.delete(key);url.searchParams.set('titles','File:Samsung Galaxy S24 01.jpg');}
  const data=await fetch(url,{headers:{'User-Agent':'PhoneStoreCoursework/1.0 (licensed image verification)'},signal:AbortSignal.timeout(15000)}).then(r=>r.json());
  const pattern=new RegExp('\\b'+model.replace(/[.*+?^${}()|[\]\\]/g,'\\$&').replaceAll(' ','[ _-]*')+'\\b','i');
  for(const page of Object.values(data.query?.pages||{})){
   const info=page.imageinfo?.[0],meta=info?.extmetadata;if(!info||!pattern.test(page.title)||/\b(pro|max|plus|ultra|case|logo|icon|schematic|teardown|inside|mockup|packaging|comparison|diagram|price|und)\b|性能|对比/i.test(page.title))continue;
   const license=meta?.LicenseShortName?.value||'';if(!/^(CC BY(?:-SA)? [234]\.0|CC0|Public domain)$/.test(license))continue;
   const imageUrl=info.thumburl||info.url;if(!imageUrl?.startsWith('https://'))continue;
   const image=await fetch(imageUrl,{signal:AbortSignal.timeout(15000)});if(!image.ok||!image.headers.get('content-type')?.startsWith('image/'))continue;await image.arrayBuffer();
   entry.images.push({url:imageUrl,source:info.descriptionurl,title:page.title,author:(meta.Artist?.value||'').replace(/<[^>]*>/g,''),license,licenseUrl:meta.LicenseUrl?.value||'',verifiedHttp:200});if(entry.images.length===2)break;
  }
  if(!entry.images.length)entry.fallbackReason='Không xác minh được ảnh đúng mẫu với giấy phép phù hợp và URL tải được; giữ ảnh dự phòng/ảnh người dùng.';
 }catch{entry.fallbackReason='Nguồn ảnh hoặc kiểm tra mạng không khả dụng; giữ ảnh hiện có.';}
 results.push(entry);console.log(model+': '+entry.images.length+' verified images');await new Promise(r=>setTimeout(r,250));
}
fs.mkdirSync('qa',{recursive:true});fs.writeFileSync('qa/product-images.json',JSON.stringify(results,null,2));
fs.mkdirSync('src/PhoneStore.Web/wwwroot/data',{recursive:true});fs.writeFileSync('src/PhoneStore.Web/wwwroot/data/image-credits.json',JSON.stringify(results,null,2));
