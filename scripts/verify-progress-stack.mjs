import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(new URL('../tests/Reference/ProgressStack.json',import.meta.url)),native=option('--native'),pngs=option('--source'),runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'progress-stack.json'));
assert(runtime.passed&&report.passed);assert.equal(runtime.component,'progress-stack');for(const[field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
assert.equal(report.frames.length,140);assert.equal(report.lifecycle.length,12);assert(report.lifecycle.every(row=>row.retained));
const key=row=>[row.id,row.scheme,row.motion].join('/');assert.deepEqual(report.frames.map(key).sort(),source.cases.map(key).sort());
const errors=[],raster=[];let maximumGeometryError=0;
function near(actual,expected,context){assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual,expected,error});}
function same(actual,expected,context){if(actual!==expected)errors.push({context,actual,expected});}
function box(actual,expected,context){assert(actual&&expected,context);for(const field of ['x','y','width','height'])near(actual[field],expected[field],context+'/'+field);}
function paint(actual,expected,context){const[r,g,b,a]=cssRgba(expected);for(const[field,wanted]of Object.entries({r,g,b}))if(a>0)same(actual[field],wanted,context+'/'+field);if(Math.abs(actual.a/255*actual.opacity-a)>=.000001)errors.push({context,actual,expected});}
const text=value=>value.replace(/[ \t\r\n\f]+/g,' ').replace(/^ +| +$/g,'');
for(const reference of source.cases){
 const context=key(reference),found=report.frames.find(row=>key(row)===context).measurement,wanted=reference.measurement;
 box(found.root.box,wanted.root.box,context+'/root');box(found.bar.box,wanted.bar.box,context+'/bar');paint(found.bar.background,wanted.bar.background,context+'/track-paint');
 same(found.segments.length,wanted.segments.length,context+'/segments');for(let i=0;i<wanted.segments.length;i++){box(found.segments[i].box,wanted.segments[i].box,context+'/segment-'+i);paint(found.segments[i].background,wanted.segments[i].background,context+'/segment-'+i+'/paint');same(found.segments[i].title,wanted.segments[i].title,context+'/segment-'+i+'/title');}
 same(found.legend===null,wanted.legend===null,context+'/legend-presence');if(wanted.legend&&found.legend)box(found.legend.box,wanted.legend.box,context+'/legend');same(found.items.length,wanted.items.length,context+'/items');
 for(let i=0;i<wanted.items.length;i++){
  const actual=found.items[i],expected=wanted.items[i];box(actual.box,expected.box,context+'/item-'+i);box(actual.swatch.box,expected.swatch.box,context+'/swatch-'+i);paint(actual.swatch.background,expected.swatch.background,context+'/swatch-'+i+'/paint');
  for(const part of ['label','value']){box(actual[part].box,expected[part].box,context+'/'+i+'/'+part);same(actual[part].text,text(expected[part].text),context+'/'+i+'/'+part+'/text');paint(actual[part].color,expected[part].color,context+'/'+i+'/'+part+'/color');near(actual[part].fontSize,parseFloat(expected[part].fontSize),context+'/'+i+'/'+part+'/font-size');near(actual[part].lineHeight,parseFloat(expected[part].lineHeight),context+'/'+i+'/'+part+'/line-height');same(actual[part].weight,Number(expected[part].weight),context+'/'+i+'/'+part+'/weight');}
 }
 same(found.IsTabStop,false,context+'/tab-stop');same(found.name,'',context+'/name');same(found.hasInvoke,false,context+'/invoke');same(found.hasRange,false,context+'/range');assert.equal(wanted.animations,0);
 const bytes=await fs.readFile(path.join(pngs,reference.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),reference.pngSha256);const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(native,reference.file)));assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);
 let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;for(let at=0;at<actual.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const error=Math.abs(actual.rgba[at+channel]-expected.rgba[at+channel]);different||=error!==0;absoluteRgbaError+=error;maximumChannelDifference=Math.max(maximumChannelDifference,error);}differingPixels+=Number(different);}raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});
}
const audit={passed:errors.length===0,frames:140,lifecyclePhases:12,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,errors:errors.slice(0,16)}));assert(audit.passed,'ProgressStack contract differs.');
