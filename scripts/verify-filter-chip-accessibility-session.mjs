import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditFilterChipSession(native){
 const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'filter-chip-accessibility-session.json'));
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'filter-chip-accessibility-session');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);
 const expectedEvents=[['native-name-health','reference','name','Native reference updated'],['label-name','single-old','name','Remove Updated label filter'],['explicit-name','single-old','name','Consumer remove name'],['bound-name','single-old','name','Bound remove'],['live-bound-name','single-old','name','Bound remove updated'],['linked-name','single-old','name','Linked name'],['live-linked-name','single-old','name','Linked name updated'],['linked-metadata-name','single-old','name','Linked automation name'],['name-priority','single-old','name','Consumer remove name'],['command-disabled','group-old','enabled','False'],['command-enabled','group-old','enabled','True'],['clear-name','clear-old','name','Clear chosen filters']];
 const expectedInvokes=[['single-live','single-old',1],['disabled','group-old',0],['command-live','group-old',1],['template-empty','single-old',1],['template-old','single-old',1],['template-new','single-new',2],['removed-model','group-old',1],['empty-clear','clear-old',0],['unloaded','single-new',2],['remounted','single-remounted',3]];
 expectedEvents.splice(11,0,['group-disabled','group-old','enabled','False'],['group-enabled','group-old','enabled','True']);
 expectedInvokes.splice(3,0,['disabled-parent','group-old',1]);
 const live=new Set(['single-live','command-live','template-new','remounted']);let maximumEventMilliseconds=0;
 const clientProcesses=[];let catalogProcess;
 for(const theme of ['Light','Dark']){
  const messages=report.reports.filter(row=>row.scheme===theme),checks=report.checks.filter(row=>row.scheme===theme);assert(messages.length>0);
  const client=messages[0].response.clientProcess;assert(Number.isInteger(client)&&client>0);assert(!clientProcesses.includes(client));clientProcesses.push(client);
  const retained=new Map();
  for(const message of messages){
   const response=message.response;assert.equal(response.clientProcess,client);catalogProcess??=response.process;assert.equal(response.process,catalogProcess);assert.notEqual(client,catalogProcess);
   const [verb,slot]=message.request.split('\t');assert.equal(response.command,verb);
   if(verb==='retain'){assert(!retained.has(slot));assert(Array.isArray(response.runtimeId)&&response.runtimeId.length>0);retained.set(slot,response.runtimeId);}
   if(verb==='invoke'){assert(retained.has(slot));assert.deepEqual(response.runtimeId,retained.get(slot));}
  }
  assert.deepEqual([...retained.keys()],['reference','single-old','group-old','clear-old','single-new','single-remounted']);assert.equal(messages.at(-1).request,'quit');
  let sequence=0;
  for(const [id,slot,property,value]of expectedEvents){
   const matches=checks.filter(check=>check.id===id);assert.equal(matches.length,1);const check=matches[0];assert.equal(check.slot,slot);assert.equal(check.property,property);assert.equal(check.value,value);assert.equal(check.delivered,true);assert.equal(check.since,sequence);assert(check.sequence>sequence);sequence=check.sequence;
   assert(check.milliseconds>=0&&check.milliseconds<2000);maximumEventMilliseconds=Math.max(maximumEventMilliseconds,check.milliseconds);
   assert(messages.some(message=>message.response.events.some(event=>event.sequence===check.sequence&&event.slot===slot&&event.property===property&&event.value===value)));
  }
  for(const [id,slot,count]of expectedInvokes){
   const matches=checks.filter(check=>check.id===id);assert.equal(matches.length,1);const check=matches[0];assert.equal(check.slot,slot);assert.equal(check.expected,count);assert.equal(check.actual,count);
   if(live.has(id))assert.equal(check.outcome,'invoked');else assert(['element-not-available','element-not-enabled'].includes(check.outcome));
  }
  const final=checks.find(check=>check.id==='final-consumers');assert(final);assert.equal(final.singleCalls,3);assert.equal(final.commandCalls,1);assert.equal(final.clearCalls,0);assert.equal(final.filters,0);
  assert.equal(checks.length,expectedEvents.length+expectedInvokes.length+1);assert.equal(messages.filter(message=>message.request.startsWith('invoke\t')).length,11);
  const png=await fs.readFile(path.join(native,`catalog-${theme.toLowerCase()}.png`));assert.equal(png.subarray(1,4).toString(),'PNG');
 }
 return{passed:true,persistentClients:2,propertyEventChecks:28,componentPropertyEventChecks:26,retainedInvocations:22,currentInvocations:8,blockedOrInactiveInvocations:14,maximumEventMilliseconds,eventDeadlineMilliseconds:2000,retainedPatternsNeverRequeried:true,narratorVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
 const report=await auditFilterChipSession(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
