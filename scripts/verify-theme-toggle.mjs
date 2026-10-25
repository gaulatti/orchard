import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const read=async file=>JSON.parse(await fs.readFile(file,'utf8')),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const native=option('--native'),source=option('--source'),reference=await read(new URL('../tests/Reference/ThemeToggle.json',import.meta.url)),result=await read(path.join(native,'result.json')),report=await read(path.join(native,'theme-toggle.json'));
assert(result.passed&&report.passed&&result.component==='theme-toggle');
assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);
const key=r=>r.id+'/'+r.scheme;assert.equal(reference.cases.length,134);assert.deepEqual(report.frames.map(key).sort(),reference.cases.map(key).sort());
let maximumGeometryError=0;const errors=[],raster=[];
function near(a,b,context){assert(Number.isFinite(a)&&Number.isFinite(b),context);const error=Math.abs(a-b);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual:a,expected:b,error});}
for(const row of reference.cases){
 const a=report.frames.find(r=>key(r)===key(row)).measurement,e=row.measurement,context=key(row);
 for(const part of['box','icon'])for(const field of['x','y','width','height'])near(a[part][field],e[part][field],context+'/'+part+'/'+field);
 for(const field of['mode','label','title','disabled'])assert.deepEqual(a[field],e[field],context+'/'+field);
 assert.equal(a.role,'Button');assert.equal(a.tabStop,true);assert.deepEqual(a.strokeRgba,e.strokeRgba);near(a.strokeWidth,1.5,context+'/stroke');near(a.iconOpacity,e.disabled?.45:1,context+'/disabled-icon');if(row.spec.state==='focus')assert.equal(a.focused,true);
 const expectedBytes=await fs.readFile(path.join(source,row.file));assert.equal(hash(expectedBytes),row.pngSha256);
 const actualBytes=await fs.readFile(path.join(native,row.file));assert.equal(hash(actualBytes),report.captures[row.file]);
 const wanted=readRgbaPng(expectedBytes),actual=readRgbaPng(actualBytes);assert.deepEqual([actual.width,actual.height],[wanted.width,wanted.height]);
 let differingPixels=0,absoluteRgbaError=0;for(let i=0;i<wanted.rgba.length;i+=4){let different=false;for(let c=0;c<4;c++){const error=Math.abs(wanted.rgba[i+c]-actual.rgba[i+c]);different||=error>0;absoluteRgbaError+=error;}differingPixels+=Number(different);}
 assert.equal(report.rasterMatches[row.file],differingPixels===0);raster.push({file:row.file,differingPixels,absoluteRgbaError});
}
assert.equal(report.interactions.length,2);for(const scheme of['Light','Dark']){const row=report.interactions.find(r=>r.scheme===scheme);assert(row);for(const field of['passed','bindingRetained','disabledRejected','staleRejected','metadataRetained','styleMetadataRetained','templateReplacement','remount'])assert.equal(row[field],true,scheme+'/'+field);assert.deepEqual(row.requests,['Light','Dark','System','Light']);}
assert.equal(report.sourceRasterParityVerified,raster.every(r=>r.differingPixels===0));
const audit={passed:errors.length===0,frames:report.frames.length,maximumGeometryError,errors,raster,sourceRasterParityVerified:report.sourceRasterParityVerified,masks:0,resampling:false,pixelTolerance:0,nativeNormalMotionVerified:false,actualScalingVerified:false,nativeArm64RuntimeVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined}));assert(audit.passed,'Theme Toggle source geometry differs.');
