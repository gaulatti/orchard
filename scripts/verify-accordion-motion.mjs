import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {cssRgba,assertNativePaint} from './reference/css-paint.mjs';
import {colourAt} from './reference/radio-motion-frame.mjs';
import {expo,revealHeight,cssEndpoint} from './reference/accordion-motion-frame.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const root=option('--native'),source=await read(new URL('../tests/Reference/AccordionMotion.json',import.meta.url)),errors=[];
let timelines=0,frames=0,movingRows=0,backgroundRows=0,intermediateBackgroundFrames=0,sourceBackgroundSamples=0,reversalSteps=0,maximumHeightError=0,maximumAngleError=0;
const near=(actual,expected,tolerance,context)=>{const delta=Math.abs(actual-expected);if(!Number.isFinite(actual)||!Number.isFinite(expected)||delta>=tolerance)errors.push({context,actual,expected,delta});return delta;};
const rgba=(actual,expected,context)=>{for(let i=0;i<4;i++)if(i<3?actual[i]!==expected[i]:Math.abs(actual[i]-expected[i])>=.000001)errors.push({context,channel:i,actual:actual[i],expected:expected[i]});};
const paint=frame=>[...frame.Background.slice(3).matchAll(/[0-9a-fA-F]{2}/g)].map(value=>parseInt(value[0],16)).concat(parseInt(frame.Background.slice(1,3),16)/255*frame.BackgroundOpacity);
function frame(row,context){
 for(const field of ['PaintedAt','ExpansionStart','ExpansionDuration','From','Target','Fraction','Height','NaturalHeight','ClipHeight','Angle','BackgroundStart','BackgroundDuration','BackgroundOpacity'])assert(Number.isFinite(row[field]),context+'/'+field);
 assert.match(row.Background,/^#[0-9a-fA-F]{8}$/);assert.equal(typeof row.Expanded,'boolean');assert.equal(typeof row.Animating,'boolean');assert(row.ExpansionDuration>=0&&row.BackgroundDuration>=0&&row.From>=0&&row.From<=1&&row.Target>=0&&row.Target<=1&&row.Fraction>=0&&row.Fraction<=1&&row.NaturalHeight>=0&&row.BackgroundOpacity>=0&&row.BackgroundOpacity<=1);
 for(const endpoint of [row.BackgroundFrom,row.BackgroundTarget]){assert.equal(typeof endpoint.Legacy,'boolean');for(const field of ['First','Second','Third','Alpha'])assert(Number.isFinite(endpoint[field]));assert(endpoint.Alpha>=0&&endpoint.Alpha<=1);}
 const progress=row.ExpansionDuration<=0?1:expo((row.PaintedAt-row.ExpansionStart)/row.ExpansionDuration),fraction=row.From+(row.Target-row.From)*progress;
 near(row.Fraction,fraction,.0000001,context+'/fraction');maximumAngleError=Math.max(maximumAngleError,near(row.Angle,Math.fround(-90*(1-row.Fraction)),Number.EPSILON,context+'/angle'));
 maximumHeightError=Math.max(maximumHeightError,near(row.Height,revealHeight(row.NaturalHeight,fraction),.001,context+'/height'));near(row.ClipHeight,row.Height,.001,context+'/actual-clip');
 rgba(paint(row),colourAt({PaintedAt:row.PaintedAt,StrokeStart:row.BackgroundStart,StrokeDuration:row.BackgroundDuration,StrokeFrom:row.BackgroundFrom,StrokeTarget:row.BackgroundTarget}),context+'/background');frames++;
}
function snapshot(rows,context){assert.equal(rows.length,2,context);for(const[index,row]of rows.entries()){assert.equal(row.Key,index===0?'first':'second',context);frame(row,context+'/'+index);}}
function endpoint(row,wanted,context){assert.equal(row.Expanded,wanted.expanded);near(row.Height,wanted.height,.001,context+'/source-height');near(row.Angle,wanted.angle,.0001,context+'/source-angle');assertNativePaint({color:row.Background,opacity:row.BackgroundOpacity},wanted.background,context+'/source-background');assert.equal(row.Animating,false);}
const backgroundAt=(row,time)=>colourAt({PaintedAt:time,StrokeStart:row.BackgroundStart,StrokeDuration:row.BackgroundDuration,StrokeFrom:row.BackgroundFrom,StrokeTarget:row.BackgroundTarget});
for(const reference of source.transitions)for(const[index,definition]of reference.definition.rows.entries()){
 const animation=definition.animations.find(row=>row.target==='header'&&row.property==='background-color');
 for(const sample of reference.samples){
  const expected=animation?colourAt({PaintedAt:sample.milliseconds,StrokeStart:0,StrokeDuration:animation.timing.duration,StrokeFrom:cssEndpoint(animation.frames[0].backgroundColor),StrokeTarget:cssEndpoint(animation.frames[1].backgroundColor)}):cssRgba(reference.after.rows[index].background);
  rgba(expected,cssRgba(sample.rows[index].background),`source/${reference.scheme}/${reference.kind}/${reference.from}/${reference.to}/${reference.reduced}/${index}/${sample.milliseconds}`);sourceBackgroundSamples++;
 }
}
const key=row=>[row.scheme,row.kind,row.from,row.to].join('/');assert.equal(source.transitions.length,48);
for(const enabled of [true,false]){
 const mode=enabled?'enabled':'reduced',folder=path.join(root,mode),runtime=await read(path.join(folder,'result.json')),native=await read(path.join(folder,'accordion-motion.json'));assert(runtime.passed&&native.passed);assert.equal(native.enabled,enabled);assert.equal(native.reports.length,24);for(const[field,value]of Object.entries({animationsEnabled:enabled,displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
 const references=source.transitions.filter(row=>row.reduced!==enabled);assert.deepEqual(native.reports.map(key).sort(),references.map(key).sort());
 for(const report of native.reports){
  const reference=references.find(row=>key(row)===key(report)),context=mode+'/'+key(report);assert.equal(report.reduced,!enabled);snapshot(report.before,context+'/before');snapshot(report.after,context+'/after');assert(report.frames.length>0);for(const[at,rows]of [report.immediate,...report.frames].entries())snapshot(rows,context+'/'+at);
  for(let index=0;index<2;index++){
   const before=report.before[index],after=report.after[index],wanted=reference.after.rows[index],definition=reference.definition.rows[index];endpoint(before,reference.before.rows[index],context+'/before/'+index);endpoint(after,wanted,context+'/after/'+index);
   const samples=[report.immediate,...report.frames,report.after],moving=new Set(),movingBackground=new Set();const expansion=definition.animations.find(row=>row.target==='grid'&&row.property==='grid-template-rows'),background=definition.animations.find(row=>row.target==='header'&&row.property==='background-color'),hasExpansion=!!expansion;
   const initial=report.immediate[index];
   if(enabled&&hasExpansion){assert.equal(initial.ExpansionDuration,expansion.timing.duration);assert.equal(initial.From,before.Fraction);}
   if(enabled&&background){assert.equal(initial.BackgroundDuration,background.timing.duration);rgba(backgroundAt(initial,initial.BackgroundStart),cssRgba(background.frames[0].backgroundColor),context+'/background-from');rgba(backgroundAt(initial,initial.BackgroundStart+initial.BackgroundDuration),cssRgba(background.frames[1].backgroundColor),context+'/background-target');}
   for(const rows of samples){const row=rows[index];assert.equal(row.Expanded,wanted.expanded);assert.equal(row.Target,wanted.expanded?1:0);for(const field of enabled?['ExpansionStart','ExpansionDuration','From','Target']:['ExpansionDuration','From','Target'])assert.equal(row[field],initial[field],context+'/retained-'+field);if(row.Fraction>0&&row.Fraction<1)moving.add(row.PaintedAt);if(row.PaintedAt>row.BackgroundStart&&row.PaintedAt<row.BackgroundStart+row.BackgroundDuration&&paint(row).some((value,i)=>Math.abs(value-cssRgba(wanted.background)[i])>.000001))movingBackground.add(row.PaintedAt);}
   if(enabled&&background){assert(movingBackground.size>=2,context+'/missing-background/'+index);backgroundRows++;intermediateBackgroundFrames+=movingBackground.size;}
   if(enabled&&hasExpansion){assert(moving.size>=2,context+'/missing-intermediate/'+index);movingRows++;}if(!enabled)for(const rows of samples){assert.equal(rows[index].Animating,false);near(rows[index].Angle,wanted.angle,.0001,context+'/reduced-angle');near(rows[index].Height,wanted.height,.001,context+'/reduced-height');}
  }
  timelines++;
 }
 assert.equal(native.reversals.length,enabled?2:0);assert.deepEqual(native.reversals.map(row=>row.scheme).sort(),enabled?['Dark','Light']:[]);
 for(const sequence of native.reversals){
  assert.equal(sequence.steps.length,3);const reference=source.reversals.find(row=>row.scheme===sequence.scheme);snapshot(sequence.before,'reversal/'+sequence.scheme+'/before');snapshot(sequence.after,'reversal/'+sequence.scheme+'/after');for(let index=0;index<2;index++){endpoint(sequence.before[index],reference.before.rows[index],'reversal/before/'+index);endpoint(sequence.after[index],reference.after.rows[index],'reversal/after/'+index);}let factor=1;
  for(const[index,step]of sequence.steps.entries()){
   assert.equal(step.target,reference.steps[index].target);assert.equal(step.elapsed,reference.steps[index].elapsed);const context=`reversal/${sequence.scheme}/${index}`,old=step.during[0],current=step.immediate[0],elapsed=current.ExpansionStart-old.ExpansionStart;assert(elapsed>=step.elapsed&&elapsed<old.ExpansionDuration);assert(old.Fraction>0&&old.Fraction<1&&old.Animating);
   factor=Math.min(1,Math.abs(expo(elapsed/old.ExpansionDuration)*factor+1-factor));near(current.ExpansionDuration,200*factor,.001,context+'/shortened-duration');near(current.From,old.From+(old.Target-old.From)*expo(elapsed/old.ExpansionDuration),.000001,context+'/continuity');assert.equal(current.Target,step.target==='none'?0:1);assert(step.frames.length>=2,context+'/missing-frames');
   for(const[at,rows]of [step.during,step.immediate,...step.frames,step.after].entries())snapshot(rows,`${context}/${at}`);assert(new Set(step.frames.map(rows=>rows[0].PaintedAt)).size>=2,context+'/duplicate-frames');reversalSteps++;
  }
  assert.equal(sequence.after[0].Fraction,0);assert.equal(sequence.after[0].Height,0);assert.equal(sequence.after[0].Animating,false);
 }
}
const preference=await read(path.join(root,'preference-restored.json'));assert(preference.passed);assert.equal(typeof preference.original,'boolean');assert.equal(preference.original,preference.restored);
const report={passed:errors.length===0,timelines,frames,movingRows,backgroundRows,intermediateBackgroundFrames,sourceBackgroundSamples,reversalSteps,maximumHeightError,maximumAngleError,errors,normalMotionNumericVerified:errors.length===0,reducedMotionStaticVerified:errors.length===0,preferencesRestored:true,sourceMotionRasterParityVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,errors:errors.slice(0,16)}));assert(report.passed,'Accordion native motion differs.');
