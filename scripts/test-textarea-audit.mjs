import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextarea} from './verify-textarea.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const native=path.resolve(option('--native')),width=Number(option('--width')),output=path.resolve(option('--output'));
await auditTextarea(native,width);
const workspace=path.join(path.dirname(output),'textarea-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});
const original=JSON.parse(await fs.readFile(path.join(native,'textarea.json'),'utf8')),desktop=JSON.parse(await fs.readFile(path.join(native,'textarea-desktop.json'),'utf8'));
const negatives=[];
for(const id of['missing-frame','wrong-height','wrong-border-alpha','wrong-font','wrong-line-spacing','lost-readonly-provider','wrong-uia-text','lost-key-owner','lost-consumer-binding','rich-paste-retained']){
 const changed=structuredClone(original),input=structuredClone(desktop);
 if(id==='missing-frame')changed.frames.pop();
 if(id==='wrong-height')changed.frames[0].height++;
 if(id==='wrong-border-alpha')changed.frames[0].borderOpacity=.5;
 if(id==='wrong-font')changed.frames[0].nativeFont='Times New Roman';
 if(id==='wrong-line-spacing')changed.frames[0].lineSpacing=16;
 if(id==='lost-readonly-provider')changed.interactions[0].readonlyUia.readOnly=false;
 if(id==='wrong-uia-text')changed.interactions[0].editableUia.text='Wrong document';
 if(id==='lost-key-owner')input.keyActions[0].targetRetained=false;
 if(id==='lost-consumer-binding')changed.interactions[0].twoWayBinding=false;
 if(id==='rich-paste-retained')changed.interactions[0].richClipboardRejected=false;
 await fs.writeFile(path.join(workspace,'textarea.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'textarea-desktop.json'),JSON.stringify(input));
 let rejected=false;try{await auditTextarea(workspace,width);}catch{rejected=true;}assert(rejected,'Audit accepted corruption '+id);negatives.push({id,rejected});
}
await fs.writeFile(output,JSON.stringify({passed:true,width,negatives,workspace},null,2)+'\n');console.log(JSON.stringify({passed:true,width,rejected:negatives.length}));
