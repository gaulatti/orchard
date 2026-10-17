import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditFilterChipAccessibility} from './verify-filter-chip-accessibility.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'filter-chip-accessibility-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
await auditFilterChipAccessibility(workspace);const negatives=[];
for(const id of ['stale-bound-name','enabled-disabled-action','accepted-disabled-invoke','missing-readonly-text','lost-tab-release']){
 const file=id==='lost-tab-release'?'filter-chip-accessibility.json':id==='stale-bound-name'?'uia-live-bound-name-Light.json':id==='missing-readonly-text'?'uia-default-Dark.json':'uia-command-disabled-Light.json';
 const original=await fs.readFile(path.join(workspace,file)),changed=JSON.parse(original);
 if(id==='stale-bound-name')changed.buttons.find(button=>button.id==='FilterA11y-Single').name='Remove selected desk';
 if(id==='enabled-disabled-action')changed.buttons.find(button=>button.id==='FilterA11y-Desk').enabled=true;
 if(id==='accepted-disabled-invoke')changed.invokeOutcome='invoked';
 if(id==='missing-readonly-text')changed.texts=changed.texts.filter(text=>text!=='Português');
 if(id==='lost-tab-release')changed.tabs[0].upOwned=false;
 await fs.writeFile(path.join(workspace,file),JSON.stringify(changed));await assert.rejects(()=>auditFilterChipAccessibility(workspace));await fs.writeFile(path.join(workspace,file),original);negatives.push({id,rejected:true});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives}));
