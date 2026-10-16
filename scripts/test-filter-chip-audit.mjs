import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditFilterChip} from './verify-filter-chip.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return process.argv[index+1];};
const native=path.resolve(option('--native')),output=path.resolve(option('--output')),width=Number(option('--width'));
const workspace=path.join(path.dirname(output),'filter-chip-negative-'+Date.now());await fs.mkdir(workspace,{recursive:true});
for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const original=await read(path.join(native,'filter-chip.json')),desktop=await read(path.join(native,'filter-chip-desktop.json'));
await auditFilterChip(workspace,width);const negatives=[];
for(const id of ['shifted-baseline','wrong-remove-name','wrong-border-alpha','lost-template-lifecycle','missing-pointer-receipt']){
 const changed=structuredClone(original),input=structuredClone(desktop);
 if(id==='shifted-baseline')changed.reports[0].chips[0].texts[0].baseline+=.1;
 if(id==='wrong-remove-name')changed.reports.find(test=>test.id==='removable').chips[0].remove.name='Wrong';
 if(id==='wrong-border-alpha')changed.reports[0].chips[0].border.opacity=1;
 if(id==='lost-template-lifecycle')changed.inputs[0].templateReplacement=false;
 if(id==='missing-pointer-receipt')input.pointerClicks[0].receipts=[];
 await fs.writeFile(path.join(workspace,'filter-chip.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'filter-chip-desktop.json'),JSON.stringify(input));
 await assert.rejects(()=>auditFilterChip(workspace,width));negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,width,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,width,negatives}));
