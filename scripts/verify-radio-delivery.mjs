import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),consumer=option('--consumer'),read=async(folder,file)=>JSON.parse((await fs.readFile(path.join(folder,file),'utf8')).replace(/^\uFEFF/,''));
const runtime=await read(native,'result.json'),packageResult=await read(consumer,'result.json');assert.equal(runtime.passed,true);assert.equal(packageResult.passed,true);assert.equal(packageResult.platform,'x64');
assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.highContrast,false);assert.equal(runtime.environment.animationsEnabled,false);
const cleanup=await read(consumer,'cleanup.json');assert.equal(cleanup.removed,true);await assert.rejects(fs.stat(cleanup.isolatedConsumer),{code:'ENOENT'});
let nativeKeyPairs=0,modifierPairs=0,packagedKeyPairs=0;
for(const theme of ['Light','Dark']){
 const radio=await read(native,`radio-runtime-${theme}.json`);assert.equal(radio.passed,true);assert.deepEqual(radio.requests,['weekly','daily','daily','daily','weekly','new','weekly','new']);
 for(const field of ['consumerControlled','disabledRejected','stalePeerRejected','keyedReorder','remount','templateReplacement','homeEnd','controlArrow','space','reverseTab','nonFirstSelectionEntry','explicitKeyboardFocus','livePalette'])assert.equal(radio[field],true,theme+'/'+field);
 const held=await read(native,`radio-${theme}-held-keys.json`);assert.deepEqual(held.map(row=>row.key),[9,40,9,9,36,35,37,9,9]);
 for(const key of held){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);nativeKeyPairs++;if(key.modifier!==null){assert([16,17].includes(key.modifier));assert.equal(key.modifierDowns,1);assert.equal(key.modifierUps,1);modifierPairs++;}}
 assert.deepEqual(held.filter(row=>row.modifier!==null).map(row=>row.modifier),[17,16]);
 const desktop=await read(native,`radio-${theme}-desktop.json`);assert.deepEqual(desktop.keyActions.map(row=>row.key),[40,32,32,32]);
 for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);nativeKeyPairs++;}
 assert.equal(desktop.pointerClicks.length,1);for(const click of desktop.pointerClicks){assert.equal(click.delivered,true);assert(click.receipts.some(row=>row.exact&&!row.IsGenerated));}
 for(const gate of desktop.pointerPresentations){assert.equal(gate.passed,true);assert(gate.stable>=2&&gate.milliseconds<2000);}
 const external=await read(consumer,`consumer-radio-${theme}.json`);assert.equal(external.passed,true);assert.deepEqual(external.requests,['weekly','daily','daily','daily']);
 for(const field of ['binding','keyedIdentity','stalePeerRejected','disabledRejected'])assert.equal(external[field],true);
 assert.deepEqual(external.keyedLifecycle.map(row=>row.kind),['move-last-to-first','move-first-to-last','replace-collection','orientation']);
 for(const row of external.keyedLifecycle){assert.equal(row.sameNode,true);assert.equal(row.selected,true);assert.equal(row.loads,row.kind==='move-first-to-last'?1:0);assert.equal(row.unloads,row.loads);}
 assert.deepEqual(external.keys.map(row=>row.key),[9,40,9,32]);for(const key of external.keys){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);packagedKeyPairs++;}
}
const full=runtime.component===undefined;
if(full){
 const profile=(await fs.readFile(path.join(native,'catalog-runtime-profile.jsonl'),'utf8')).trim().split('\n').map(line=>JSON.parse(line));assert.equal(profile.at(-1).passed,true);assert(profile.at(-1).elapsedMilliseconds<480000);
 for(const theme of ['Light','Dark']){const phases=profile.filter(row=>row.kind==='phase-start'&&row.theme===theme).map(row=>row.phase);assert.equal(phases.filter(phase=>phase==='RadioGroupsFixture').length,1);const index=phases.indexOf('RadioGroupsFixture');assert.equal(phases[index-1],'CheckboxesFixture');assert.equal(phases[index+1],'SwitchesFixture');}
}else assert.equal(runtime.component,'radio-group');
const result={passed:true,themes:2,fullCatalogIntegration:full,nativeRequests:16,consumerRequests:8,nativeKeyPairs,modifierPairs,packagedKeyPairs,temporaryConsumerRemoved:true,sourceRasterParityVerified:false,normalMotionVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
