import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {expo,revealHeight} from './reference/accordion-motion-frame.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,'')),root=option('--native'),source=await read(new URL('../tests/Reference/AccordionContinuity.json',import.meta.url)),errors=[];
const hash=bytes=>createHash('sha256').update(bytes).digest('hex');for(const[file,field]of [['../tests/Reference/Accordion.json','normalReferenceSha256'],['./reference/accordion-client.tsx','clientSha256'],['./reference/capture-accordion-continuity.cjs','scriptSha256']])assert.equal(hash(await fs.readFile(new URL(file,import.meta.url))),source[field]);
const near=(actual,expected,tolerance,context)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);if(error>=tolerance)errors.push({context,actual,expected,error});return error;};
let cases=0,propertySamples=0,sourceSamples=0,retainedClocks=0,resetClocks=0,maximumHeightError=0,maximumAngleError=0;
assert.equal(source.lifecycle.length,32);const key=row=>[row.scheme,row.kind].join('/');
for(const reference of source.lifecycle){
 assert.equal(typeof reference.sameNode,'boolean');assert.equal(typeof reference.retained,'boolean');assert.equal(reference.samples.length,6);
 const samples=[reference.before,...reference.samples];for(const[index,row]of samples.entries()){
  const fraction=reference.reduced||index>0&&!reference.retained?1:expo((index===0?47.5:row.milliseconds)/200);
  near(row.height,revealHeight(row.naturalHeight,fraction),.001,'source/'+key(reference)+'/'+index+'/height');near(row.angle,-90*(1-fraction),.0001,'source/'+key(reference)+'/'+index+'/angle');sourceSamples++;
 }
}
function frame(row,context){
 assert.equal(row.Key,'first');for(const field of ['PaintedAt','ExpansionStart','ExpansionDuration','From','Target','Fraction','Height','NaturalHeight','ClipHeight','Angle','BackgroundOpacity'])assert(Number.isFinite(row[field]),context+'/'+field);
 for(const field of ['Expanded','Hovered','Animating'])assert.equal(typeof row[field],'boolean',context+'/'+field);assert(row.Expanded&&!row.Hovered&&row.BackgroundOpacity===0,context+'/state');assert(row.From>=0&&row.From<=1&&row.Target===1&&row.ExpansionDuration>=0&&row.NaturalHeight>0);
 const fraction=row.ExpansionDuration<=0?1:row.From+(row.Target-row.From)*expo((row.PaintedAt-row.ExpansionStart)/row.ExpansionDuration);
 near(row.Fraction,fraction,.0000001,context+'/fraction');maximumHeightError=Math.max(maximumHeightError,near(row.Height,revealHeight(row.NaturalHeight,fraction),.001,context+'/height'));near(row.ClipHeight,row.Height,.001,context+'/clip');maximumAngleError=Math.max(maximumAngleError,near(row.Angle,Math.fround(-90*(1-row.Fraction)),Number.EPSILON,context+'/angle'));propertySamples++;
}
for(const enabled of [true,false]){
 const mode=enabled?'enabled':'reduced',folder=path.join(root,mode),native=await read(path.join(folder,'accordion-continuity.json')),runtime=await read(path.join(folder,'result.json')),references=source.lifecycle.filter(row=>row.reduced!==enabled);
 assert(native.passed&&runtime.passed);assert.equal(native.enabled,enabled);assert.equal(native.lifecycle.length,16);assert.deepEqual(native.lifecycle.map(key).sort(),references.map(key).sort());for(const[field,value]of Object.entries({animationsEnabled:enabled,displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
 for(const report of native.lifecycle){
  const context=mode+'/'+key(report),reference=references.find(row=>key(row)===key(report));assert.equal(report.reduced,!enabled);assert.equal(report.sameNode,reference.sameNode,context+'/identity');assert.deepEqual(report.order,reference.order,context+'/order');assert.equal(report.title,reference.after.title);assert.equal(report.subtitle,reference.after.subtitle);assert.equal(report.content,reference.after.content);assert.equal(report.automationName,[reference.after.title,reference.after.subtitle].filter(Boolean).join(' '));assert.equal(report.expanded,reference.after.expanded);assert.equal(report.staleRejected,report.kind==='remove-reinsert');assert(Number.isInteger(report.loads)&&Number.isInteger(report.unloads)&&report.loads>=0&&report.unloads>=0);
  assert(report.frames.length>0,context+'/missing-frames');for(const[index,row]of [report.before,report.immediate,report.after,...report.frames,report.final].entries())frame(row,context+'/'+index);if(enabled&&reference.retained)assert(new Set(report.frames.map(row=>row.PaintedAt)).size>=2,context+'/missing-distinct-paints');
  const before=report.before,after=report.after,retained=report.sameNode&&before.ExpansionStart===after.ExpansionStart&&before.ExpansionDuration===after.ExpansionDuration;assert.equal(report.retained,retained,context+'/claimed-clock');
  if(enabled){assert.equal(before.ExpansionDuration,200);assert.equal(before.From,0);assert(before.Fraction>0&&before.Fraction<1&&before.Animating);assert(before.PaintedAt-before.ExpansionStart>=47.5);assert.equal(retained,reference.retained,context+'/source-clock');if(retained){assert.equal(report.loads,0);assert.equal(report.unloads,0);retainedClocks++;}else{assert.equal(after.Fraction,1);assert.equal(after.Animating,false);assert.equal(after.ExpansionDuration,0);assert.equal(report.unloads,1);assert.equal(report.loads,report.sameNode?1:0);resetClocks++;}}
  else{for(const row of [before,report.immediate,after,...report.frames,report.final]){assert.equal(row.Fraction,1);assert.equal(row.Animating,false);assert.equal(row.ExpansionDuration,0);}}
  near(after.NaturalHeight,reference.after.naturalHeight,.001,context+'/source-natural-height');near(report.final.Height,reference.final.height,.001,context+'/source-final-height');assert.equal(report.final.Animating,false);cases++;
 }
}
const preference=await read(path.join(root,'preference-restored.json'));assert(preference.passed);assert.equal(typeof preference.original,'boolean');assert.equal(preference.original,preference.restored);
const report={passed:errors.length===0,cases,propertySamples,sourceSamples,retainedClocks,resetClocks,maximumHeightError,maximumAngleError,errors,preferencesRestored:true,sourceMotionRasterParityVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,errors:errors.slice(0,12)}));assert(report.passed,'Accordion continuity differs.');
