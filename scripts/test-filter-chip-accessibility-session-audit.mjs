import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditFilterChipSession} from './verify-filter-chip-accessibility-session.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'filter-chip-session-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const original=JSON.parse((await fs.readFile(path.join(native,'filter-chip-accessibility-session.json'),'utf8')).replace(/^\uFEFF/,''));
await auditFilterChipSession(workspace);const negatives=[];
for(const id of ['missing-name-event','reused-old-event','changed-client','replaced-retained-pattern','stale-callback']){
 const changed=structuredClone(original);
 if(id==='missing-name-event')for(const row of changed.reports)row.response.events=row.response.events.filter(event=>!(event.slot==='single-old'&&event.value==='Bound remove updated'));
 if(id==='reused-old-event')changed.checks.find(check=>check.id==='command-enabled').sequence=changed.checks.find(check=>check.id==='command-disabled').sequence;
 if(id==='changed-client')changed.reports.find(row=>row.request==='events').response.clientProcess++;
 if(id==='replaced-retained-pattern')changed.reports.find(row=>row.request==='invoke\tsingle-old').response.runtimeId=[99,99];
 if(id==='stale-callback')changed.checks.find(check=>check.id==='template-old').actual++;
 await fs.writeFile(path.join(workspace,'filter-chip-accessibility-session.json'),JSON.stringify(changed));await assert.rejects(()=>auditFilterChipSession(workspace));negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives}));
