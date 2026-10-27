import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return process.argv[i+1];};
const native=path.resolve(option('--native')),app=path.resolve(option('--app')),host=option('--host'),referencePath=path.resolve(option('--reference'));
assert(['Orchard.Catalog','PackageConsumer'].includes(host));const hash=b=>crypto.createHash('sha256').update(b).digest('hex');const read=async file=>JSON.parse(await fs.readFile(path.join(native,file)));
const bytes=await fs.readFile(referencePath),reference=JSON.parse(bytes),report=await read('popovers.json'),exit=await read('process-exit.json');
assert.equal(report.passed,true);assert.deepEqual(report.failures,[]);assert.equal(report.scope,'hidden-popover-source-layout-and-contracts');assert.equal(report.diagnosticOnly,true);assert.equal(exit.exitCode,0);assert.equal(exit.exited,true);assert.equal(report.referenceSha256,hash(bytes));
for(const field of ['activateCalled','showCalled','focusCalled','inputSent','sourceRasterParityVerified','nativeInputFocusVerified','visibleRuntimeVerified','nativeComponentAccepted'])assert.equal(report[field],false,field);
assert.equal(report.activations,0);assert.equal(report.focus,0);assert(report.guardChecks>=750);assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.hostSha256,hash(await fs.readFile(path.join(app,host+'.dll'))));
const architecture=process.argv.includes('--architecture')?option('--architecture'):'X64';assert(['X64','Arm64'].includes(architecture));assert.equal(report.processArchitecture,architecture);assert.equal(report.osArchitecture,architecture);
let packageSha256,consumerReceiptKind;if(host==='PackageConsumer'){
 packageSha256=hash(await fs.readFile(path.resolve(option('--package'))));
 const build=await read('result.json'),cleanup=await read('cleanup.json');
 assert.equal(build.platform.toLowerCase(),architecture.toLowerCase());
 assert.equal(build.packageSha256,packageSha256);
 if(build.buildPassed===true){
  assert.equal(build.runtimeVerified,false);
  consumerReceiptKind='build';
 }else{
  assert.equal(build.passed,true);
  assert.equal(build.runtime?.passed,true);
  assert.equal(build.runtime.architecture,architecture);
  assert.equal(build.runtime.osArchitecture,architecture);
  assert.equal(hash(await fs.readFile(build.runtime.assembly)),report.librarySha256);
  consumerReceiptKind='runtime';
 }
 assert.equal(cleanup.removed,true);
 assert.equal(build.isolatedConsumer,cleanup.isolatedConsumer);
 await assert.rejects(fs.access(cleanup.isolatedConsumer),{code:'ENOENT'});
}
assert.deepEqual(report.captures.map(row=>row.scheme),['Light','Dark']);for(const row of report.captures){assert.equal(row.file,'popover-'+row.scheme+'.png');assert.equal(row.scope,'native-popup-child-only');assert.equal(row.completeSourceRasterCompared,false);assert.equal(row.pixelWidth,224);assert.equal(row.pixelHeight,78);const data=await fs.readFile(path.join(native,row.file));assert.equal(hash(data),row.sha256);const png=readRgbaPng(data);assert.equal(png.width,224);assert.equal(png.height,78);const at=(40*224+200)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],row.scheme==='Light'?[255,255,255,255]:[24,37,51,255]);}
assert.equal(reference.format,'orchard-popover-reference-v1');assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.cases.length,122);assert.equal(report.frames.length,122);assert.equal(new Set(report.frames.map(row=>row.scheme+'/'+row.id)).size,122);
assert.deepEqual(report.checks.map(row=>row.id),['source-layout-matrix','native-trigger-command-before-request-and-cancellation','controlled-request-and-separate-default-history','original-edit-state-template-retirement-and-remount','disabled-original-trigger-and-programmatic-state','original-template-models-and-live-intrinsic-size']);assert(report.checks.every(row=>row.passed===true));
const failures=[];let geometryChecks=0,paintChecks=0,maximumGeometryError=0;
const near=(actual,expected,context)=>{assert.equal(typeof actual,'number',context);assert(Number.isFinite(actual),context);const error=Math.abs(actual-expected);geometryChecks++;maximumGeometryError=Math.max(maximumGeometryError,error);if(error>.000001)failures.push({context,actual,expected,error});};
const box=(a,e,context)=>{assert(a&&e,context);for(const key of ['x','y','width','height'])near(a[key],e[key],context+'/'+key);};
for(const[index,source]of reference.cases.entries()){
 const frame=report.frames[index],id=source.scheme+'/'+source.id,m=source.measurement;assert.equal(frame.id,source.id);assert.equal(frame.scheme,source.scheme);assert.equal(frame.actualTheme,source.scheme);assert.equal(frame.expanded,m.expanded);assert.equal(frame.disabled,m.disabled);box(frame.trigger,m.trigger,id+'/trigger');
 const png=await fs.readFile(path.join(path.dirname(referencePath),'Popover',source.file));assert.equal(hash(png),source.pngSha256);
 if(!m.popup){assert.equal(frame.popup,null);continue;}
 const a=frame.popup,e=m.popup;box(a.box,e.box,id+'/popup');assert.equal(a.side,e.side,id+'/side');assert.equal(a.align,e.align,id+'/align');if(e.content)box(a.content,e.content,id+'/content');else assert.equal(a.content,null);
 for(const key of ['background','border','color']){assertNativePaint(a.paint[key],e.paint[key],id+'/'+key);paintChecks++;}
 for(const key of ['radius','borderWidth','minimum','fontSize'])near(a.paint[key],e.paint[key],id+'/'+key);assert.deepEqual(a.paint.padding,e.paint.padding);
}
const audit={passed:failures.length===0,scope:report.scope,sourceStates:122,openStates:120,contractChecks:report.checks.length-1,geometryChecks,paintChecks,maximumGeometryError,failures:failures.length,examples:failures.slice(0,70),guardChecks:report.guardChecks,librarySha256:report.librarySha256,hostSha256:report.hostSha256,packageSha256,consumerReceiptKind,sourceRasterParityVerified:false,nativeInputFocusVerified:false,visibleRuntimeVerified:false,nativeComponentAccepted:false};
await fs.writeFile(path.resolve(option('--output')),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,examples:undefined}));if(!audit.passed)process.exitCode=1;
