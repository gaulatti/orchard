import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextareaLimits} from './verify-textarea-limits.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const native=path.resolve(option('--native')),width=Number(option('--width')),output=path.resolve(option('--output'));
await auditTextareaLimits(native,width);
const workspace=path.join(path.dirname(output),'textarea-limits-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});
const original=JSON.parse(await fs.readFile(path.join(native,'textarea-limits.json'),'utf8')),deferred=JSON.parse(await fs.readFile(path.join(native,'textarea-deferred-Light.json'),'utf8')),menuOriginal=JSON.parse(await fs.readFile(path.join(native,'textarea-menu-limits-Light.json'),'utf8')),negatives=[];
for(const id of['missing-case','paste-over-limit','split-surrogate','wrong-newline','lost-selection','lost-binding','lost-undo','lost-limit','deferred-old-limit','stale-provider-write','menu-bypassed-limit','readonly-menu-enabled']){
 const changed=structuredClone(original),late=structuredClone(deferred),menu=structuredClone(menuOriginal),byId=id=>changed.frames.find(f=>f.id===id);
 if(id==='missing-case')changed.frames.pop();
 if(id==='paste-over-limit')byId('paste').text='abcdef';
 if(id==='split-surrogate')byId('surrogate-boundary').text='A\ud83d';
 if(id==='wrong-newline')byId('paste-crlf').text='ab\rcd';
 if(id==='lost-selection')byId('selection').selection=[5,5];
 if(id==='lost-binding')byId('paste').consumerText='Old value';
 if(id==='lost-undo')byId('paste').undo='abcde';
 if(id==='lost-limit')byId('live-limit').MaxLength=5;
 if(id==='deferred-old-limit')late.cases.find(c=>c.action==='limit-decrease').text='abcde';
 if(id==='stale-provider-write')late.cases.find(c=>c.action==='remount').text='abcde';
 if(id==='menu-bypassed-limit')menu.text='abcde';
 if(id==='readonly-menu-enabled')menu.readOnlyPasteDisabled=false;
 await fs.writeFile(path.join(workspace,'textarea-limits.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'textarea-deferred-Light.json'),JSON.stringify(late));
 await fs.writeFile(path.join(workspace,'textarea-menu-limits-Light.json'),JSON.stringify(menu));
 await assert.rejects(auditTextareaLimits(workspace,width),undefined,id);negatives.push(id);
}
await fs.writeFile(output,JSON.stringify({width,rejectedCorruptions:negatives,workspace},null,2)+'\n');console.log(JSON.stringify({width,rejectedCorruptions:negatives.length}));
