// Offline asset/credit verification. Visual subject review is recorded separately.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import cp from 'node:child_process';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const facts=JSON.parse(fs.readFileSync(path.join(root,'tools/PhoneStore.DemoData/Catalog/real-products.json'),'utf8'));
const credits=fs.readFileSync(path.join(root,'docs/demo-data/IMAGE_CREDITS.md'),'utf8');
const review=JSON.parse(fs.readFileSync(path.join(root,'docs/demo-data/IMAGE_REVIEW.json'),'utf8'));
const digest=b=>crypto.createHash('sha256').update(b).digest('hex');
const errors=[],files=[],licenses={};
const attr=(tag,key)=>Number(tag.match(new RegExp('\\b'+key+'="([^"]+)"'))?.[1]);
for(const product of facts){
 const parent=product.photos[0].crop;
 for(const photo of product.photos){
  const file=path.join(root,'src/PhoneStore.Web/wwwroot/images/demo',photo.file);
  const svg=fs.readFileSync(file,'utf8'),image=svg.match(/<image\s[^>]+>/)?.[0]??'';
  const href=image.match(/href="(data:image\/(?:jpeg|png);base64,([A-Za-z0-9+/=]+))"/);
  const clip=svg.match(/<clipPath id="photo"><rect ([^>]+)\/><\/clipPath>/)?.[1]??'';
  const crop=photo.crop;
  const sx=attr(image,'width')/photo.width,sy=attr(image,'height')/photo.height;
  const shown={x:(attr(clip,'x')-attr(image,'x'))/sx,y:(attr(clip,'y')-attr(image,'y'))/sy,
   width:attr(clip,'width')/sx,height:attr(clip,'height')/sy};
  const reasons=[];
  if(!href||digest(Buffer.from(href[2],'base64'))!==photo.sha256||digest(svg)!==photo.wrapperSha256)reasons.push('byte digest mismatch');
  if(!/^lc\d{3}-[123]\.svg$/.test(photo.file)||!credits.includes('`'+photo.file+'`'))reasons.push('missing per-file credit');
  const inspected=review.files?.find(x=>x.file===photo.file);
  if(!inspected||inspected.wrapperSha256!==photo.wrapperSha256||!['displayedCropReviewed','noIdentifiablePerson','noPricePlacardOrPriceOverlay','noThirdPartyLogo','modelVersionMatched','rendered'].every(key=>inspected[key]===true))reasons.push('missing or stale visual review');
  if(!photo.author||!photo.licenseUrl||!(photo.license==='CC0'||/^CC BY(?:-SA)? [34]\.0$/.test(photo.license)))reasons.push('missing licensed attribution/version');
  const expectedLicense={'CC BY-SA 4.0':'/licenses/by-sa/4.0','CC BY-SA 3.0':'/licenses/by-sa/3.0','CC BY 4.0':'/licenses/by/4.0','CC0':'/publicdomain/zero/1.0'}[photo.license];
  let grant;try{grant=new URL(photo.licenseUrl)}catch{}
  if(!grant||grant.hostname!=='creativecommons.org'||!expectedLicense||!([expectedLicense,expectedLicense+'/'].includes(grant.pathname)||grant.pathname.startsWith(expectedLicense+'/deed.')))reasons.push('license label/URL mismatch');
  if(!Object.keys(shown).every(k=>Number.isFinite(shown[k])&&Math.abs(shown[k]-crop[k])<0.02))reasons.push('SVG reveals pixels outside recorded crop');
  if(crop.x<0||crop.y<0||crop.width<1||crop.height<1||crop.x+crop.width>photo.width||crop.y+crop.height>photo.height)reasons.push('invalid raster bounds');
  if(crop.x<parent.x-1||crop.y<parent.y-1||crop.x+crop.width>parent.x+parent.width+1||crop.y+crop.height>parent.y+parent.height+1)reasons.push('detail escapes reviewed parent');
  if(/<(?:script|foreignObject)\b/.test(svg)||svg.includes('href="http'))reasons.push('active/external image dependency');
  errors.push(...reasons.map(reason=>photo.file+': '+reason));
  licenses[photo.license]=(licenses[photo.license]??0)+1;
  files.push({file:photo.file,model:product.name,wrapperSha256:photo.wrapperSha256,crop,downloadSha256:photo.sha256,valid:reasons.length===0});
 }
}
if(facts.length!==50||files.length!==150||new Set(files.map(x=>x.file)).size!==150)errors.push('Expected 50 models/150 unique assets');
if(review.catalogSha256!==digest(fs.readFileSync(path.join(root,'tools/PhoneStore.DemoData/Catalog/real-products.json')))||review.files?.length!==150)errors.push('Visual review belongs to a different catalog');
const candidateSha=cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim();
const workingTreeClean=cp.execFileSync('git',['status','--porcelain','--untracked-files=normal'],{cwd:root,encoding:'utf8',stdio:['ignore','pipe','ignore']}).trim()==='';
const result={candidateSha,workingTreeClean,scope:'offline current catalog/credits/SVG geometry and digest-bound recorded visual review; does not automatically infer visual subject correctness',valid:errors.length===0,models:facts.length,images:files.length,bytes:files.reduce((sum,x)=>sum+fs.statSync(path.join(root,'src/PhoneStore.Web/wwwroot/images/demo',x.file)).size,0),licenses,errors,files};
const index=process.argv.indexOf('--output');
if(index>=0){const target=path.resolve(process.argv[index+1]);fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,JSON.stringify(result,null,2)+'\n');}
console.log(JSON.stringify({candidateSha,valid:result.valid,images:result.images,bytes:result.bytes,licenses,errors},null,2));
process.exitCode=result.valid?0:1;
