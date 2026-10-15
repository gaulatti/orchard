import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const i=process.argv.indexOf(name);if(i<0||!process.argv[i+1])throw Error('Pass '+name+' <path>.');return path.resolve(process.argv[i+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,'')),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const source=await read(path.join(sourceDirectory,'result.json')),native=await read(path.join(nativeDirectory,'result.json'));
assert.equal(source.format,'orchard-brush-alpha-source-v1');assert.equal(source.masks,0);assert.equal(source.tolerance,0);
assert.equal(source.referenceSha256,hash(await fs.readFile('tests/Reference/DataTableToolbarLayout.json')));
assert.equal(source.scriptSha256,hash(await fs.readFile('scripts/reference/capture-brush-alpha.mjs')));
assert.equal(native.passed,true);assert.equal(native.component,'brush-alpha');
for(const field of ['displayScale','textScale'])assert.equal(native.environment[field],1);
assert.equal(native.environment.highContrast,false);
const reports=[],buttons=[];
for(const point of source.reports){
 for(const presentation of [point.originalPresentation,point.swatchPresentation]){assert.ok(presentation.stable>=3);assert.ok(presentation.milliseconds<2000);}
 const observed=native.reports.find(item=>item.scheme===point.scheme);assert.ok(observed);assert.equal(observed.rounded,point.rounded);assert.equal(observed.floor,point.floor);
 const sourceBytes=await fs.readFile(path.join(sourceDirectory,point.file));assert.equal(hash(sourceBytes),point.sha256);assert.equal(point.sha256,point.repeatSha256);
 const browser=readRgbaPng(sourceBytes),nativeBytes=await fs.readFile(path.join(nativeDirectory,observed.file)),windows=readRgbaPng(nativeBytes);
 const originalBytes=await fs.readFile(path.join(sourceDirectory,point.originalFile));assert.equal(hash(originalBytes),point.originalSha256);assert.equal(point.originalSha256,point.originalRepeatSha256);const original=readRgbaPng(originalBytes);
 const pixel=(image,x,y)=>[...image.rgba.subarray((y*image.width+x)*4,(y*image.width+x)*4+4)];
 const originalRgba=pixel(original,Math.ceil(point.source.bounds.x)+10,8),swatchRgba=pixel(browser,16,40),sourceSwatchMatchesOriginal=JSON.stringify(originalRgba)===JSON.stringify(swatchRgba);
 assert.equal(browser.width,1024);assert.equal(browser.height,745);assert.equal(windows.width,1024);assert.equal(windows.height,745);
 const cells=[];
 for(const cell of observed.cells){
  const expected=point.cells.find(item=>item.row===cell.row&&item.column===(cell.column===3?3:0));let differentPixels=0,maxChannel=0;
  const at=(image,x,y)=>[...image.rgba.subarray((y*image.width+x)*4,(y*image.width+x)*4+4)];
  const pixels=new Set();
  for(let y=0;y<32;y++)for(let x=0;x<32;x++){
   const a=at(browser,expected.x+x,expected.y+y),b=at(windows,cell.x+x,cell.y+y);pixels.add(b.join(','));
   let changed=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(a[channel]-b[channel]);changed||=delta!==0;maxChannel=Math.max(maxChannel,delta);}if(changed)differentPixels++;
  }
  assert.equal(pixels.size,1,'Native cell is not a flat paint');
  cells.push({...cell,sourceRgba:at(browser,expected.x,expected.y),nativeRgba:[...pixels][0].split(',').map(Number),differentPixels,maxChannel,masks:0,tolerance:0});
 }
 reports.push({scheme:point.scheme,source:point.source,originalRgba,swatchRgba,sourceSwatchMatchesOriginal,alpha:observed.alpha,rounded:observed.rounded,floor:observed.floor,nativeSha256:hash(nativeBytes),cells});
 if(native.buttons){
  const button=native.buttons.find(item=>item.scheme===point.scheme);assert.ok(button);assert.equal(button.captures.length,4);
  const captures=[];
  for(const capture of button.captures){
   assert.equal(capture.IsPointerOver,true);assert.equal(capture.IsPressed,false);
   const bytes=await fs.readFile(path.join(nativeDirectory,capture.file)),image=readRgbaPng(bytes);
   assert.equal(image.width,1024);assert.equal(image.height,745);
   const background=pixel(image,0,0);assert.deepEqual(background,point.scheme==='Light'?[255,255,255,255]:[13,24,33,255]);
   const sourceX=Math.ceil(point.source.bounds.x)+9,nativeX=Math.ceil(capture.bounds.x)+9,nativeY=Math.ceil(capture.bounds.y)+7;
   const samples=[];let differentPixels=0,maxChannel=0;
   for(let y=0;y<3;y++)for(let x=0;x<4;x++){
    const expected=pixel(original,sourceX+x,7+y),actual=pixel(image,nativeX+x,nativeY+y);let changed=false;
    for(let channel=0;channel<4;channel++){const delta=Math.abs(expected[channel]-actual[channel]);changed||=delta!==0;maxChannel=Math.max(maxChannel,delta);}
    if(changed)differentPixels++;samples.push({source:expected,native:actual});
   }
   captures.push({...capture,sha256:hash(bytes),sourcePatch:{x:sourceX,y:7,width:4,height:3},nativePatch:{x:nativeX,y:nativeY,width:4,height:3},samples,differentPixels,maxChannel,masks:0,tolerance:0});
  }
  buttons.push({scheme:point.scheme,captures});
 }
}
const passed=reports.every(item=>item.sourceSwatchMatchesOriginal);
const buttonPaintPassed=buttons.length===2&&buttons.every(item=>item.captures.find(capture=>capture.variant==='production').differentPixels===0);
await fs.writeFile(output,JSON.stringify({format:'orchard-brush-alpha-comparison-v1',diagnosticComplete:true,passed,buttonPaintPassed,supportsProductionChange:false,environment:native.environment,sourceCommit:source.sourceCommit,masks:0,tolerance:0,reports,buttons},null,2)+'\n');
console.log(JSON.stringify(reports.map(item=>({scheme:item.scheme,alpha:item.alpha,rows:[0,1,2].map(row=>item.cells.filter(cell=>cell.row===row).map(cell=>({column:cell.column,source:cell.sourceRgba,native:cell.nativeRgba,differentPixels:cell.differentPixels})))}))));
console.log(JSON.stringify(buttons.map(item=>({scheme:item.scheme,captures:item.captures.map(capture=>({variant:capture.variant,differentPixels:capture.differentPixels,maxChannel:capture.maxChannel,first:capture.samples[0]}))}))));
if(!passed)process.exitCode=1;
