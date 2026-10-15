import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const index=process.argv.indexOf(name);if(index<0||!process.argv[index+1])throw Error('Pass '+name+' <path>.');return path.resolve(process.argv[index+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex'),read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const referenceBytes=await fs.readFile(new URL('../tests/Reference/DataTableToolbarLayout.json',import.meta.url));
const reference=JSON.parse(referenceBytes),sourceBytes=await fs.readFile(new URL('../tests/Reference/DataTableToolbarStatePixels.json',import.meta.url)),source=JSON.parse(sourceBytes);
assert.equal(source.format,'orchard-data-table-toolbar-state-pixels-v1');assert.equal(source.captureComplete,true);assert.equal(source.referenceSha256,hash(referenceBytes));
for(const field of ['sourceCommit','sourceSha256','compiledCssSha256','clientSha256','browser'])assert.equal(source[field],reference[field]);
assert.equal(source.captures.length,390);assert.equal(new Set(source.captures.map(item=>`${item.id}/${item.scheme}/${item.step}`)).size,390);
assert.equal(source.caret,'initial');assert.equal(source.animations,'allow');assert.equal(source.masks,0);assert.equal(source.tolerance,0);
const nativeRuntime=await read(path.join(nativeDirectory,'result.json'));assert.equal(nativeRuntime.passed,true);assert.equal(nativeRuntime.component,'data-table-toolbar-interactions');
assert.equal(nativeRuntime.environment.displayScale,1);assert.equal(nativeRuntime.environment.textScale,1);
assert.equal(nativeRuntime.environment.animationsEnabled,false);assert.equal(nativeRuntime.environment.highContrast,false);
const nativeAudit=await read(path.join(nativeDirectory,'interaction-audit.json'));assert.equal(nativeAudit.checkedGatesPassed,true);assert.equal(nativeAudit.referenceSha256,source.referenceSha256);
const difference=(first,second,width,height,origin={x:0,y:0},secondWidth=width)=>{
 let pixels=0,maxChannel=0,minX=width,minY=height,maxX=-1,maxY=-1;
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){
  const a=(y*width+x)*4,b=((y+origin.y)*secondWidth+x+origin.x)*4;let changed=false;
  for(let channel=0;channel<4;channel++){const delta=Math.abs(first[a+channel]-second[b+channel]);if(delta){changed=true;maxChannel=Math.max(maxChannel,delta);}}
  if(changed){pixels++;minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);}
 }
 return{differentPixels:pixels,maxChannel,bounds:pixels?[minX,minY,maxX+1,maxY+1]:null,masks:0,tolerance:0};
};
const pairs=[],initials=[];let sourceChangedRepeats=0;
for(const scheme of ['Light','Dark']){
 const matrix=await read(path.join(nativeDirectory,`data-table-toolbar-matrix-${scheme}.json`));assert.equal(matrix.referenceSha256,source.referenceSha256);
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(sample=>sample.states.map((step,index)=>({id:sample.id,step:index+1,action:step.action,expected:step.state})));
 assert.equal(matrix.reports.length,expected.length);
 for(const [index,point]of expected.entries()){
  const native=matrix.reports[index];assert.equal(native.id,point.id);assert.equal(native.step,point.step);assert.equal(native.action,point.action);assert.deepEqual(native.errors,[]);
  const capture=source.captures.find(item=>item.id===point.id&&item.scheme===scheme&&item.step===point.step);assert.ok(capture);assert.equal(capture.action,point.action);
  assert.equal(capture.stateSha256,capture.expectedStateSha256);assert.equal(capture.stateSha256,capture.postCaptureStateSha256);assert.equal(capture.inputReceipts,capture.trustedInputReceipts);assert.ok(capture.presentation.stable>=3);
  const firstBytes=await fs.readFile(path.join(sourceDirectory,capture.file)),repeatBytes=await fs.readFile(path.join(sourceDirectory,capture.repeatFile));assert.equal(hash(firstBytes),capture.sha256);assert.equal(hash(repeatBytes),capture.repeatSha256);
  const first=readRgbaPng(firstBytes),repeat=readRgbaPng(repeatBytes),nativeBytes=await fs.readFile(path.join(nativeDirectory,native.capture)),window=readRgbaPng(nativeBytes);
  assert.equal(first.width,capture.width);assert.equal(first.height,capture.height);assert.equal(repeat.width,first.width);assert.equal(repeat.height,first.height);
  const sourceRepeat=difference(first.rgba,repeat.rgba,first.width,first.height);if(sourceRepeat.differentPixels)sourceChangedRepeats++;
  assert.equal(capture.repeatPixelStable,firstBytes.equals(repeatBytes));
  const rect=native.surface;assert.equal(rect.Width,first.width);assert.equal(rect.Height,first.height);assert.equal(rect.X,Math.round(rect.X));assert.equal(rect.Y,Math.round(rect.Y));
  assert.ok(rect.X>=0&&rect.Y>=0&&rect.Right<=window.width&&rect.Bottom<=window.height);
  const crop={x:rect.X,y:rect.Y,width:rect.Width,height:rect.Height};
  pairs.push({id:point.id,scheme,step:point.step,action:point.action,sourceFile:capture.file,sourceRepeatFile:capture.repeatFile,nativeFile:native.capture,nativeSha256:hash(nativeBytes),crop,
   first:difference(first.rgba,window.rgba,first.width,first.height,crop,window.width),repeat:difference(repeat.rgba,window.rgba,repeat.width,repeat.height,crop,window.width),sourceRepeat,
   sourceFocus:native.sourceFocus,nativeFocus:native.nativeFocus,sourceFocusMatched:native.sourceFocusMatched});
 }
 for(const capture of source.captures.filter(item=>item.scheme===scheme&&item.step===0)){
  const sample=reference.cases.find(item=>item.id===capture.id&&item.scheme===scheme),bytes=await fs.readFile(path.join(sourceDirectory,capture.file)),repeat=await fs.readFile(path.join(sourceDirectory,capture.repeatFile));
  assert.equal(hash(bytes),sample.screenshots[0].sha256);assert.equal(hash(bytes),capture.sha256);assert.equal(hash(repeat),capture.repeatSha256);assert.ok(bytes.equals(repeat));initials.push({id:capture.id,scheme,sha256:hash(bytes)});
 }
}
assert.equal(pairs.length,376);assert.equal(initials.length,14);
const report={format:'orchard-data-table-toolbar-state-raster-comparison-v1',referenceSha256:source.referenceSha256,sourceManifestSha256:hash(sourceBytes),sourceDirectory,nativeDirectory,
 nativeEnvironment:nativeRuntime.environment,nativeComponentAccepted:false,sourceCapturesVerified:true,initials,pairs,sourceChangedRepeats,firstExactPairs:pairs.filter(pair=>pair.first.differentPixels===0).length,repeatExactPairs:pairs.filter(pair=>pair.repeat.differentPixels===0).length,
 passed:pairs.every(pair=>pair.first.differentPixels===0&&pair.repeat.differentPixels===0)&&sourceChangedRepeats===0,masks:0,tolerance:0};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({pairs:pairs.length,firstExactPairs:report.firstExactPairs,repeatExactPairs:report.repeatExactPairs,sourceChangedRepeats,passed:report.passed,
 differenceRange:{minimum:Math.min(...pairs.map(pair=>pair.first.differentPixels)),maximum:Math.max(...pairs.map(pair=>pair.first.differentPixels))},maxChannel:Math.max(...pairs.map(pair=>pair.first.maxChannel)),masks:0,tolerance:0}));
if(!report.passed)process.exitCode=1;
