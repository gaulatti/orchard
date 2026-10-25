import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {assertNativePaint,cssRgba} from './reference/css-paint.mjs';

const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const folder=option('--native'),app=option('--app'),source=option('--source'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const referenceBytes=await fs.readFile(new URL('../tests/Reference/Pagination.json',import.meta.url)),reference=JSON.parse(referenceBytes);
const report=JSON.parse(await fs.readFile(path.join(folder,'pagination.json'))),result=JSON.parse(await fs.readFile(path.join(folder,'result.json'))),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));
assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.sourceSha256,'9111a975e78d31dcd8e3c68b588110547b45ee31facad43b81f84bb9e7318834');
assert.equal(report.referenceSha256,hash(referenceBytes));assert.equal(report.scope,'foreground-pagination-source-capture');assert.equal(report.passed,true);
assert.equal(report.rawClientFrames,true);assert.equal(report.foregroundCapture,true);assert.equal(report.sourcePixelsPainted,false);assert.equal(report.nativeComponentAccepted,false);
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.catalogSha256,hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))));
assert.equal(reference.fonts.find(font=>font.weight===500).sha256,hash(await fs.readFile(path.join(app,'Orchard/Assets/Fonts/EncodeSans-Medium.ttf'))));
assert.equal(exit.component,'pagination');assert.equal(exit.exited,true);assert.equal(exit.exitCode,0,'Native Pagination did not exit cleanly.');
assert.equal(result.passed,true);assert.equal(result.component,'pagination');assert.equal(result.environment.processArchitecture,result.environment.osArchitecture);assert(['X64','Arm64'].includes(result.environment.processArchitecture));assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);
assert.equal(reference.cases.length,66);assert.equal(report.frames.length,66);assert.equal(Object.keys(report.captures).length,66);assert.equal(Object.keys(report.rasterMatches).length,66);
const keys=rows=>rows.map(row=>row.scheme+'/'+row.id).sort();assert.deepEqual(keys(report.frames),keys(reference.cases));assert.deepEqual(Object.keys(report.captures).sort(),reference.cases.map(row=>row.file).sort());
let maximumGeometryError=0,underBorderPixelsVerified=0;const geometryErrors=[],rows=[];
const box=(actual,expected,name)=>{assert(actual&&expected,name);for(const field of['x','y','width','height']){assert(Number.isFinite(actual[field]),name+'/'+field);const error=Math.abs(actual[field]-expected[field]);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>.001)geometryErrors.push({name,field,actual:actual[field],expected:expected[field],error});}};
for(const wanted of reference.cases){
 const row=report.frames.find(row=>row.id===wanted.id&&row.scheme===wanted.scheme),actual=row.measurement,expected=wanted.measurement,label=wanted.scheme+'/'+wanted.id;
 assert.equal(actual.visible,expected.visible,label);assert.equal(actual.buttons.length,expected.buttons.length,label);
 if(!expected.visible)assert.equal(actual.nav,null,label);else{assert.equal(actual.label,expected.label,label);box(actual.nav,expected.nav,label+'/nav');}
 for(let index=0;index<expected.buttons.length;index++){const a=actual.buttons[index],e=expected.buttons[index];for(const field of['text','title','disabled','focused'])assert.equal(a[field],e[field],label+'/'+index+'/'+field);assert.equal(a.opacity,Math.fround(Number(e.paint.opacity)),label+'/'+index+'/native-float-opacity');box(a.box,e.box,label+'/'+index);for(const [field,css]of [['foreground',e.paint.color],['background',e.paint.background],['border',e.paint.border]]){const context=label+'/'+index+'/'+field;if(cssRgba(css)[3]===0){assert.equal(parseInt(a.paint[field].color.slice(1,3),16)/255*a.paint[field].opacity,0,context);}else assertNativePaint(a.paint[field],css,context);}}
 const sourceBytes=await fs.readFile(path.join(source,wanted.file)),nativeBytes=await fs.readFile(path.join(folder,wanted.file));assert.equal(hash(sourceBytes),wanted.pngSha256);assert.equal(hash(nativeBytes),report.captures[wanted.file]);
 const expectedPng=readRgbaPng(sourceBytes),actualPng=readRgbaPng(nativeBytes);assert.deepEqual([actualPng.width,actualPng.height],[expectedPng.width,expectedPng.height]);assert.deepEqual([actualPng.width,actualPng.height],[700,160]);
 // CSS fills the border box, including under transparent borders. This gate
 // checks actual composed paint; the full unmasked comparison still follows.
 if(expected.visible&&!wanted.spec.state)for(const button of expected.buttons){
  if(button.title!==''||Number(button.text)===wanted.spec.currentPage)continue;
  const x=Math.floor(button.box.x+button.box.width/2),y=Math.floor(button.box.y);
  if(x<0||x>=actualPng.width||y<0||y>=actualPng.height)continue;
  const index=(y*actualPng.width+x)*4;
  assert.deepEqual([...actualPng.rgba.slice(index,index+4)],[...expectedPng.rgba.slice(index,index+4)],label+'/transparent-border-fill');underBorderPixelsVerified++;
 }
 let differingPixels=0;for(let index=0;index<actualPng.rgba.length;index+=4){let different=false;for(let channel=0;channel<4;channel++)different||=actualPng.rgba[index+channel]!==expectedPng.rgba[index+channel];differingPixels+=Number(different);}
 assert.equal(report.rasterMatches[wanted.file],differingPixels===0);rows.push({id:wanted.id,scheme:wanted.scheme,file:wanted.file,differingPixels,sha256:hash(nativeBytes)});
}
assert.equal(report.interactions.length,4);assert.deepEqual(report.interactions.map(row=>row.scheme+'/'+row.accept).sort(),['Dark/false','Dark/true','Light/false','Light/true']);
for(const actual of report.interactions){const wanted=reference.interactions.find(row=>row.scheme===actual.scheme&&row.accept===actual.accept);assert(wanted);assert.deepEqual(actual.samples,wanted.samples);for(const flag of['bindingRetained','focusedButtonRetained','disabledRejected','detachedRejected','emptySpaceRemoved','remount','templateReplacement'])assert.equal(actual[flag],true,flag);assert.equal(actual.contradictoryAvailabilityRequest,0);}
const audit={passed:geometryErrors.length===0,scope:report.scope,librarySha256:report.librarySha256,catalogSha256:report.catalogSha256,sourceStates:66,rawPngs:66,maximumGeometryError,geometryErrors,underBorderPixelsVerified,interactionSequences:4,sourceInputSamples:12,controlledRequestsVerified:true,sourceAvailabilityVerified:true,rows,exactPngs:rows.filter(row=>row.differingPixels===0).length,differingPixels:rows.reduce((sum,row)=>sum+row.differingPixels,0),masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:rows.every(row=>row.differingPixels===0),nativeComponentAccepted:false,nativeARM64RuntimeVerified:result.environment.processArchitecture==='Arm64'};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,geometryErrors:geometryErrors.slice(0,12),rows:undefined}));assert.equal(audit.passed,true,'Native Pagination geometry differs from source.');
