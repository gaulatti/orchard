import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {pathToFileURL} from 'node:url';
const hash=bytes=>createHash('sha256').update(bytes).digest('hex');
export const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function loadProgressStackAccessibility(native){
 const referenceBytes=await fs.readFile(new URL('../tests/Reference/ProgressStackAccessibility.json',import.meta.url));
 const reference=JSON.parse(referenceBytes);
 assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-progress-stack-accessibility.cjs',import.meta.url))));
 assert.equal(reference.textReferenceSha256,hash(await fs.readFile(new URL('../tests/Reference/ProgressStackText.json',import.meta.url))));
 const source=await read(new URL('../tests/Reference/ProgressStackText.json',import.meta.url));
 assert.equal(reference.cases.length,36);
 for(const row of reference.cases){const text=source.cases.find(text=>text.id===row.id&&text.scheme===row.scheme&&text.motion==='reduce');assert(text);assert.equal(row.sourcePngSha256,text.pngSha256);assert.deepEqual(row.texts,text.spec.items.flatMap(item=>[item.label,String(Number(item.value))]));}
 return{reference,report:await read(path.join(native,'progress-stack-accessibility.json')),runtime:await read(path.join(native,'result.json')),updates:await Promise.all(['Light','Dark'].map(theme=>read(path.join(native,`uia-updates-${theme}.json`))))};
}
export function auditProgressStackAccessibility({reference,report,runtime,updates}){
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'progress-stack-accessibility');
 assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);
 assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);assert.equal(report.snapshots.length,36);
 let textNodes=0,updateStates=0,nameEvents=0;
 const keys=new Set();
 function tree(actual,expected){
  assert(actual.process>0&&actual.clientProcess>0&&actual.process!==actual.clientProcess,'External UIA process boundary');
  const group=actual.group;assert.equal(group.type,'ControlType.Group');assert.equal(group.name,'Consumer distribution');assert.equal(group.focusable,false);assert.equal(group.hasInvoke,false);assert.equal(group.hasRange,false);
  const texts=actual.elements.filter(item=>item.type==='ControlType.Text'&&(item.control||item.content));
  assert.deepEqual(texts.map(item=>item.name),expected,'Source logical accessible names');
  for(const item of texts){assert.equal(item.control,true);assert.equal(item.content,true);assert.equal(item.focusable,false);assert.equal(item.hasText,true);assert.equal(item.hasInvoke,false);assert.equal(item.hasRange,false);assert.equal(typeof item.text,'string');assert(item.runtimeId.length>0);}
  return texts;
 }
 for(const snapshot of report.snapshots){
  const key=snapshot.scheme+'/'+snapshot.id;assert(!keys.has(key));keys.add(key);
  const source=reference.cases.find(row=>row.id===snapshot.id&&row.scheme===snapshot.scheme);assert(source);
  assert.deepEqual(snapshot.labels.flatMap((label,index)=>[label,snapshot.values[index]]),source.texts);
  const texts=tree(snapshot.tree,source.texts);assert.deepEqual(texts.map(item=>item.text),snapshot.painted,'Native TextPattern retains actual painted text');textNodes+=texts.length;
 }
 assert.equal(textNodes,144);assert.equal(updates.length,2);
 for(const theme of ['Light','Dark']){
  const update=updates.find(item=>item.scheme===theme);assert(update);
  const required=['initial','live-name-event','widen','reorder','hide','show','remount','retemplate','clear'];
  for(const phase of required)assert(update.states.some(row=>row.phase===phase),'Missing lifecycle phase '+phase);
  const initial=update.states[0];assert.equal(initial.phase,'initial');
  const changed=update.states.findLast(row=>row.phase==='live-name-event');
  assert.deepEqual(changed.tree.retained.runtimeId,initial.tree.retained.runtimeId,'Retained text provider identity');
  assert.equal(changed.tree.retained.name,'日本語の公開ワークフロー');
  assert(changed.tree.nameEvents.includes('日本語の公開ワークフロー'),'External Name property event');nameEvents++;
  for(const state of update.states){tree(state.tree,state.labels.flatMap((label,index)=>[label,state.values[index]]));updateStates++;}
  const wide=update.states.find(row=>row.phase==='widen');assert.deepEqual(wide.tree.retained.runtimeId,initial.tree.retained.runtimeId);assert.equal(wide.tree.retained.text,'日本語の公開ワークフロー');
  assert.equal(update.states.find(row=>row.phase==='hide').tree.elements.filter(item=>item.control||item.content).length,0);
  assert.equal(update.states.find(row=>row.phase==='clear').tree.elements.filter(item=>item.control||item.content).length,0);
 }
 return{passed:true,sourceCases:36,externalTextNodes:textNodes,updateStates,externalNameEventTransitions:nameEvents,logicalNamesMatchSource:true,nativeTextPatternRetained:true,narratorVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
 const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
 const result=auditProgressStackAccessibility(await loadProgressStackAccessibility(option('--native')));
 await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
