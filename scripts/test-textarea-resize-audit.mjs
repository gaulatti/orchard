import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextareaResize} from './verify-textarea-resize.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const native=path.resolve(option('--native')),width=Number(option('--width')),output=path.resolve(option('--output'));
await auditTextareaResize(native,width);
const workspace=path.join(path.dirname(output),'textarea-resize-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});
const file=path.join(workspace,'textarea-resize.json'),original=JSON.parse(await fs.readFile(file,'utf8')),negatives=[];
for(const id of['missing-case','disabled-blocked','lost-readonly','wrong-rtl-edge','minimum-reversal','focus-stolen','text-changed','capture-missing','lifecycle-leak','synthetic-only']){
 const changed=structuredClone(original),first=changed.frames[0];
 if(id==='missing-case')changed.frames.pop();
 if(id==='disabled-blocked')changed.frames.find(f=>f.id==='md-disabled-ltr').enlarged.height-=48;
 if(id==='lost-readonly')changed.frames.find(f=>f.id==='md-readonly-ltr').IsReadOnly=false;
 if(id==='wrong-rtl-edge')changed.frames.find(f=>f.id==='md-editable-rtl').gripBounds.X=width-32;
 if(id==='minimum-reversal')first.reversed.height+=20;
 if(id==='focus-stolen')first.focus='Pointer';
 if(id==='text-changed')first.Text='Changed';
 if(id==='capture-missing')first.captured=false;
 if(id==='lifecycle-leak')changed.frames.find(f=>f.id==='lg-disabled-rtl').lifecycleCancellation=false;
 if(id==='synthetic-only')first.receipts.forEach(r=>r.IsGenerated=true);
 await fs.writeFile(file,JSON.stringify(changed));await assert.rejects(auditTextareaResize(workspace,width),undefined,id);negatives.push(id);
}
await fs.writeFile(file,JSON.stringify(original));
await fs.writeFile(output,JSON.stringify({width,rejectedCorruptions:negatives,workspace},null,2)+'\n');console.log(JSON.stringify({width,rejectedCorruptions:negatives.length}));
