import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditSearchInput} from './verify-search-input.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return process.argv[index+1];};
const native=path.resolve(option('--native')),output=path.resolve(option('--output')),width=Number(option('--width'));
await auditSearchInput(native,width);const workspace=path.join(path.dirname(output),'search-input-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});
const original=JSON.parse(await fs.readFile(path.join(native,'search-input.json'),'utf8')),desktop=JSON.parse(await fs.readFile(path.join(native,'search-input-desktop.json'),'utf8'));const negatives=[];
for(const id of ['shifted-clear','missing-case','lost-binding','missing-pointer-receipt','wrong-external-value','same-process-client','wrong-paint-alpha','unexpected-click']){
 const changed=structuredClone(original),input=structuredClone(desktop);
 if(id==='shifted-clear')changed.reports.find(item=>item.clear).clear.box.x+=1;
 if(id==='missing-case')changed.reports.pop();
 if(id==='lost-binding')changed.inputs[0].twoWayBinding=false;
 if(id==='missing-pointer-receipt')input.pointerClicks[0].receipts=[];
 if(id==='wrong-external-value')changed.accessibility[0].tree.editors[0].value='stale';
 if(id==='same-process-client')changed.accessibility[0].tree.clientProcess=changed.accessibility[0].tree.process;
 if(id==='wrong-paint-alpha')changed.reports[0].editor.background.opacity=.99;
 if(id==='unexpected-click')input.pointerClicks.push(structuredClone(input.pointerClicks.find(click=>click.target==='ClearSearch')));
 await fs.writeFile(path.join(workspace,'search-input.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'search-input-desktop.json'),JSON.stringify(input));
 await assert.rejects(()=>auditSearchInput(workspace,width));negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,width,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,width,negatives}));
