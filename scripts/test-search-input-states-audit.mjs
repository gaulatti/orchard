import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import {auditSearchInputStates} from './verify-search-input-states.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const native=path.resolve(option('--native')),output=path.resolve(option('--output')),width=Number(option('--width'));await auditSearchInputStates(native,width);
const workspace=path.join(path.dirname(output),'search-input-states-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});const original=JSON.parse(await fs.readFile(path.join(native,'search-input-states.json'),'utf8')),pointer=JSON.parse(await fs.readFile(path.join(native,'search-input-states-held-pointer.json'),'utf8'));const negatives=[];
for(const id of['enter-on-release','missing-edge-receipt','wrong-hover-alpha','wrong-pressed-size','missing-state','lost-focus','unscaled-focus-outline','missing-paint-gate']){
 const changed=structuredClone(original),input=structuredClone(pointer);
 if(id==='enter-on-release')changed.reports.find(r=>r.id==='clear-enter-pressed').state.clearActions=0;
 if(id==='missing-edge-receipt')input[1].receipts[1].owned=false;
 if(id==='wrong-hover-alpha')changed.reports.find(r=>r.id==='clear-hover').state.clear.background.opacity=.5;
 if(id==='wrong-pressed-size')changed.reports.find(r=>r.id==='clear-space-pressed').state.clear.paintedBox.width=18;
 if(id==='missing-state')changed.reports.pop();
 if(id==='lost-focus')changed.reports.find(r=>r.id==='clear-focus').state.clear.focus='Unfocused';
 if(id==='unscaled-focus-outline')changed.reports.find(r=>r.id==='clear-space-pressed').state.clear.focusCue.box.width=20;
 if(id==='missing-paint-gate')changed.paintGates[0].frames=[];
 await fs.writeFile(path.join(workspace,'search-input-states.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'search-input-states-held-pointer.json'),JSON.stringify(input));await assert.rejects(()=>auditSearchInputStates(workspace,width));negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,width,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,width,negatives}));
