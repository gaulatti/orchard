import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {cssRgba} from './reference/css-paint.mjs';
import {premium,colourAt,nativeColour,validateFrame} from './reference/radio-motion-frame.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const root=option('--native'),source=await read(new URL('../tests/Reference/RadioContinuity.json',import.meta.url));
const key=row=>[row.scheme,row.kind].join('/');
const close=(actual,expected,tolerance,context)=>assert(Number.isFinite(actual)&&Math.abs(actual-expected)<tolerance,`${context}: ${actual} != ${expected}`);
const rgba=(actual,expected,context)=>actual.forEach((value,index)=>index<3?assert.equal(value,expected[index],`${context}/${index}`):close(value,expected[index],.000001,`${context}/alpha`));
const endpoint=colour=>colourAt({StrokeDuration:0,StrokeTarget:colour,StrokeFrom:colour});
let lifecycleCases=0,retainedClocks=0,resetClocks=0,reversalSteps=0,frames=0,intermediateFrames=0,maximumScaleError=0;
function frame(sample,context){
 validateFrame(sample,context);assert.equal(sample.Opacity,1,context+'/opacity');frames++;
 rgba(nativeColour(sample),colourAt(sample),context+'/paint');
 const progress=sample.DotDuration<=0?1:premium((sample.PaintedAt-sample.DotStart)/sample.DotDuration);
 const expected=sample.DotFrom+(sample.DotTarget-sample.DotFrom)*progress,error=Math.abs(sample.Scale-expected);
 maximumScaleError=Math.max(maximumScaleError,error);close(sample.Scale,expected,.000001,context+'/scale');
 if(sample.Scale>0&&sample.Scale<1)intermediateFrames++;
}
assert.equal(source.lifecycle.length,28);assert.equal(source.reversals.length,2);
for(const enabled of [true,false]){
 const mode=enabled?'enabled':'reduced',folder=path.join(root,mode,'radio-continuity');
 const runtime=await read(path.join(folder,'result.json')),native=await read(path.join(folder,'radio-continuity.json'));
 assert(runtime.passed&&native.passed);assert.equal(native.enabled,enabled);assert.equal(native.requests,0);
 for(const [field,value]of Object.entries({animationsEnabled:enabled,highContrast:false,displayScale:1,textScale:1}))assert.equal(runtime.environment[field],value,`${mode}/${field}`);
 const references=source.lifecycle.filter(row=>row.reduced!==enabled);
 assert.equal(native.lifecycle.length,14);assert.deepEqual(native.lifecycle.map(key).sort(),references.map(key).sort());
 for(const row of native.lifecycle){
  const reference=references.find(candidate=>key(candidate)===key(row)),context=`${mode}/${key(row)}`;
  assert.equal(row.reduced,!enabled);assert.equal(row.sameNode,reference.sameNode,context+'/identity');
  assert.deepEqual(row.order,reference.order,context+'/order');assert.equal(row.label,reference.after.label,context+'/visible-label');
  assert.equal(row.automationName,reference.after.label,context+'/automation-name');assert.equal(row.selected,reference.after.checked,context+'/selection');
  const moved=row.kind==='move-first-to-last',fresh=['remove-reinsert','group-remount'].includes(row.kind);
  assert.equal(row.loads,moved?1:0,context+'/loads');assert.equal(row.unloads,moved||fresh?1:0,context+'/unloads');
  frame(row.before,context+'/before');frame(row.after,context+'/after');
  if(enabled){
   assert(row.before.Scale>0&&row.before.Scale<1,context+'/initial-intermediate');
   const sameClock=['StrokeStart','StrokeDuration','DotStart','DotDuration'].every(field=>row.before[field]===row.after[field]);
   assert.equal(row.retained,reference.retained,context+'/retained');assert.equal(row.sameNode&&sameClock,reference.retained,context+'/actual-clocks');
   if(reference.retained)retainedClocks++;else resetClocks++;
  }
  if(!enabled||!reference.retained){
   assert.equal(row.after.Scale,reference.after.scale,context+'/settled-scale');assert.equal(row.after.Animating,false,context+'/settled');
   assert.equal(row.after.StrokeDuration,0,context+'/stroke-reset');assert.equal(row.after.DotDuration,0,context+'/dot-reset');
   rgba(nativeColour(row.after),cssRgba(reference.after.border),context+'/source-paint');
  }
  if(!enabled){assert.equal(row.before.Scale,1,context+'/reduced-before');assert.equal(row.before.Animating,false,context+'/reduced-before-animation');}
  lifecycleCases++;
 }
 assert.equal(native.reversals.length,enabled?2:0);
 if(enabled){
  assert.deepEqual(native.reversals.map(row=>row.scheme).sort(),['Dark','Light']);
  for(const sequence of native.reversals){
   const reference=source.reversals.find(row=>row.scheme===sequence.scheme),context=`reversal/${sequence.scheme}`;
   assert.equal(sequence.steps.length,3);frame(sequence.before,context+'/before');
   rgba(nativeColour(sequence.before),cssRgba(reference.before.border),context+'/source-before');
   let strokeFactor=1,dotFactor=1,previous;
   for(const [index,step]of sequence.steps.entries()){
    const label=`${context}/${index}`,old=step.during,current=step.immediate,sourceStep=reference.steps[index];
    assert.equal(step.value==='on',sourceStep.checked,label+'/controlled-value');assert.equal(step.elapsed,sourceStep.elapsed,label+'/requested-elapsed');
    frame(old,label+'/during');frame(current,label+'/immediate');
    if(previous){for(const field of ['StrokeStart','StrokeDuration','DotStart','DotDuration','DotFrom','DotTarget'])assert.equal(old[field],previous[field],label+'/continued-'+field);}
    else{
     const initial=sourceStep.during.animations.find(row=>row.property==='border-top-color');
     rgba(endpoint(old.StrokeFrom),cssRgba(reference.before.border),label+'/initial-from');
     rgba(endpoint(old.StrokeTarget),cssRgba(initial.frames[1].borderTopColor),label+'/initial-target');
     assert.equal(old.StrokeDuration,190);assert.equal(old.DotDuration,220);assert.equal(old.DotFrom,0);assert.equal(old.DotTarget,1);
    }
    for(const [prefix,full,previousFactor]of [['Stroke',190,strokeFactor],['Dot',220,dotFactor]]){
     const elapsed=current[`${prefix}Start`]-old[`${prefix}Start`],duration=old[`${prefix}Duration`];
     assert(duration>0&&elapsed>=step.elapsed&&elapsed<duration,label+'/'+prefix+'/live-reversal');
     const factor=Math.min(1,Math.abs(premium(elapsed/duration)*previousFactor+1-previousFactor));
     close(current[`${prefix}Duration`],full*factor,.001,label+'/'+prefix+'/shortened-duration');
     if(prefix==='Stroke')strokeFactor=factor;else dotFactor=factor;
    }
    const atEvent={...old,PaintedAt:current.StrokeStart};rgba(endpoint(current.StrokeFrom),colourAt(atEvent),label+'/continuous-border');
    const dotAtEvent=old.DotFrom+(old.DotTarget-old.DotFrom)*premium((current.DotStart-old.DotStart)/old.DotDuration);
    close(current.DotFrom,dotAtEvent,.000001,label+'/continuous-dot');assert.equal(current.DotTarget,sourceStep.checked?1:0,label+'/dot-target');
    const animation=sourceStep.definition.animations.find(row=>row.property==='border-top-color');
    rgba(endpoint(current.StrokeTarget),cssRgba(animation.frames[1].borderTopColor),label+'/source-target');
    assert(step.frames.length>=2,label+'/missing-frames');let last=-Infinity;let moving=0;
    for(const [sampleIndex,sample]of step.frames.entries()){
     frame(sample,`${label}/frame/${sampleIndex}`);assert(sample.PaintedAt>last,label+'/frame-order');last=sample.PaintedAt;
     for(const field of ['StrokeStart','StrokeDuration','DotStart','DotDuration','DotFrom','DotTarget'])assert.equal(sample[field],current[field],label+'/'+field);
     if(sample.Scale>0&&sample.Scale<1)moving++;
    }
    assert(moving>=2,label+'/missing-intermediate-frames');frame(step.after,label+'/after');previous=current;reversalSteps++;
   }
   frame(sequence.after,context+'/after');assert.equal(sequence.after.Scale,0);assert.equal(sequence.after.Animating,false);
   rgba(nativeColour(sequence.after),cssRgba(reference.after.border),context+'/source-after');
  }
 }
}
const preference=await read(path.join(root,'preference-restored.json'));assert(preference.passed);assert.equal(preference.original,preference.restored);
let previousMismatches;
if(process.argv.includes('--before')){
 const previous=await read(path.join(option('--before'),'radio-continuity.json'));
 assert.equal(previous.enabled,true);assert.equal(previous.lifecycle.length,14);
 previousMismatches=previous.lifecycle.filter(row=>{const reference=source.lifecycle.find(candidate=>!candidate.reduced&&key(candidate)===key(row));return row.sameNode!==reference.sameNode||row.retained!==reference.retained;}).map(key).sort();
 assert.deepEqual(previousMismatches,['Dark/move-first-to-last','Dark/move-last-to-first','Light/move-first-to-last','Light/move-last-to-first']);
}
const report={passed:true,lifecycleCases,retainedClocks,resetClocks,reversalSteps,frames,intermediateFrames,maximumScaleError,previousMismatches,normalMotionContinuityVerified:true,reducedMotionStaticVerified:true,visibleLabelsAndSelectionVerified:true,windowsPreferenceRestored:true,motionRasterParityVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
