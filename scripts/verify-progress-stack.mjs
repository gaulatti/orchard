import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {compareProgressStackFrame} from './progress-stack-contract.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const unicode=process.argv.includes('--text');
const source=await read(new URL(unicode?'../tests/Reference/ProgressStackText.json':'../tests/Reference/ProgressStack.json',import.meta.url)),native=option('--native'),pngs=option('--source'),runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'progress-stack.json'));
assert(runtime.passed&&report.passed);assert.equal(runtime.component,'progress-stack');for(const[field,value]of Object.entries({displayScale:1,textScale:1,highContrast:false}))assert.equal(runtime.environment[field],value);
assert.equal(report.frames.length,unicode?72:140);assert.equal(report.lifecycle.length,unicode?14:12);assert(report.lifecycle.every(row=>row.retained));
const key=row=>[row.id,row.scheme,row.motion].join('/');assert.deepEqual(report.frames.map(key).sort(),source.cases.map(key).sort());
const errors=[],raster=[];let maximumGeometryError=0;
for(const reference of source.cases){
 const frame=report.frames.find(row=>key(row)===key(reference));const comparison=compareProgressStackFrame(reference,frame,unicode);errors.push(...comparison.errors);maximumGeometryError=Math.max(maximumGeometryError,comparison.maximumGeometryError);
 const bytes=await fs.readFile(path.join(pngs,reference.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),reference.pngSha256);const expected=readRgbaPng(bytes),actual=readRgbaPng(await fs.readFile(path.join(native,reference.file)));assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);
 let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;for(let at=0;at<actual.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const error=Math.abs(actual.rgba[at+channel]-expected.rgba[at+channel]);different||=error!==0;absoluteRgbaError+=error;maximumChannelDifference=Math.max(maximumChannelDifference,error);}differingPixels+=Number(different);}raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});
}
const audit={passed:errors.length===0,frames:unicode?72:140,lifecyclePhases:unicode?14:12,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,errors:errors.slice(0,16)}));assert(audit.passed,'ProgressStack contract differs.');
