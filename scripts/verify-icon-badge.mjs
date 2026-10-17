import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(new URL('../tests/Reference/IconBadge.json',import.meta.url)),native=option('--native'),pngs=option('--source');
const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'icon-badge.json')),errors=[],raster=[];
assert(runtime.passed&&report.passed);assert.equal(runtime.component,'icon-badge');
for(const[field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
assert.equal(report.frames.length,136);assert.equal(report.lifecycle.length,8);assert(report.lifecycle.every(row=>row.retained));
const key=row=>[row.id,row.scheme,row.motion].join('/');
assert.deepEqual(report.frames.map(key).sort(),source.cases.map(key).sort());
let maximumGeometryError=0,childGeometries=0,scalarCases=0;
function same(actual,expected,context){if(actual!==expected)errors.push({context,actual,expected});}
function near(actual,expected,context){assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual,expected,error});}
function box(actual,expected,context){assert(actual&&expected,context);for(const field of ['x','y','width','height'])near(actual[field],expected[field],context+'/'+field);}
function paint(actual,expected,context){const [r,g,b,a]=cssRgba(expected);for(const [field,wanted]of Object.entries({r,g,b}))same(actual[field],wanted,context+'/'+field);if(Math.abs(actual.a/255*actual.opacity-a)>=.000001)errors.push({context,actual,expected});}
for(const reference of source.cases){
 const found=report.frames.find(row=>key(row)===key(reference)).measurement,wanted=reference.measurement,context=key(reference);
 box(found.box,wanted.box,context+'/box');near(found.borderWidth,parseFloat(wanted.borderWidth),context+'/border-width');near(found.radius,parseFloat(wanted.radius),context+'/corner-radius');
 for(const field of ['background','border','foreground'])paint(found[field],wanted[field],context+'/'+field);
 same(found.IsTabStop,false,context+'/tab-stop');same(found.hasInvoke,false,context+'/invoke');same(found.role,'Group',context+'/native-role');same(found.name,reference.spec.named?'Consumer icon marker':'',context+'/name');
 if(wanted.children.length===1){box(found.child,wanted.children[0].box,context+'/child');paint(found.childForeground,wanted.children[0].color,context+'/child-color');childGeometries++;}
 if(reference.spec.child!=='rich'&&wanted.children.length===0){same(found.text,wanted.text.replace(/[ \t\r\n\f]+/g,' ').trim(),context+'/scalar');scalarCases++;}
 const bytes=await fs.readFile(path.join(pngs,reference.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),reference.pngSha256);
 const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(native,reference.file)));
 assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;
 for(let at=0;at<actual.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const error=Math.abs(actual.rgba[at+channel]-expected.rgba[at+channel]);different||=error!==0;absoluteRgbaError+=error;maximumChannelDifference=Math.max(maximumChannelDifference,error);}differingPixels+=Number(different);}
 raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});
}
assert.equal(childGeometries,52);assert.equal(scalarCases,80);
const audit={passed:errors.length===0,frames:report.frames.length,childGeometries,scalarCases,lifecyclePhases:report.lifecycle.length,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,errors:errors.slice(0,12)}));assert(audit.passed,'IconBadge contract differs.');
