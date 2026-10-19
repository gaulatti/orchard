import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(new URL('../tests/Reference/Empty.json',import.meta.url)),native=option('--native'),pngs=option('--source'),runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'empty.json'));
assert(runtime.passed&&report.passed);assert.equal(runtime.component,'empty');for(const[field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
assert.equal(report.frames.length,120);assert.equal(report.lifecycle.length,12);assert(report.lifecycle.every(row=>row.retained));
const key=row=>[row.id,row.scheme,row.motion].join('/');assert.deepEqual(report.frames.map(key).sort(),source.cases.map(key).sort());
const errors=[],raster=[];let maximumGeometryError=0;
function near(actual,expected,context){assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual,expected,error});}
function same(actual,expected,context){if(actual!==expected)errors.push({context,actual,expected});}
function box(actual,expected,context){assert(actual&&expected,context);for(const field of ['x','y','width','height'])near(actual[field],expected[field],context+'/'+field);}
function paint(actual,expected,context){const[r,g,b,a]=cssRgba(expected);for(const[field,wanted]of Object.entries({r,g,b}))same(actual[field],wanted,context+'/'+field);if(Math.abs(actual.a/255*actual.opacity-a)>=.000001)errors.push({context,actual,expected});}
const text=value=>value.replace(/[ \t\r\n\f]+/g,' ').trim();
for(const reference of source.cases){
 const context=key(reference),found=report.frames.find(row=>key(row)===context).measurement,wanted=reference.measurement;
 for(const field of ['root','icon','body','title'])box(found[field].box,wanted[field].box,context+'/'+field);
 for(const field of ['background','border']){paint(found.root[field],wanted.root[field],context+'/root/'+field);paint(found.icon[field],wanted.icon[field],context+'/icon/'+field);}
 paint(found.icon.color,wanted.icon.color,context+'/icon/color');same(found.icon.text,text(wanted.icon.text),context+'/icon/text');
 near(found.root.radius,parseFloat(wanted.root.radius),context+'/radius');near(found.root.borderWidth,parseFloat(wanted.root.borderWidth),context+'/border-width');
 if(wanted.iconChildren.length===1)box(found.icon.child,wanted.iconChildren[0].box,context+'/icon/child');
 same(found.title.text,text(wanted.title.text),context+'/title/text');same(found.title.weight,600,context+'/title/weight');same(found.title.heading,'Level3',context+'/title/heading');near(found.title.fontSize,16,context+'/title/font-size');near(found.title.lineHeight,24,context+'/title/line-height');
 for(const field of ['description','action']){same(found[field]===null,wanted[field]===null,context+'/'+field+'/presence');if(wanted[field]&&found[field]){box(found[field].box,wanted[field].box,context+'/'+field);if(field==='description')same(found[field].text,text(wanted[field].text),context+'/description/text');}}
 same(found.IsTabStop,false,context+'/tab-stop');same(found.name,'',context+'/name');same(found.hasInvoke,false,context+'/invoke');
 const bytes=await fs.readFile(path.join(pngs,reference.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),reference.pngSha256);const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(native,reference.file)));assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);
 let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;for(let at=0;at<actual.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const error=Math.abs(actual.rgba[at+channel]-expected.rgba[at+channel]);different||=error!==0;absoluteRgbaError+=error;maximumChannelDifference=Math.max(maximumChannelDifference,error);}differingPixels+=Number(different);}raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});
}
const audit={passed:errors.length===0,frames:120,lifecyclePhases:12,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,errors:errors.slice(0,16)}));assert(audit.passed,'Empty contract differs.');
