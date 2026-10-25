import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';

const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const bytes=await fs.readFile(new URL('../tests/Reference/NavMenu.json',import.meta.url));
const reference=JSON.parse(bytes),report=JSON.parse(await fs.readFile(path.join(native,'nav-menus.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'nav-menus-exit.json')));
assert.equal(reference.sourceSha256,'1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5');
assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');
assert.equal(report.passed,true);assert.equal(report.scope,'hidden-nav-menu-contracts');assert.equal(report.referenceSha256,hash(bytes));assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));
for(const flag of ['hiddenWindow','isolated'])assert.equal(report[flag],true,flag);
for(const flag of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[flag],false,flag);
assert.equal(report.activationEvents,0);assert.equal(report.focusEvents,0);assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.deepEqual(report.failures,[]);
assert(Number.isInteger(report.guardChecks)&&report.guardChecks>=300);assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);
const key=row=>row.scheme+'/'+row.id;assert.equal(reference.cases.length,42);assert.deepEqual(report.frames.map(key).sort(),reference.cases.map(key).sort());
let geometryChecks=0,maximumGeometryError=0,paintChecks=0,metadataChecks=0;
const box=(a,e,label)=>{assert(a&&e,label);for(const field of ['x','y','width','height']){assert(Number.isFinite(a[field]),label);const error=Math.abs(a[field]-e[field]);maximumGeometryError=Math.max(maximumGeometryError,error);geometryChecks++;assert(error<=.001,label+'/'+field);}};
for(const wanted of reference.cases){
 const actual=report.frames.find(row=>key(row)===key(wanted)),e=wanted.measurement,label=key(wanted),normal=!Object.hasOwn(wanted.spec,'state');
 box(actual.host,e.host,label+'/host');box(actual.root,e.root,label+'/root');assert.equal(actual.normalPaint,normal,label);assert.equal(actual.links.length,e.links.length,label);
 for(let index=0;index<e.links.length;index++){
  const a=actual.links[index],w=e.links[index],item=wanted.spec.items[index],location=label+'/'+index;box(a.box,w.box,location);
  assert.equal(a.label,w.label,location);assert.equal(a.name,w.label,location);assert.equal(a.href,item.href,location);assert.equal(a.external,item.external??false,location);assert.equal(a.marker,item.marker,location);assert.equal(a.originalItem,true,location);assert.equal(a.role,'Hyperlink',location);assert(a.fontFamily.includes('EncodeSans-Medium.ttf'),location);
  assert.equal(a.fontSize,parseFloat(w.fontSize),location);assert.equal(a.fontWeight,Number(w.fontWeight),location);assert.equal(a.lineHeight,parseFloat(w.lineHeight),location);assert.equal(a.characterSpacing,w.letterSpacing==='normal'?0:Math.round(parseFloat(w.letterSpacing)/parseFloat(w.fontSize)*1000),location);metadataChecks+=12;
  if(normal){assertNativePaint(a.paint,w.color,location);paintChecks++;}else assert.equal(a.paint,null,location+' must not claim hidden pointer/focus paint');
 }
}
const names=['binding-original','reorder','same-key-original','command-original','inactive','changed-key-stale','template-binding','detached'];
assert.equal(report.checks.length,16);assert.deepEqual(report.checks.map(row=>row.id).sort(),['Light','Dark'].flatMap(theme=>names.map(name=>theme+'-'+name)).sort());assert(report.checks.every(row=>row.passed));
const audit={passed:true,scope:report.scope,librarySha256:report.librarySha256,sourceStates:42,geometryChecks,maximumGeometryError,paintChecks,metadataChecks,checks:16,guardChecks:report.guardChecks,isolated:true,activationEvents:0,focusEvents:0,processArchitecture:report.processArchitecture,packageConsumerRuntimeVerified:true,visibleRuntimeVerified:false,inputStatesReplayed:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
