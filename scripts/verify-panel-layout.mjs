import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse(await fs.readFile(file,'utf8')),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const native=option('--native'),source=option('--source'),reference=await read(new URL('../tests/Reference/PanelLayout.json',import.meta.url));
const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'panel-layout.json'));
assert(result.passed&&report.passed&&result.component==='panel-layout');assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);
const key=row=>row.id+'/'+row.scheme;assert.equal(reference.cases.length,86);assert.deepEqual(report.frames.map(key).sort(),reference.cases.map(key).sort());
let maximumGeometryError=0;const errors=[],raster=[];
function near(actual,expected,context){assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual,expected,error});}
for(const row of reference.cases){
 const actual=report.frames.find(value=>key(value)===key(row)).measurement,expected=row.measurement,context=key(row);
 for(const field of ['x','y','width','height'])near(actual.frame[field],expected.frame[field],context+'/frame/'+field);
 assert.deepEqual(actual.items.map(v=>v.id).sort(),expected.items.map(v=>v.id).sort());
 for(const item of expected.items){const rendered=actual.items.find(v=>v.id===item.id);for(const part of ['box','wrapper'])for(const field of ['x','y','width','height'])near(rendered[part][field],item[part][field],context+'/'+item.id+'/'+part+'/'+field);}
 assert.equal(actual.passive,true);assert.equal(actual.group,true);
 if(!row.spec.column){const padding=row.spec.padding?16:0;near(Math.floor(actual.viewportWidth+padding+.5),expected.clientWidth,context+'/viewportWidth');near(Math.floor(actual.extentWidth+padding+.5),expected.scrollWidth,context+'/extentWidth');}
 if(!row.file)continue;
 const wantedBytes=await fs.readFile(path.join(source,row.file));assert.equal(hash(wantedBytes),row.pngSha256);
 const renderedBytes=await fs.readFile(path.join(native,row.file));assert.equal(hash(renderedBytes),report.captures[row.file],context+'/native-capture-hash');
 const wanted=readRgbaPng(wantedBytes),rendered=readRgbaPng(renderedBytes);assert.deepEqual([rendered.width,rendered.height],[wanted.width,wanted.height]);
 let differingPixels=0,absoluteRgbaError=0;for(let i=0;i<wanted.rgba.length;i+=4){let differs=false;for(let c=0;c<4;c++){const d=Math.abs(wanted.rgba[i+c]-rendered.rgba[i+c]);differs||=d>0;absoluteRgbaError+=d;}differingPixels+=Number(differs);}assert.equal(report.rasterMatches[row.file],differingPixels===0,context+'/raw-raster-receipt');raster.push({file:row.file,differingPixels,absoluteRgbaError});
}
assert.equal(report.interactions.length,2);for(const theme of ['Light','Dark']){
 const row=report.interactions.find(value=>value.theme===theme);assert(row);for(const field of ['passed','growthMetadata','liveGrowth','verticalGrowth','growthBinding','moveRetained','focusRetained','templateReplacement','remount','scrollRetained','callerMetadata','collectionRetained','keyboardViewport','intrinsicWidth'])assert.equal(row[field],true,theme+'/'+field);
 assert.equal(row.actions,2);assert.equal(row.moveUnloads,0);assert.notEqual(row.retainedText,'Consumer text');assert(row.horizontalScroll>0);assert(row.keyboardScroll>0);
 const file='panel-layout-demo-'+theme+'.png';assert.equal(hash(await fs.readFile(path.join(native,file))),report.demoCaptures[file]);
}
assert.equal(report.sourceRasterParityVerified,raster.every(row=>row.differingPixels===0));
const audit={passed:errors.length===0,frames:report.frames.length,maximumGeometryError,errors,inputThemes:2,raster,masks:0,resampling:false,pixelTolerance:0,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeArm64RuntimeVerified:false,actualScalingVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined}));assert(audit.passed,'PanelLayout source geometry differs.');
