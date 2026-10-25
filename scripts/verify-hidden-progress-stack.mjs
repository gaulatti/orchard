import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {compareProgressStackFrame} from './progress-stack-contract.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const input=option('--input'),report=await read(input),references=option('--reference');
assert(report.passed);assert.equal(report.scope,'hidden-progress-stack-layout-and-raster');assert.equal(report.hiddenWindow,true);
for(const field of ['activateCalled','inputSent','visible','nativeVisible','nativeComponentAccepted','visibleRuntimeVerified'])assert.equal(report[field],false,field);
assert.equal(report.activationEvents,0);assert(report.guardChecks>=report.frames.length*3);assert.equal(report.frames.length,212);
assert.equal(report.displayScale,1);assert.equal(report.textScale,1);assert.equal(report.highContrast,false);
assert.equal(report.fontTransitions.length,18);
assert.equal(report.collectionUpdates.length,16);
for(const scheme of ['Light','Dark'])for(const kind of ['progress-stack','empty','icon-badge']){
 const rows=report.fontTransitions.filter(row=>row.scheme===scheme&&row.kind===kind);assert.deepEqual(rows.map(row=>row.phase),['original','custom','restored']);
 for(const row of rows){assert.equal(row.Text,'日本語 العربية');if(row.phase==='custom'){assert.equal(row.ownerFamily,'Arial');assert.equal(row.textFamily,'Arial');assert.deepEqual(row.explicitRunFamilies,[]);}else assert.deepEqual(row.explicitRunFamilies,['Microsoft YaHei','Segoe UI']);}
 assert.equal(rows[0].ownerFamily,rows[2].ownerFamily);assert.equal(rows[0].textFamily,rows[2].textFamily);
}
const key=row=>[row.reference,row.id,row.scheme,row.motion].join('/');assert.equal(new Set(report.frames.map(key)).size,212);
const errors=[],raster=[];let maximumGeometryError=0;const expectedKeys=[];
const paintReference=await read(path.join(references,'ProgressStackPaint.json')),trackPaintRaster=[];
assert.equal(paintReference.scope,'square paint-only diagnostic using unchanged frozen track CSS in the canonical 640x400 context; not component raster acceptance');
assert.equal(paintReference.trackReferenceSha256,crypto.createHash('sha256').update(await fs.readFile(path.join(references,'ProgressStack.json'))).digest('hex'));
assert.equal(paintReference.cases.length,10);assert.equal(report.paintFrames.length,10);
const paintKey=row=>[row.id,row.scheme].join('/');assert.deepEqual(report.paintFrames.map(paintKey).sort(),paintReference.cases.map(paintKey).sort());
for(const expected of paintReference.cases){
 const frame=report.paintFrames.find(row=>paintKey(row)===paintKey(expected));assert.equal(frame.capture.file,expected.file);assert.equal(path.basename(frame.capture.file),frame.capture.file);assert.equal(expected.viewportWidth,640);assert.equal(expected.viewportHeight,400);assert.equal(frame.capture.pixelWidth,640);assert.equal(frame.capture.pixelHeight,400);
 const [r,g,b,a]=cssRgba(expected.paint);assert.deepEqual([frame.brush.r,frame.brush.g,frame.brush.b],[r,g,b]);assert(Math.abs(frame.brush.a/255*frame.brush.opacity-a)<.000001);
 const expectedBytes=await fs.readFile(path.join(references,'ProgressStackPaint',expected.file)),actualBytes=await fs.readFile(path.join(path.dirname(input),expected.file));
 assert.equal(crypto.createHash('sha256').update(expectedBytes).digest('hex'),expected.pngSha256);assert.equal(crypto.createHash('sha256').update(actualBytes).digest('hex'),frame.capture.pngSha256);
 const wanted=readRgbaPng(expectedBytes),actual=readRgbaPng(actualBytes);assert.deepEqual([wanted.width,wanted.height,actual.width,actual.height],[640,400,640,400]);
 let differingPixels=0;for(let at=0;at<actual.rgba.length;at+=4)if(!actual.rgba.subarray(at,at+4).equals(wanted.rgba.subarray(at,at+4)))differingPixels++;
 trackPaintRaster.push({file:expected.file,differingPixels});if(differingPixels)errors.push({context:'track-paint/'+paintKey(expected),differingPixels});
}
const updates=JSON.parse(await fs.readFile(path.join(references,'ProgressStackText.json'),'utf8')).updates;
const updateKey=row=>[row.id,row.scheme,row.motion].join('/');
assert.deepEqual(report.collectionUpdates.map(updateKey).sort(),updates.map(updateKey).sort());
for(const expected of updates){const actual=report.collectionUpdates.find(row=>updateKey(row)===updateKey(expected));assert.equal(actual.retainedOriginalCollection,true);assert.equal(actual.retainedOriginalItems,true);const result=compareProgressStackFrame(expected,actual);errors.push(...result.errors.map(error=>({reference:'collection-updates',...error})));maximumGeometryError=Math.max(maximumGeometryError,result.maximumGeometryError);}
for(const name of ['ProgressStack.json','ProgressStackText.json']){
 const bytes=await fs.readFile(path.join(references,name));assert.equal(report.references.find(row=>row.name===name).sha256,crypto.createHash('sha256').update(bytes).digest('hex'));
 const source=JSON.parse(bytes);assert.equal(source.sourceSha256,'101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254');
 for(const reference of source.cases){const expected={reference:name,...reference},id=key(expected);expectedKeys.push(id);const frame=report.frames.find(row=>key(row)===id);assert(frame,id);const result=compareProgressStackFrame(reference,frame,name==='ProgressStackText.json');errors.push(...result.errors.map(error=>({reference:name,...error})));maximumGeometryError=Math.max(maximumGeometryError,result.maximumGeometryError);
  assert.equal(frame.capture.file,reference.file);assert.equal(path.basename(frame.capture.file),frame.capture.file);assert.equal(frame.capture.pixelWidth,640);assert.equal(frame.capture.pixelHeight,400);
  const expectedBytes=await fs.readFile(path.join(references,name.replace(/\.json$/,''),reference.file)),actualBytes=await fs.readFile(path.join(path.dirname(input),reference.file));
  assert.equal(crypto.createHash('sha256').update(expectedBytes).digest('hex'),reference.pngSha256);assert.equal(crypto.createHash('sha256').update(actualBytes).digest('hex'),frame.capture.pngSha256);
  const wanted=readRgbaPng(expectedBytes),actual=readRgbaPng(actualBytes);assert.deepEqual([wanted.width,wanted.height],[640,400]);assert.deepEqual([actual.width,actual.height],[640,400]);
  let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;for(let at=0;at<actual.rgba.length;at+=4){let different=false;for(let channel=0;channel<4;channel++){const error=Math.abs(actual.rgba[at+channel]-wanted.rgba[at+channel]);different||=error!==0;absoluteRgbaError+=error;maximumChannelDifference=Math.max(maximumChannelDifference,error);}differingPixels+=Number(different);}
  raster.push({file:reference.file,differingPixels,absoluteRgbaError,maximumChannelDifference});
 }
}
assert.deepEqual(report.frames.map(key).sort(),expectedKeys.sort());
const audit={passed:errors.length===0,scope:'hidden-progress-stack-layout-and-raster',frames:212,collectionUpdates:16,trackPaintFrames:10,trackPaintParityVerified:trackPaintRaster.every(row=>row.differingPixels===0),trackPaintRaster,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,hiddenXamlLayoutVerified:errors.filter(row=>!row.context.startsWith('track-paint/')).length===0,hiddenRasterCaptureVerified:true,visibleRuntimeVerified:false,inputAcceptanceVerified:false,sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,trackPaintRaster:undefined,errors:errors.slice(0,12)}));assert(audit.passed,'Hidden XAML layout or track paint differs from source.');
