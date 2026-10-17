import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(new URL('../tests/Reference/Accordion.json',import.meta.url)),native=option('--native'),sourcePngs=option('--source'),errors=[],raster=[];
let frames=0,rows=0,maximumGeometryError=0;
const near=(actual,expected,context)=>{const delta=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,delta);if(!Number.isFinite(actual)||!Number.isFinite(expected)||delta>=.001)errors.push({context,actual,expected,delta});};
const box=(actual,expected,context)=>{for(const field of ['x','y','width','height'])near(actual[field],expected[field],context+'/'+field);};
for(const width of [320,639,640]){
 const folder=path.join(native,String(width)),runtime=await read(path.join(folder,'result.json')),report=await read(path.join(folder,'accordion-reference.json'));assert(runtime.passed&&report.passed);assert.equal(report.frames.length,20);
 for(const [field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false,animationsEnabled:false}))assert.equal(runtime.environment[field],value);
 const references=source.cases.filter(row=>row.width===width);assert.deepEqual(report.frames.map(row=>row.id+'/'+row.scheme).sort(),references.map(row=>row.id+'/'+row.scheme).sort());
 for(const reference of references){
  const actual=report.frames.find(row=>row.id===reference.id&&row.scheme===reference.scheme),context=reference.file,expected=reference.measurement;box(actual.box,expected.box,context+'/group');assertNativePaint(actual.background,expected.paint.background,context+'/background');assertNativePaint(actual.border,expected.paint.border,context+'/border');assert.equal(actual.rows.length,expected.rows.length);
  for(const [index,wanted]of expected.rows.entries()){
   const found=actual.rows[index],label=context+'/'+index;assert.equal(found.expanded,wanted.expanded);for(const field of ['box','header','button','body'])box(found[field],wanted[field],label+'/'+field);
   box(found.title.box,wanted.title.box,label+'/title');assert.equal(found.title.text,wanted.title.text,label+'/title-text');near(found.title.fontSize,parseFloat(wanted.title.paint.fontSize),label+'/title-font');near(found.title.lineHeight,parseFloat(wanted.title.paint.lineHeight),label+'/title-line-height');assertNativePaint(found.title.foreground,wanted.title.paint.color,label+'/title-color');
   assert.equal(found.subtitle===null,wanted.subtitle===null,label+'/subtitle-presence');if(wanted.subtitle){box(found.subtitle.box,wanted.subtitle.box,label+'/subtitle');assert.equal(found.subtitle.text,wanted.subtitle.text,label+'/subtitle-text');near(found.subtitle.fontSize,parseFloat(wanted.subtitle.paint.fontSize),label+'/subtitle-font');near(found.subtitle.lineHeight,parseFloat(wanted.subtitle.paint.lineHeight),label+'/subtitle-line-height');assertNativePaint(found.subtitle.foreground,wanted.subtitle.paint.color,label+'/subtitle-color');}
   assert.equal(found.actions===null,wanted.actions===null,label+'/actions-presence');if(wanted.actions)box(found.actions,wanted.actions,label+'/actions');
   near(found.angle,wanted.expanded?0:-90,label+'/chevron');assert.equal(found.name,[wanted.title.text,wanted.subtitle?.text].filter(Boolean).join(' '),label+'/accessible-name');rows++;
  }
  const bytes=await fs.readFile(path.join(sourcePngs,reference.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),reference.pngSha256);const wanted=readRgbaPng(bytes),found=readRgbaPng(await fs.readFile(path.join(folder,reference.file)));assert.deepEqual([found.width,found.height],[wanted.width,wanted.height]);let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;
  for(let at=0;at<found.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(found.rgba[at+channel]-wanted.rgba[at+channel]);absoluteRgbaError+=delta;maximumChannelDifference=Math.max(maximumChannelDifference,delta);different||=delta!==0;}differingPixels+=Number(different);}raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});frames++;
 }
}
const preference=await read(path.join(native,'preference-restored.json'));assert(preference.passed);assert.equal(preference.original,preference.restored);
const report={passed:errors.length===0,frames,rows,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(frame=>frame.differingPixels===0),normalMotionVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,raster:undefined,errors:errors.slice(0,16)}));assert(report.passed,'Accordion source/native contract differs.');
