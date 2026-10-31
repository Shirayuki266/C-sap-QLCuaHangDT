// Read-only pattern inventory. Never print matched values, source lines or secret fingerprints.
import fs from 'node:fs';
import path from 'node:path';
import cp from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const outputIndex=process.argv.indexOf('--output');
if(outputIndex<0||!process.argv[outputIndex+1])throw Error('Use --output <sanitized-report.json> [--history]');
const output=path.resolve(process.argv[outputIndex+1]);
const git=(args,input)=>{const r=cp.spawnSync('git',args,{cwd:root,input,maxBuffer:128*1024*1024});if(r.status!==0)throw Error('Git inventory failed');return r.stdout;};
const patterns={
 private_key:/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/g,
 github_token:/\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b/g,
 aws_access_key:/\b(?:AKIA|ASIA)[A-Z0-9]{16}\b/g,
 literal_password_or_secret:/\b\w*(?:password(?:hash)?|pwd|secret|api[_-]?key|access[_-]?token)['"]?\s*[:=]\s*['"]([^'"\r\n]{3,})['"]/gi,
 credential_call_literal:/\b(?:HashPassword|LoginRequest|RegistrationRequest)\s*\([^,\r\n]{0,160},\s*['"]([^'"\r\n]{3,})['"]/g,
 connection_password:/(?:^|[;"'])\s*(?:password|pwd)\s*=\s*([^;"'\r\n]+)/gim,
 yaml_secret:/^\s*(?:MSSQL_SA_PASSWORD|SQL_PASSWORD|API_KEY|ACCESS_TOKEN)\s*:\s*([^\s#${]+)/gim,
 connection_remote_host:/(?:^|[;"'])\s*(?:server|data source)\s*=\s*(?!localhost\b|127\.0\.0\.1\b|\(localdb\)|\(local\)|[.;'"\s<${])([^;"'\r\n]+)/gim
};
const textExtensions=new Set(['.cs','.csproj','.props','.json','.xml','.config','.sql','.md','.yml','.yaml','.ps1','.js','.mjs','.txt','.http','.env']);
const isQA=p=>p.startsWith('tests/')||p==='.github/workflows/backend.yml';
function inspect(data,p,blob){
 const text=data.toString('utf8'),rows=[];
 for(const [kind,regex] of Object.entries(patterns))for(const match of text.matchAll(regex))
  rows.push({path:p,line:text.slice(0,match.index).split('\n').length,kind,classification:isQA(p)?'QA_FIXTURE':'REVIEW',...(blob?{blob}:{})});
 return rows;
}
let trackedTextFiles=0;const current=[];
for(const p of git(['ls-files','-z']).toString().split('\0')){
 if(!p||!textExtensions.has(path.extname(p).toLowerCase())||!fs.existsSync(path.join(root,p)))continue;
 trackedTextFiles++;current.push(...inspect(fs.readFileSync(path.join(root,p)),p));
}
let historyTextBlobs=0;const history=[];
if(process.argv.includes('--history')){
 const objects=git(['rev-list','--objects','--all']).toString().split('\n').map(line=>{
  const space=line.indexOf(' ');return space<0?null:{sha:line.slice(0,space),p:line.slice(space+1)};
 }).filter(row=>row&&textExtensions.has(path.extname(row.p).toLowerCase()));
 const batch=git(['cat-file','--batch'],objects.map(row=>row.sha+'\n').join(''));let offset=0;
 for(const row of objects){
  const end=batch.indexOf(10,offset);if(end<0)throw Error('Truncated git batch');
  const [sha,type,sizeText]=batch.subarray(offset,end).toString().split(' '),size=Number(sizeText);offset=end+1;
  if(!Number.isSafeInteger(size)||size<0||offset+size>batch.length)throw Error('Invalid git object length');
  const data=batch.subarray(offset,offset+size);offset+=size+1;
  if(type==='blob'){historyTextBlobs++;history.push(...inspect(data,row.p,sha));}
 }
}
const report={head:git(['rev-parse','HEAD']).toString().trim(),trackedTextFiles,historyTextBlobs,
 scope:'Tracked current files and all locally reachable refs. Ignored/local environment files and external databases are excluded.',
 limitations:'Pattern inventory plus manual review; REVIEW is not a claim that a credential is live. No credential validation, values, source lines or secret fingerprints are exported.',current,history};
fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(report,null,2));
console.log(JSON.stringify({trackedTextFiles,historyTextBlobs,currentReview:current.filter(x=>x.classification==='REVIEW').length,
 currentQA:current.filter(x=>x.classification==='QA_FIXTURE').length,historyReview:history.filter(x=>x.classification==='REVIEW').length,report:output}));
