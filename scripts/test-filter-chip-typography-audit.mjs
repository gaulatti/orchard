import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditFilterChipTypography} from './verify-filter-chip-typography.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'filter-chip-typography-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const original=JSON.parse(await fs.readFile(path.join(native,'filter-chip-typography.json'),'utf8'));
await auditFilterChipTypography(workspace);const negatives=[];
for(const id of ['wrong-style','stale-width','missing-baseline','wrong-rtl-order']){
 const changed=structuredClone(original);
 if(id==='wrong-style')changed.reports.find(item=>item.phase==='Light/italic-expanded').texts[0].style='Normal';
 if(id==='stale-width')changed.reports[0].texts[0].box.Item3+=1;
 if(id==='missing-baseline')delete changed.reports[0].texts[0].baseline;
 if(id==='wrong-rtl-order')changed.reports.find(item=>item.phase==='Dark/rtl').chips.reverse();
 await fs.writeFile(path.join(workspace,'filter-chip-typography.json'),JSON.stringify(changed));await assert.rejects(()=>auditFilterChipTypography(workspace));negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives}));
