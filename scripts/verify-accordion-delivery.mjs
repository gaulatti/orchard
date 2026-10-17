import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),consumer=option('--consumer'),read=async(root,file)=>JSON.parse((await fs.readFile(path.join(root,file),'utf8')).replace(/^\uFEFF/,''));
const runtime=await read(native,'result.json'),external=await read(consumer,'result.json');assert(runtime.passed&&external.passed);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(external.platform,'x64');
for(const[field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
const cleanup=await read(consumer,'cleanup.json');assert(cleanup.removed);await assert.rejects(fs.stat(cleanup.isolatedConsumer),{code:'ENOENT'});
let nativeKeyPairs=0,consumerKeyPairs=0;
for(const theme of ['Light','Dark']){
 const report=await read(native,`accordion-runtime-${theme}.json`);assert(report.passed);assert.equal(report.phases.length,11);assert.equal(report.actions,1);assert.equal(report.templateEvidence.commandInvocations,1);assert.equal(report.templateEvidence.modelValue,report.templateEvidence.contentText);
 const desktop=await read(native,`accordion-${theme}-desktop.json`);assert.deepEqual(desktop.keyActions.map(row=>row.key),[32,13,65]);for(const row of desktop.keyActions){assert.equal(row.downs,1);assert.equal(row.ups,1);assert.equal(row.targetRetained,true);nativeKeyPairs++;}
 const held=await read(native,`accordion-${theme}-held-keys.json`);assert.deepEqual(held.map(row=>row.key),[9]);for(const row of held){assert.equal(row.downs,1);assert.equal(row.ups,1);assert(row.downOwned&&row.upOwned);nativeKeyPairs++;}
 const packaged=await read(consumer,`consumer-accordion-${theme}.json`);assert(packaged.passed);assert.equal(packaged.actions,1);for(const field of ['singleExpansion','contentRetained','defaultIsInitialOnly','keyedIdentity','stalePeerRejected','disabledRejected'])assert.equal(packaged[field],true);assert.deepEqual(packaged.keys.map(row=>row.key),[32,9,13]);for(const row of packaged.keys){assert.equal(row.downs,1);assert.equal(row.ups,1);assert(row.downOwned&&row.upOwned);consumerKeyPairs++;}
 await fs.access(path.join(native,`accordion-runtime-${theme}.png`));
}
const full=runtime.component===undefined;if(full){const profile=(await fs.readFile(path.join(native,'catalog-runtime-profile.jsonl'),'utf8')).trim().split('\n').map(line=>JSON.parse(line));assert(profile.at(-1).passed&&profile.at(-1).elapsedMilliseconds<480000);for(const theme of ['Light','Dark']){const phases=profile.filter(row=>row.kind==='phase-start'&&row.theme===theme).map(row=>row.phase),at=phases.indexOf('AccordionsFixture');assert.equal(phases.filter(phase=>phase==='AccordionsFixture').length,1);assert.equal(phases[at-1],'RadioGroupsFixture');assert.equal(phases[at+1],'SwitchesFixture');}}else assert.equal(runtime.component,'accordion');
const report={passed:true,fullCatalogIntegration:full,themes:2,nativeKeyPairs,consumerKeyPairs,independentActions:2,temporaryConsumerRemoved:true,sourceRasterParityVerified:false,normalMotionTrajectoryVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
