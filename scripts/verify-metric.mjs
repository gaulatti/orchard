import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const input=option('--native'),output=option('--output');
const reference=JSON.parse(await fs.readFile(new URL('../tests/Reference/Metric.json',import.meta.url),'utf8'));
export function auditMetric(reference,reports){
 assert.equal(reference.format,'orchard-metric-v1');assert.equal(reference.sourceCommit,'a86059013cd59684ce452849278ea81e734f2066');
 assert.equal(reference.sourceSha256,'2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7');
 assert.equal(reference.formatterSha256,'e7c2b863087ac5e0c06973e27abbcab597f745b0540cd8698def589a8fe7129c');
 assert.equal(reference.cases.length,680);assert.equal(new Set(reference.cases.map(c=>`${c.id}/${c.locale}/${c.scheme}`)).size,680);
 let count=0;
 for(const scheme of['Light','Dark']){
  const report=reports[scheme];assert.equal(report.passed,true);assert.equal(report.scheme,scheme);
  for(const flag of['liveUpdates','templateReplacement','retainedRichContent','bindingUpdates','nativeSlotInput','focusedSlotRetained','stableConsumerLifecycle','remount'])assert.equal(report[flag],true);
  const expected=reference.cases.filter(c=>c.scheme===scheme);assert.equal(report.cases.length,expected.length);
  const keys=new Set();for(const receipt of report.cases){const key=`${receipt.id}/${receipt.locale}`;assert(!keys.has(key));keys.add(key);
   const test=expected.find(c=>c.id===receipt.id&&c.locale===receipt.locale);assert(test,key);
   assert.equal(receipt.rendered,test.state.text,key);assert.equal(receipt.expected,test.state.text,key);assert.equal(receipt.automationName,test.state.text,key);assert.equal(receipt.file??undefined,test.file,key);count++;
  }
 }
 return{passed:true,cases:count,locales:[...new Set(reference.cases.map(c=>c.locale))],themes:2,componentAccepted:false};
}
const reports=Object.fromEntries(await Promise.all(['Light','Dark'].map(async scheme=>[scheme,JSON.parse(await fs.readFile(path.join(input,`metric-${scheme}.json`),'utf8'))])));
const audit=auditMetric(reference,reports),negativeCases=[];
for(const[name,mutate]of[
 ['wrong-value',r=>r.Light.cases[0].rendered='invented'],['lost-name',r=>r.Light.cases[0].automationName='invented'],
 ['missing-case',r=>r.Dark.cases.pop()],['duplicate-case',r=>r.Dark.cases[1]=structuredClone(r.Dark.cases[0])],
 ['template-not-verified',r=>r.Dark.templateReplacement=false],['rich-content-not-retained',r=>r.Light.retainedRichContent=false],['focused-slot-lost',r=>r.Light.focusedSlotRetained=false],['lifecycle-unverified',r=>r.Dark.stableConsumerLifecycle=false]
]){const copy=structuredClone(reports);mutate(copy);assert.throws(()=>auditMetric(reference,copy),undefined,name);negativeCases.push(name);}
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const script=await fs.readFile(new URL('./reference/capture-metric.cjs',import.meta.url),'utf8');assert.equal(hash(script.replace(/\r\n/g,'\n')),reference.scriptSha256);
const rich=JSON.parse(await fs.readFile(new URL('../tests/Reference/MetricRich.json',import.meta.url),'utf8'));
assert.equal(rich.cases.length,20);assert.equal(rich.sourceSha256,reference.sourceSha256);
assert.equal(hash((await fs.readFile(new URL('./reference/capture-metric-rich.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n')),rich.scriptSha256);
for(const test of rich.cases){if(test.id==='outside-focus')assert.equal(test.state.outsideFocusRetained,true);else{assert.equal(test.state.focused,true);assert.equal(test.state.sameNode,true);assert.deepEqual(test.state.events,[]);assert.deepEqual(test.state.lifecycle,[]);}}
const lifecycle=[];
for(const scheme of['Light','Dark']){
 const row=JSON.parse(await fs.readFile(path.join(input,`metric-rich-lifecycle-${scheme}.json`),'utf8'));
 assert.equal(row.passed,true);assert.deepEqual(row.updateEvents,[]);assert.deepEqual(row.focusEvents,[]);assert.equal(row.batchedValues,27);assert.equal(row.formattingUpdates,true);assert.equal(row.prefixUpdates,true);assert.equal(row.slotGeometry.length,4);
 assert.equal(new Set(row.slotGeometry.map(g=>`${g.direction}/${g.width}`)).size,4);
 for(const g of row.slotGeometry){assert(['LeftToRight','RightToLeft'].includes(g.direction));assert([140,320].includes(g.width));assert(g.error<=.05);for(const key of['X','Y','Width','Height'])assert(Math.abs(g.expected[key]-g.actual[key])<=.05);}
 lifecycle.push({scheme,consumerEvents:row.updateEvents.length,geometryCases:row.slotGeometry.length});
}
for(const test of reference.cases.filter(c=>c.file)){const bytes=await fs.readFile(path.join(input,test.file));assert.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');}
const report={...audit,negativeCases,richSourceCases:rich.cases.length,lifecycle};await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
