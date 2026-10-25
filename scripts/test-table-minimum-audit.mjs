import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {verifyTableMinimumReport} from './verify-table-minimum.mjs';
const[folder,output]=process.argv.slice(2);assert(folder&&output);const original=JSON.parse(await fs.readFile(folder+'/table-minimum.json'));
verifyTableMinimumReport(original);
const cases=[
 ['failure',r=>r.passed=false],['wrong-format',r=>r.format='other'],['foreground',r=>r.showWindowCalled=true],['focus',r=>r.focus=1],['input',r=>r.hardwareInputSent=true],['missing-guards',r=>r.guards=0],
 ['missing-stage',r=>r.states.pop()],['duplicate-stage',r=>r.states[1]=r.states[0]],['wrong-theme',r=>r.states[0].scheme='Other'],['wrong-minimum',r=>r.states[0].cellWidth=20],['forged-expected-minimum',r=>{r.states[0].cellWidth=20;r.states[0].expectedWidth=20;}],
 ['lost-viewport',r=>r.states.find(s=>s.name==='scroll-auto').viewportScrollableWidth=0],['lost-overflow',r=>r.states.find(s=>s.name==='scroll-minimum').scrollableWidth=0],['lost-caller-probe',r=>r.states.find(s=>s.name==='caller-override-initial').callerZeroMeasures=0],
 ['lost-live-growth',r=>r.states.find(s=>s.name==='caller-override-grown').callerMinimum=340],['lost-template',r=>r.states.find(s=>s.name==='caller-template').callerTemplateRetained=false],['replaced-content',r=>r.states[0].originalContentRetained=false],['unloaded-content',r=>r.states[0].originalContentLoaded=false],['missing-frame',r=>r.pngs.pop()],['wrong-frame',r=>r.states[0].file='../outside.png']
];
for(const[name,change]of cases){const r=structuredClone(original);change(r);assert.throws(()=>verifyTableMinimumReport(r),undefined,name);}
const result={passed:true,corruptReportsRejected:cases.length,cases:cases.map(([name])=>name),nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
