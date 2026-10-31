import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const native=path.resolve(option('--native')),app=path.resolve(option('--app')),host=option('--host'),referencePath=path.resolve(option('--reference')),architecture=option('--architecture');
assert(['Orchard.Catalog','PackageConsumer'].includes(host));assert(['X64','Arm64'].includes(architecture));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex'),read=async file=>JSON.parse(await fs.readFile(path.join(native,file)));
const bytes=await fs.readFile(referencePath),reference=JSON.parse(bytes),report=await read('hover-cards.json'),exit=await read('process-exit.json');
assert.equal(report.passed,true);assert.deepEqual(report.failures,[]);assert.equal(report.scope,'hidden-hover-card-source-renderer-and-contracts');assert.equal(report.diagnosticOnly,true);assert.equal(report.stateInjection,'private-production-model-and-presentation-clock');assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);
assert.equal(report.referenceSha256,hash(bytes));assert.equal(reference.format,'orchard-hover-card-reference-v1');assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.cases.length,44);assert.equal(reference.transitions.length,16);
for(const field of ['activateCalled','showCalled','focusCalled','inputSent','sourceRasterParityVerified','nativeInputFocusVerified','visibleRuntimeVerified','nativeComponentAccepted'])assert.equal(report[field],false,field);
assert.equal(report.activations,0);assert.equal(report.focus,0);assert(report.guards>=900);assert.equal(report.processArchitecture,architecture);assert.equal(report.osArchitecture,architecture);
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.hostSha256,hash(await fs.readFile(path.join(app,host+'.dll'))));
let packageSha256;if(host==='PackageConsumer'){packageSha256=hash(await fs.readFile(path.resolve(option('--package'))));const build=await read('result.json'),cleanup=await read('cleanup.json');assert(build.buildPassed===true&&build.runtimeVerified===false||build.passed===true&&build.runtime?.passed===true);assert.equal(build.platform.toLowerCase(),architecture.toLowerCase());assert.equal(build.packageSha256,packageSha256);assert.equal(cleanup.removed,true);assert.equal(build.isolatedConsumer,cleanup.isolatedConsumer);await assert.rejects(fs.access(cleanup.isolatedConsumer),{code:'ENOENT'});}
assert.deepEqual(report.checks.map(r=>r.id),['source-geometry-and-paint-matrix','original-action-and-disabled-trigger','native-hit-query-and-sibling-layer','original-editor-and-template-lifetime','original-model-selector-and-live-size','native-presentation-source-timelines']);assert(report.checks.every(r=>r.passed===true));
assert.deepEqual(report.frames.map(r=>[r.scheme,r.id]),reference.cases.map(r=>[r.scheme,r.id]));
let geometryChecks=0,paintChecks=0,motionSamples=0,maximumGeometryError=0,maximumOpacityError=0;const failures=[];
const near=(actual,expected,name,motion=false)=>{assert(Number.isFinite(actual),name);const error=Math.abs(actual-expected);if(motion)maximumOpacityError=Math.max(maximumOpacityError,error);else{geometryChecks++;maximumGeometryError=Math.max(maximumGeometryError,error);}if(error>(motion?.000002:.000001))failures.push({name,actual,expected,error});};
const box=(a,b,name)=>{assert(a&&b,name);for(const key of ['x','y','width','height'])near(a[key],b[key],name+'/'+key);};
for(const[index,source]of reference.cases.entries()){
 const r=report.frames[index],m=source.measurement,id=source.scheme+'/'+source.id;assert.equal(r.actualTheme,source.scheme);assert.equal(r.hovered,m.hovered);assert.equal(r.disabled,m.disabled);assert.equal(r.role,'tooltip');assert.equal(r.originalValuesRetained,true);assert.equal(r.flowUnchanged,true);near(r.opacity,m.opacity,id+'/opacity',true);assert.equal(r.hitTestVisible,m.pointerEvents==='auto');
 for(const key of ['trigger','wrapper','surface'])box(r[key],m[key],id+'/'+key);if(m.content)box(r.content,m.content,id+'/content');else assert.equal(r.content,null);
 for(const key of ['background','border','color']){assertNativePaint(r.paint[key],m.paint[key],id+'/'+key);paintChecks++;}for(const key of ['radius','borderWidth','fontSize'])near(r.paint[key],m.paint[key],id+'/'+key);assert.deepEqual(r.paint.padding,m.paint.padding);
 assert.equal(hash(await fs.readFile(path.join(path.dirname(referencePath),'HoverCard',source.file))),source.pngSha256);
}
assert.equal(report.timelines.length,16);for(const[index,source]of reference.transitions.entries()){
 const row=report.timelines[index];for(const key of ['scheme','reduced','side','kind'])assert.equal(row[key],source[key]);assert.equal(row.samples.length,6);
 for(const[i,sample]of row.samples.entries()){const expected=source.samples[i],name=source.scheme+'/'+source.side+'/'+source.kind+'/'+expected.milliseconds;assert.equal(sample.milliseconds,expected.milliseconds);near(sample.opacity,expected.opacity,name+'/opacity',true);assert.equal(sample.hitTestVisible,expected.pointerEvents==='auto');near(sample.surface.x,expected.x,name+'/x');near(sample.surface.y,expected.y,name+'/y');motionSamples++;}
}
assert.deepEqual(report.captures.map(r=>r.name),['Light','Dark'].flatMap(s=>['ltr-top-open','ltr-bottom-closed','ltr-bottom-open','clipped'].map(id=>s+'-'+id)));
for(const row of report.captures){assert.equal(row.file,row.name+'.png');assert.equal(row.scope,'complete-native-client');assert.equal(row.completeSourceRasterCompared,false);assert.equal(row.width,800);assert.equal(row.height,600);const bytes=await fs.readFile(path.join(native,row.file));assert.equal(hash(bytes),row.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,800);assert.equal(png.height,600);const closed=row.name.includes('closed'),top=row.name.includes('top'),clipped=row.name.includes('clipped'),x=clipped?340:460,y=top?188:330,at=(y*800+x)*4,expected=closed?[255,255,255,255]:row.name.startsWith('Dark')?[24,37,51,255]:[255,255,255,255];assert.deepEqual([...png.rgba.subarray(at,at+4)],expected,row.name+'/native-fill');}
const audit={passed:failures.length===0,scope:report.scope,sourceStates:44,contractGroups:5,geometryChecks,paintChecks,motionSamples,maximumGeometryError,maximumOpacityError,completeNativeClientPngs:8,failures:failures.length,examples:failures.slice(0,60),guardChecks:report.guards,librarySha256:report.librarySha256,hostSha256:report.hostSha256,packageSha256,sourceRasterParityVerified:false,nativeInputFocusVerified:false,visibleRuntimeVerified:false,nativeComponentAccepted:false};
await fs.writeFile(path.resolve(option('--output')),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,examples:undefined}));if(!audit.passed)process.exitCode=1;
