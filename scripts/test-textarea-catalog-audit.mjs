import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextareaCatalog} from './verify-textarea-catalog.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const native=path.resolve(option('--native')),output=path.resolve(option('--output'));await auditTextareaCatalog(native);
const workspace=path.join(path.dirname(output),'textarea-catalog-negative-'+Date.now());await fs.mkdir(workspace);
for(const name of['result.json',...['Light','Dark'].flatMap(s=>[`textarea-catalog-${s}.json`,`textarea-catalog-${s}-desktop.json`,`textareas-${s}.png`])])await fs.copyFile(path.join(native,name),path.join(workspace,name));
const read=async file=>JSON.parse((await fs.readFile(path.join(native,file),'utf8')).replace(/^\uFEFF/,''));
const original=await read('textarea-catalog-Light.json'),originalDesktop=await read('textarea-catalog-Light-desktop.json'),negatives=[];
for(const id of['missing-phase','stale-height','stale-native-spacing','lost-consumer','readonly-mutated','readonly-guard-lost','stale-theme','wrong-theme-paint','lost-validity','not-restored','missing-key-up','wrong-key-target']){
 const report=structuredClone(original),desktop=structuredClone(originalDesktop);
 if(id==='missing-phase')report.phases.pop();if(id==='stale-height')report.phases[2].height=128;if(id==='stale-native-spacing')report.phases[2].nativeLineSpacing=15;if(id==='lost-consumer')report.phases[5].consumer='Stale';
 if(id==='readonly-mutated')report.phases[6].text+='b';if(id==='readonly-guard-lost')report.phases[6].nativeReadOnly=false;if(id==='stale-theme')report.phases[7].actualTheme='Light';if(id==='wrong-theme-paint')report.phases[7].border='#66E6D5B8';
 if(id==='lost-validity')report.phases[9].explicitFalseValid=false;if(id==='not-restored')report.restored=false;if(id==='missing-key-up')desktop.keyActions[0].ups=0;if(id==='wrong-key-target')desktop.keyActions[0].target='Other';
 await fs.writeFile(path.join(workspace,'textarea-catalog-Light.json'),JSON.stringify(report));await fs.writeFile(path.join(workspace,'textarea-catalog-Light-desktop.json'),JSON.stringify(desktop));await assert.rejects(auditTextareaCatalog(workspace),undefined,id);negatives.push(id);
}
await fs.writeFile(output,JSON.stringify({passed:true,rejectedCorruptions:negatives,workspace},null,2)+'\n');console.log(JSON.stringify({passed:true,rejectedCorruptions:negatives.length}));
