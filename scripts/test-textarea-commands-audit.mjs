import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextareaCommands} from './verify-textarea-commands.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const native=path.resolve(option('--native')),width=Number(option('--width')),output=path.resolve(option('--output'));
await auditTextareaCommands(native,width);const workspace=path.join(path.dirname(output),'textarea-commands-negative-'+Date.now());await fs.cp(native,workspace,{recursive:true,errorOnExist:true});
const original=JSON.parse(await fs.readFile(path.join(native,'textarea-commands.json'),'utf8')),keyOriginal=JSON.parse(await fs.readFile(path.join(native,'textarea-commands-context-key-Light-copy.json'),'utf8')),pointerOriginal=JSON.parse(await fs.readFile(path.join(native,'textarea-commands-menu-pointer-Light-copy.json'),'utf8')),ownerOriginal=JSON.parse(await fs.readFile(path.join(native,'textarea-command-ownership-Light.json'),'utf8')),negatives=[];
for(const id of['missing-state','rich-copy','wrong-clipboard','lost-binding','wrong-undo','readonly-mutated','unconstrained-menu','lost-focus','missing-key-up','foreign-key-source','empty-key-receipts','empty-key-counts','missing-pointer-up','empty-pointer-receipts','owner-not-verified']){
 const changed=structuredClone(original),key=structuredClone(keyOriginal),pointer=structuredClone(pointerOriginal),owner=structuredClone(ownerOriginal),byId=id=>changed.frames.find(f=>f.id===id);
 if(id==='missing-state')changed.frames.pop();if(id==='rich-copy')byId('copy').rtf=true;if(id==='wrong-clipboard')byId('copy').clipboardText='Wrong clipboard';if(id==='lost-binding')byId('cut').consumerText='First\nSecond';
 if(id==='wrong-undo')byId('undo').text='';if(id==='readonly-mutated')byId('readonly-cut').invoked=true;if(id==='unconstrained-menu')byId('copy').menuConstrained=false;if(id==='lost-focus')byId('copy').focus='Unfocused';
 if(id==='missing-key-up')key.counts['16']=[1,0];if(id==='foreign-key-source')key.receipts[0].exactSource=false;if(id==='missing-pointer-up')pointer.ups=0;if(id==='owner-not-verified')owner.phases.pop();
 if(id==='empty-key-receipts')key.receipts=[];if(id==='empty-key-counts')key.counts={};if(id==='empty-pointer-receipts')pointer.receipts=[];
 await fs.writeFile(path.join(workspace,'textarea-commands.json'),JSON.stringify(changed));await fs.writeFile(path.join(workspace,'textarea-commands-context-key-Light-copy.json'),JSON.stringify(key));await fs.writeFile(path.join(workspace,'textarea-commands-menu-pointer-Light-copy.json'),JSON.stringify(pointer));await fs.writeFile(path.join(workspace,'textarea-command-ownership-Light.json'),JSON.stringify(owner));
 await assert.rejects(auditTextareaCommands(workspace,width),undefined,id);negatives.push(id);
}
await fs.writeFile(output,JSON.stringify({width,rejectedCorruptions:negatives,workspace},null,2)+'\n');console.log(JSON.stringify({width,rejectedCorruptions:negatives.length}));
