import fs from 'node:fs';
const file=process.argv[2];let module='ENV',section='';const counters={},plan=[];
for(const line of fs.readFileSync(file,'utf8').split(/\r?\n/)){
 const h=line.match(/^([3-9])\. /);if(h){module=({'3':'ENV','4':'UC','5':'API','6':'DB','7':'UI','8':'SEC','9':'E2E'})[h[1]];section=line;}
 const u=line.match(/^(UC\d{2}) —/);if(u){module=u[1];section=line;}
 const title=line.match(/^\[ \] (.+)/);if(title){const n=counters[module]=(counters[module]||0)+1;plan.push({id:module+'-'+String(n).padStart(3,'0'),module,source:section,title:title[1],expected:title[1]});}
}
fs.writeFileSync('qa/plan.json',JSON.stringify(plan,null,2));
let md='# Checklist QA\n\nNOT RUN = chưa chạy trong đợt QA này. Từng trường hợp có dữ liệu/bước/bằng chứng riêng sau khi thực hiện.\n';
for(const m of new Set(plan.map(x=>x.module))){md+='\n## '+m+'\n\n| ID | Use case/module | Yêu cầu nguồn | Trường hợp kiểm tra | Điều kiện/dữ liệu | Các bước | Kết quả mong đợi | Kết quả thực tế | Trạng thái | Bằng chứng | Mã lỗi |\n|---|---|---|---|---|---|---|---|---|---|---|\n';for(const t of plan.filter(x=>x.module===m))md+=`| ${t.id} | ${m} | ${t.source} | ${t.title} | DB QA riêng | Đang chuẩn bị test | ${t.expected} | Chưa chạy | NOT RUN | — | — |\n`;}
fs.writeFileSync('QA_CHECKLIST.md',md);console.log(plan.length+' minimum cases planned');
