import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';

const option=name=>{
 const at=process.argv.indexOf(name);
 assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);
 return path.resolve(process.argv[at+1]);
};
const native=option('--native'),output=option('--output');
const rows=(await fs.readFile(path.join(native,'catalog-runtime-profile.jsonl'),'utf8')).trim().split('\n').map(line=>JSON.parse(line));
assert.deepEqual(rows[0],{kind:'start',format:'orchard-catalog-runtime-profile-v1'});
const phases=[],categories=new Map();let active=null,end=null,previous=0;
for(const row of rows.slice(1)){
 assert.ok(Number.isFinite(row.elapsedMilliseconds)&&row.elapsedMilliseconds>=previous);
 previous=row.elapsedMilliseconds;
 assert.equal(end,null,'Profile has records after its terminal record.');
 if(row.kind==='phase-start'){
  assert.equal(active,null);active={phase:row.phase,theme:row.theme};
 }else if(row.kind==='phase'){
  if(active)assert.deepEqual({phase:row.phase,theme:row.theme},active);
  else assert.equal(row.phase,'startup');
  assert.ok(Number.isFinite(row.milliseconds)&&row.milliseconds>=0);
  for(const sample of row.measurements){
   assert.equal(typeof sample.category,'string');assert.ok(Number.isInteger(sample.count)&&sample.count>0);
   assert.ok(Number.isFinite(sample.milliseconds)&&sample.milliseconds>=0);
   assert.ok(Number.isFinite(sample.maximumMilliseconds)&&sample.maximumMilliseconds>=0);
   const value=categories.get(sample.category)??{category:sample.category,count:0,milliseconds:0,maximumMilliseconds:0};
   value.count+=sample.count;value.milliseconds+=sample.milliseconds;
   value.maximumMilliseconds=Math.max(value.maximumMilliseconds,sample.maximumMilliseconds);
   categories.set(sample.category,value);
  }
  phases.push(row);active=null;
 }else if(row.kind==='end'){
  assert.equal(typeof row.passed,'boolean');assert.equal(active,null);end=row;
 }else assert.fail(`Unknown profile record: ${row.kind}`);
}
const resultExists=(await fs.readdir(native)).includes('result.json');
const runtime=resultExists?JSON.parse((await fs.readFile(path.join(native,'result.json'),'utf8')).replace(/^\uFEFF/,'')):null;
if(end)assert.equal(runtime?.passed,end.passed,'Runtime and terminal profile disagree.');
const totals=[...categories.values()];
const report={
 format:'orchard-catalog-runtime-profile-summary-v1',native,
 diagnosticOnly:true,profileEnded:end!==null,runtimeResultPresent:resultExists,
 runtimePassed:runtime?.passed??null,component:runtime?.component??null,
 fullRuntimePassed:runtime?.passed===true&&runtime.component===undefined&&end?.passed===true,
 elapsedMilliseconds:end?.elapsedMilliseconds??null,
 completedPhases:phases.length,unfinishedPhase:active,
 scopeSemantics:'Wall-clock spans include awaited work. Nested categories overlap and must not be added together. Readiness counters record scheduling choices, not actual elapsed wait time.',
 categories:totals.sort((a,b)=>b.milliseconds-a.milliseconds),
 phases:phases.sort((a,b)=>b.milliseconds-a.milliseconds)
};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({...report,categories:report.categories.slice(0,12),phases:report.phases.slice(0,8).map(({measurements,...phase})=>phase)}));
