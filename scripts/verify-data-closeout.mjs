// Read-only release gate. This program never merges, tags, pushes or changes Git refs.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import cp from 'node:child_process';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const git=args=>cp.execFileSync('git',args,{cwd:root,encoding:'utf8',stdio:['ignore','pipe','pipe']}).trim();
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const official=[...['11_DAC_TA_DU_LIEU_DEMO.md','13_CHECKLIST_QUET_DU_LIEU_CU.md'].flatMap(name=>[...fs.readFileSync(path.join(root,'docs/demo-data/sources',name),'utf8').matchAll(/^- \[ \] \[(P[012])\] ((?:DATA|CLEAN)\.[A-Z]+\.\d+)/gm)].map(m=>({id:m[2],priority:m[1]})))];
function validate(report,head,tree,ci){
 const errors=[];
 if(official.length!==76||new Set(official.map(x=>x.id)).size!==76)errors.push('Official checklist must contain exactly 43 DATA and 33 CLEAN IDs');
 if(report.candidateSha!==head)errors.push('Report is not bound to current candidate SHA');
 if(report.tree!==tree)errors.push('Report content tree differs');
 if(ci.headSha!==head||ci.conclusion!=='success'||ci.status!=='completed'||!Array.isArray(ci.jobs)||ci.jobs.length<2||ci.jobs.some(j=>j.conclusion!=='success'))errors.push('Exact-candidate CI is not completed SUCCESS for every job');
 const records=Array.isArray(report.records)?report.records:[];
 if(records.length!==official.length||new Set(records.map(x=>x.id)).size!==official.length)errors.push('Missing or duplicate checklist records');
 for(const item of official){
  const row=records.find(x=>x.id===item.id);
  const exception=item.id==='CLEAN.FILES.04' && row?.status==='ACCEPTED_EXCEPTION' &&
   row.approvalRef==='docs/demo-data/RELEASE_CLOSEOUT.md#approval-legacy-seeder-2026-11-04' &&
   typeof row.reason==='string' && row.reason.trim().length>=30;
  const statusOk=item.id==='CLEAN.FILES.04'?exception:row?.status==='PASS';
  if(!row||row.priority!==item.priority||!statusOk||!Array.isArray(row.evidence)||row.evidence.length===0)
   errors.push(item.id+': requires '+(item.id==='CLEAN.FILES.04'?'documented ACCEPTED_EXCEPTION':'PASS')+' with evidence and original priority');
 }
 const evidence=Array.isArray(report.evidence)?report.evidence:[];
 const keys=new Set(evidence.map(x=>x.id));
 for(const row of records)for(const key of row.evidence??[])if(!keys.has(key))errors.push(row.id+': missing referenced evidence');
 if(!evidence.length)errors.push('No executed evidence supplied');
 for(const e of evidence){if(e.candidateSha!==head||e.executed!==true||typeof e.path!=='string'||!/^\w[\w-]*$/.test(e.id??'')){errors.push('Evidence metadata is incomplete');continue;}
  const target=path.resolve(root,e.path);if(!target.startsWith(root+path.sep)||!fs.existsSync(target)){errors.push(e.id+': artifact missing or outside checkout');continue;}
  const digest=crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex');if(digest!==e.sha256)errors.push(e.id+': artifact checksum differs');
 }
 return {valid:errors.length===0,candidateSha:head,expectedRecords:76,counts:Object.fromEntries(['PASS','ACCEPTED_EXCEPTION','FAIL','BLOCKED','NOT RUN'].map(status=>[status,records.filter(x=>x.status===status).length])),errors};
}
if(process.argv.includes('--self-test')){
 const head='a'.repeat(40),tree='b'.repeat(40),ci={headSha:head,status:'completed',conclusion:'success',jobs:[{conclusion:'success'},{conclusion:'success'}]};
 const empty=validate({candidateSha:head,tree,records:[],evidence:[]},head,tree,ci);if(empty.valid)throw Error('Empty evidence was accepted');
 const declared=validate({candidateSha:'c'.repeat(40),tree,records:official.map(x=>({...x,status:'PASS',evidence:['missing']})),evidence:[]},head,tree,{...ci,headSha:'d'.repeat(40)});if(declared.valid||!declared.errors.some(x=>x.includes('candidate SHA'))||!declared.errors.some(x=>x.includes('CI'))||!declared.errors.some(x=>x.includes('missing referenced')))throw Error('False completion was accepted');
 const pending=validate({candidateSha:head,tree,records:official.map(x=>({...x,status:x.id==='CLEAN.REAL.01'?'NOT RUN':'PASS',evidence:['missing']})),evidence:[]},head,tree,ci);if(!pending.errors.some(x=>x.startsWith('CLEAN.REAL.01')))throw Error('Unverified image rights were accepted');
 const records=official.map(x=>({...x,status:x.id==='CLEAN.FILES.04'?'ACCEPTED_EXCEPTION':'PASS',evidence:['missing'],approvalRef:'docs/demo-data/RELEASE_CLOSEOUT.md#approval-legacy-seeder-2026-11-04',reason:'User approved keeping the unchanged runtime legacy seeder only.'}));
 const accepted=validate({candidateSha:head,tree,records,evidence:[]},head,tree,ci);
 if(accepted.errors.some(x=>x.startsWith('CLEAN.FILES.04: requires'))||accepted.counts.PASS!==75||accepted.counts.ACCEPTED_EXCEPTION!==1)throw Error('Approved exception not counted separately');
 for(const id of ['CLEAN.FILES.03','CLEAN.REAL.01']){const altered=structuredClone(records);altered.find(x=>x.id===id).status='ACCEPTED_EXCEPTION';if(!validate({candidateSha:head,tree,records:altered,evidence:[]},head,tree,ci).errors.some(x=>x.startsWith(id+': requires')))throw Error('Unapproved exception accepted');}
 const invalid=structuredClone(records);invalid.find(x=>x.id==='CLEAN.FILES.04').approvalRef='unverified';if(!validate({candidateSha:head,tree,records:invalid,evidence:[]},head,tree,ci).errors.some(x=>x.startsWith('CLEAN.FILES.04: requires')))throw Error('Missing approval reference accepted');
 console.log('PASS closeout gate: exact 76 IDs, no evidence/duplicate declarations, wrong SHA/CI, pending rights rejected; no ref mutations');
}else{
 const value=flag=>{const i=process.argv.indexOf(flag);return i<0?null:process.argv[i+1];};const reportPath=value('--report'),run=value('--ci-run');
 if(!reportPath||!/^\d+$/.test(run??''))throw Error('Use --report <evidence.json> --ci-run <numeric run ID>, or --self-test');
 const ci=JSON.parse(cp.execFileSync('gh',['run','view',run,'--json','headSha,status,conclusion,jobs,url'],{cwd:root,encoding:'utf8',stdio:['ignore','pipe','pipe']}));
 const result=validate(json(path.resolve(reportPath)),git(['rev-parse','HEAD']),git(['rev-parse','HEAD^{tree}']),ci);
 const scope=[['backend-v1.0',['src/PhoneStore.Infrastructure']],['web-v1.0',['src/PhoneStore.Web']],['admin-v1.0',['src/PhoneStore.Admin','src/PhoneStore.Admin.Client']]];
 for(const [tag,paths]of scope){if(cp.spawnSync('git',['merge-base','--is-ancestor',tag,'HEAD'],{cwd:root}).status!==0)result.errors.push(tag+': baseline tag is not an ancestor');const rows=git(['diff','--name-status',tag,'HEAD','--',...paths]).split('\n').filter(Boolean);for(const row of rows)if(!/^A\t(src\/PhoneStore.Web\/wwwroot\/images\/.+\.(?:svg|png|jpe?g|webp|avif|gif))$/u.test(row))result.errors.push('Baseline runtime diff outside new static images: '+row);}
 result.valid=result.errors.length===0;console.log(JSON.stringify(result,null,2));process.exitCode=result.valid?0:1;
}
