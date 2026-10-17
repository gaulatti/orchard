import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTextareaCatalog} from './verify-textarea-catalog.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const native=option('--native'),baseline=option('--baseline'),output=option('--output');
const read=async(folder,file)=>JSON.parse((await fs.readFile(path.join(folder,file),'utf8')).replace(/^\uFEFF/,''));
const priorResult=await read(baseline,'result.json');assert.equal(priorResult.passed,true);assert.equal(priorResult.component,undefined);
const result=await read(native,'result.json');assert.equal(result.passed,true);assert.equal(result.component,undefined);
const files=await fs.readdir(native),prior=(await fs.readdir(baseline)).filter(name=>name.endsWith('.png')),pngs=files.filter(name=>name.endsWith('.png'));
assert(prior.length>=1000);assert.deepEqual(prior.filter(name=>!pngs.includes(name)),[]);
const profile=(await fs.readFile(path.join(native,'catalog-runtime-profile.jsonl'),'utf8')).trim().split('\n').map(line=>JSON.parse(line));
assert.equal(profile[0].kind,'start');assert.equal(profile.at(-1).kind,'end');assert.equal(profile.at(-1).passed,true);assert(profile.at(-1).elapsedMilliseconds<480000);
const starts=profile.filter(row=>row.kind==='phase-start');
for(const theme of['Light','Dark']){
 const phases=starts.filter(row=>row.theme===theme).map(row=>row.phase),index=phases.indexOf('TextareasFixture');assert(index>0);
 assert.equal(phases[index-1],'InputsFixture');assert.equal(phases[index+1],'DateControlsFixture');assert.equal(phases[index+2],'KeyboardFixture');assert.equal(phases.filter(p=>p==='TextareasFixture').length,1);
 assert.equal(phases.filter(p=>p==='DateControlsFixture').length,1);
 const dates=await read(native,`dates-catalog-${theme}.json`);assert.equal(dates.passed,true);assert.equal(dates.consumerUpdates,3);assert.equal(dates.popupClosed,true);assert(pngs.includes(`dates-${theme}.png`));
}
let moves=0,gates=0,keys=0,clicks=0;
for(const file of files.filter(name=>name.endsWith('-desktop.json'))){
 const report=await read(native,file);
 for(const move of report.pointerMoves){assert.equal(move.positioned,true);assert(!move.deliveryFailed);assert(move.stationary||move.delivered);moves++;}
 for(const gate of report.pointerPresentations){assert.equal(gate.passed,true);assert(gate.stable>=2);assert(gate.milliseconds<2000);gates++;}
 for(const key of report.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);keys++;}
 for(const click of report.pointerClicks){assert.equal(click.delivered,true);assert(click.receipts.some(r=>!r.IsGenerated&&r.exact));clicks++;}
}
assert(moves>=2010);assert(gates>=2010);assert(keys>=1762);assert(clicks>=76);
let retainedGlyphs=0;
for(const component of['radial-bar-chart','sunburst-chart'])for(const theme of['Light','Dark']){
 const file=`${component}-${theme}.json`,previous=await read(baseline,file),current=await read(native,file);assert.equal(current.passed,true);assert.equal(current.reports.length,previous.reports.length);
 for(const expected of previous.reports){const found=current.reports.find(row=>row.id===expected.id);assert(found);assert.deepEqual(found.errors,[]);assert.equal(found.glyphSha256,expected.glyphSha256);retainedGlyphs++;}
}
assert.equal(retainedGlyphs,174);const textarea=await auditTextareaCatalog(native);assert.equal(textarea.catalogIntegrationVerified,true);
const report={passed:true,priorPngs:prior.length,pngs:pngs.length,newPngs:pngs.filter(name=>!prior.includes(name)),elapsedMilliseconds:profile.at(-1).elapsedMilliseconds,completedPhases:profile.filter(row=>row.kind==='phase').length,ownedPointerMoves:moves,pointerGates:gates,ownedKeyActions:keys,ownedClicks:clicks,retainedGlyphHashes:retainedGlyphs,textarea,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
