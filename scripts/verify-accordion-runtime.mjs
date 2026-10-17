import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const root=option('--native'),read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const phases=['initial-and-name','space-switch','independent-action','tab-enter','content-retention-and-focus','live-model-and-default','keyed-remove-reinsert','template-and-remount','disabled-guard','duplicate-id-error','data-templates'];
let keyPairs=0,pointerClicks=0;
for(const [mode,enabled]of [['enabled',true],['reduced',false]]){
 const folder=path.join(root,mode),runtime=await read(path.join(folder,'result.json'));assert(runtime.passed);assert.equal(runtime.component,'accordion');for(const[field,value]of Object.entries({animationsEnabled:enabled,displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
 for(const theme of ['Light','Dark']){
  const result=await read(path.join(folder,`accordion-runtime-${theme}.json`));assert(result.passed);assert.deepEqual(result.phases,phases);assert.equal(result.actions,1);for(const field of ['defaultIsInitialOnly','contentRetained','stalePeerRejected','disabledRejected'])assert.equal(result[field],true);
  const template=result.templateEvidence;assert.equal(template.beforeName,'Template title Template subtitle');assert.equal(template.afterName,'Live template title Template subtitle');assert.equal(typeof template.contentText,'string');assert(template.contentText.length>0&&template.contentText!=='Bound content');assert.equal(template.modelValue,template.contentText);assert.equal(template.commandInvocations,1);assert.equal(template.expandedId,'template');
  const held=await read(path.join(folder,`accordion-${theme}-held-keys.json`));assert.deepEqual(held.map(row=>row.key),[9]);for(const key of held){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.downOwned,true);assert.equal(key.upOwned,true);keyPairs++;}
  const desktop=await read(path.join(folder,`accordion-${theme}-desktop.json`));assert.deepEqual(desktop.keyActions.map(row=>row.key),[32,13,65]);for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);keyPairs++;}assert.equal(desktop.pointerClicks.length,1);for(const click of desktop.pointerClicks){assert(click.delivered&&click.receipts.some(row=>row.exact&&!row.IsGenerated));pointerClicks++;}for(const gate of desktop.pointerPresentations){assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);}
 }
}
const preference=await read(path.join(root,'preference-restored.json'));assert(preference.passed);assert.equal(preference.original,preference.restored);
const report={passed:true,themeMotionCombinations:4,lifecyclePhases:44,keyPairs,pointerClicks,preferencesRestored:true,sourceMotionTrajectoryVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
