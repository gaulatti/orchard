import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint,cssRgba} from './reference/css-paint.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const native=path.resolve(option('--native')),app=path.resolve(option('--app')),host=option('--host'),output=path.resolve(option('--output')),referencePath=path.resolve(option('--reference'));
assert(['Orchard.Catalog','PackageConsumer'].includes(host));const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex'),read=async file=>JSON.parse(await fs.readFile(path.join(native,file)));
const architecture=process.argv.includes('--architecture')?option('--architecture'):'X64';assert(['X64','Arm64'].includes(architecture));
const bytes=await fs.readFile(referencePath),reference=JSON.parse(bytes),report=await read('header.json'),exit=await read('process-exit.json');
assert.equal(report.referenceSha256,hash(bytes));assert.equal(reference.sourceSha256,'e6d0c2969a7f042e13cf3261043c74dc8ae9c7caf9c7225db9271ea3d28bd009');assert.equal(reference.cases.length,72);
assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(report.passed,true);assert.equal(report.diagnosticOnly,true);assert.equal(report.scope,'hidden-header-source-layout');assert.equal(report.failure,null);
for(const field of['activateCalled','showCalled','focusCalled','inputSent','actualNativeScrollVerified','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[field],false,field);
assert.equal(report.processArchitecture,architecture);assert.equal(report.osArchitecture,architecture);assert.equal(report.activations,0);assert.equal(report.focus,0);assert(report.guardChecks>=792);
const librarySha256=hash(await fs.readFile(path.join(app,'Orchard.dll'))),hostSha256=hash(await fs.readFile(path.join(app,host+'.dll')));assert.equal(report.librarySha256,librarySha256);assert.equal(report.hostSha256,hostSha256);
for(const [file,expected]of[['Orchard.dll',librarySha256],[host+'.dll',hostSha256]])assert.equal(hash(await fs.readFile(path.join(native,'app-identity',file))),expected);
let packageSha256;if(host==='PackageConsumer'){packageSha256=hash(await fs.readFile(path.resolve(option('--package'))));const build=await read('result.json'),cleanup=await read('cleanup.json');assert.equal(build.buildPassed,true);assert.equal(build.runtimeVerified,false);assert.equal(build.platform.toLowerCase(),architecture.toLowerCase());assert.equal(build.packageSha256,packageSha256);assert.equal(cleanup.removed,true);assert.equal(build.isolatedConsumer,cleanup.isolatedConsumer);await assert.rejects(fs.access(cleanup.isolatedConsumer),{code:'ENOENT'});}
assert.deepEqual(report.frames.map(row=>[row.scheme,row.id]),reference.cases.map(row=>[row.scheme,row.id]));
let geometryChecks=0,paintChecks=0,encodedPaintChecks=0,maximumGeometryError=0;const failures=[];
const fail=(name,error)=>failures.push({name,message:error.message??String(error)});
const box=(actual,expected,name)=>{assert(actual,name+' missing native rectangle');assert(expected,name+' missing source rectangle');for(const key of['x','y','width','height']){assert(Number.isFinite(actual[key]),name+' '+key+' is not finite');const error=Math.abs(actual[key]-expected[key]);maximumGeometryError=Math.max(maximumGeometryError,error);geometryChecks++;if(error>.000001)failures.push({name:name+'.'+key,expected:expected[key],actual:actual[key],error});}};
const paint=(actual,expected,name)=>{paintChecks++;try{assertNativePaint(actual,expected,name);}catch(error){fail(name,error);}};
for(let index=0;index<reference.cases.length;index++){
 const source=reference.cases[index],frame=report.frames[index],id=source.scheme+'/'+source.id,m=source.measurement;
 assert.equal(frame.actualTheme,source.scheme);assert.equal(frame.menuOpen,!!source.spec.open);assert.equal(frame.flowHeight,(source.spec.position??'fixed')==='fixed'?0:(source.spec.headerHeight??72)+1);
 for(const key of['host','following','header','container','row','brand','desktopNavigation','actions','toggle','panel','mobileContent'])box(frame.measurement[key],m[key],id+'/'+key);
 if(m.mobileActions)box(frame.measurement.mobileActions,m.mobileActions,id+'/mobileActions');else assert.equal(frame.measurement.mobileActions,null);
 const links=m.links; if(frame.links.length!==links.length)failures.push({name:id+'/links',expected:links.length,actual:frame.links.length});
 for(let link=0;link<Math.min(frame.links.length,links.length);link++){
  const actual=frame.links[link],expected=links[link];for(const key of['href','label'])if(actual[key]!==expected[key])failures.push({name:id+'/link'+link+'/'+key,expected:expected[key],actual:actual[key]});
  const marker=link===0?null:source.spec.navigation[(link-1)%source.spec.navigation.length].marker;assert.equal(actual.marker,marker,id+'/original item marker');if(source.spec.custom)assert.equal(actual.marker,expected.marker,id+'/custom source marker');
  box(actual.box,expected.box,id+'/link'+link);paint(actual.paint,expected.paint.color,id+'/link'+link+'/color');
 }
 paint(frame.paint.header,m.paint.header.background,id+'/background');paint(frame.paint.bottomRule,m.paint.header.border[1],id+'/bottomRule');paint(frame.paint.mobile,m.paint.panel.background,id+'/mobile');paint(frame.paint.mobileRule,m.paint.panel.topBorder[1],id+'/mobileRule');
 for(const[key,css]of[['header',m.paint.header.background],['bottomRule',m.paint.header.border[1]]]){const[r,g,b,a]=cssRgba(css);assert.deepEqual(frame.renderPaint[key],{color:'#'+[Math.round(a*255),r,g,b].map(v=>v.toString(16).padStart(2,'0').toUpperCase()).join(''),opacity:1},id+'/'+key+'/actual-byte-alpha');encodedPaintChecks++;}
 assert.deepEqual(frame.icon,frame.menuOpen?[[18,6,6,18],[6,6,18,18]]:[[4,5,20,5],[4,12,20,12],[4,19,20,19]]);
}
const audit={passed:failures.length===0,scope:'hidden-header-source-layout',layouts:report.frames.length,geometryChecks,paintChecks,encodedPaintChecks,maximumGeometryError,failures:failures.length,examples:failures.slice(0,80),guardChecks:report.guardChecks,librarySha256,hostSha256,packageSha256,referenceSha256:hash(bytes),actualNativeScrollVerified:false,sourceInputHistoriesVerified:false,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,examples:undefined}));if(!audit.passed)process.exitCode=1;
