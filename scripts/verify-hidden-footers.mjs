import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),output=option('--output'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const packageConsumer=process.argv.includes('--package-consumer');
const bytes=await fs.readFile(new URL('../tests/Reference/Footer.json',import.meta.url)),reference=JSON.parse(bytes),report=JSON.parse(await fs.readFile(path.join(native,'hidden-footers.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));
assert.equal(report.scope,packageConsumer?'hidden-footer-package-layout-diagnostic':'hidden-footer-repeated-layout-diagnostic');assert.equal(report.referenceSha256,hash(bytes));assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));
const hostSha256=packageConsumer?report.consumerSha256:report.catalogSha256;assert.equal(hostSha256,hash(await fs.readFile(path.join(app,packageConsumer?'PackageConsumer.dll':'Orchard.Catalog.dll'))));
let packageSha256;
if(packageConsumer){const build=JSON.parse(await fs.readFile(path.join(native,'result.json'))),cleanup=JSON.parse(await fs.readFile(path.join(native,'cleanup.json')));packageSha256=hash(await fs.readFile(option('--package')));
 // verify-package emits either its explicit build-only receipt or a complete
 // generic consumer runtime receipt. Footer runtime is checked separately here.
 if(Object.hasOwn(build,'buildPassed')){assert.equal(build.buildPassed,true);assert.equal(build.runtimeVerified,false);}else{assert.equal(build.passed,true);assert.equal(build.runtime.passed,true);assert.equal(build.runtime.architecture.toLowerCase(),report.processArchitecture.toLowerCase());}
 assert.equal(build.packageSha256,packageSha256);assert.equal(build.platform.toLowerCase(),report.processArchitecture.toLowerCase());assert.equal(cleanup.removed,true);
 // The current isolated package builder records isolatedProject/cleanup.path;
 // earlier Footer builders recorded isolatedConsumer in both receipts.
 const currentReceipt=Object.hasOwn(build,'isolatedProject');
 const isolated=currentReceipt?build.isolatedProject:build.isolatedConsumer;
 const cleaned=currentReceipt?cleanup.path:cleanup.isolatedConsumer;
 assert.equal(typeof isolated,'string');assert(isolated.length>0);assert.equal(isolated,cleaned);await assert.rejects(fs.access(isolated));}
for(const flag of ['passed','diagnosticOnly','hiddenWindow','isolated'])assert.equal(report[flag],true,flag);
for(const flag of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[flag],false,flag);
assert.equal(report.activationEvents,0);assert.equal(report.focusEvents,0);assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.equal(report.exceptionCount,0);assert(Number.isInteger(report.guardChecks)&&report.guardChecks>=reference.cases.length*3*9+reference.cases.reduce((sum,row)=>sum+(row.spec.mutations?.length??0),0)*3*4);assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);
const key=row=>row.cycle+'/'+row.scheme+'/'+row.id;assert.equal(reference.cases.length,64);assert.equal(report.frames.length,3*reference.cases.length);assert.deepEqual(report.frames.map(key).sort(),[0,1,2].flatMap(cycle=>reference.cases.map(row=>key({...row,cycle}))).sort());
let geometryChecks=0,maximumGeometryError=0,paintChecks=0;const geometryErrors=[],paintErrors=[];
const box=(a,e,label)=>{if(a===null||e===null){assert.equal(a,e,label);return;}assert(a&&e,label);for(const field of ['x','y','width','height']){assert(Number.isFinite(a[field]),label);const error=Math.abs(a[field]-e[field]);geometryChecks++;maximumGeometryError=Math.max(maximumGeometryError,error);if(error>.001)geometryErrors.push({label:label+'/'+field,actual:a[field],expected:e[field],error});}};
const paint=(a,e,label)=>{paintChecks++;try{assertNativePaint(a,e,label);}catch(error){paintErrors.push({label,error:error.message});}};
for(const actual of report.frames){const wanted=reference.cases.find(row=>row.id===actual.id&&row.scheme===actual.scheme),e=wanted.measurement,label=key(actual);assert.equal(actual.mutationsApplied,wanted.spec.mutations?.length??0,label);
 for(const name of ['footer','container','grid','brand','description','bottom'])box(actual[name],e[name],label+'/'+name);box(actual.preparedBrand,e.links[0].box,label+'/preparedBrand');assert.equal(actual.links.length,e.links.length,label);
 box(actual.bottomLeft,e.bottomLeft.box,label+'/bottom-left');box(actual.bottomRight,e.bottomRight.box,label+'/bottom-right');assert.equal(actual.sections.length,e.sections.length,label);for(let index=0;index<e.sections.length;index++){assert.equal(actual.sections[index].title,e.sections[index].title);box(actual.sections[index].box,e.sections[index].box,label+'/section/'+index);box(actual.sections[index].heading,e.sections[index].heading,label+'/heading/'+index);}
 const originalItems=wanted.spec.sections.flatMap(section=>section.items);
 for(let index=0;index<e.links.length;index++){const a=actual.links[index],w=e.links[index];box(a.box,w.box,label+'/link/'+index);assert.equal(a.href,w.href,label);if(index!==0){const original=originalItems[index-1]??originalItems.find(item=>item.href.includes('github.com'));assert(original,label);assert.equal(a.originalItem,true,label);assert.equal(a.label,original.label,label);assert.equal(a.marker,original.marker,label);paint(a.paint,w.paint.color,label+'/link/'+index);}else{assert.equal(a.label,wanted.spec.brand.name??wanted.spec.brand.logoAlt??'gaulatti',label);assert.equal(a.originalItem,false,label);}}
 paint(actual.paint.footer,e.paint.footer.background,label+'/background');paint(actual.paint.topRule,e.paint.topRule.background,label+'/top-rule');paint(actual.paint.bottomRule,e.paint.bottom.border[1],label+'/bottom-rule');if(e.accent)paint(actual.paint.accent,e.paint.accent.background,label+'/accent');
}
const names=['footer-background-command','footer-background-motion-callback','footer-background-collection','nav-background-command','nav-background-collection','footer-template-retirement','footer-detached-action'];assert.equal(report.checks.length,7);assert.deepEqual(report.checks.map(row=>row.id).sort(),names.sort());assert(report.checks.every(row=>row.passed));
for(const check of report.checks.filter(row=>row.id.endsWith('command')||row.id.endsWith('motion-callback'))){assert(Number.isInteger(check.producerThread));assert(Number.isInteger(check.uiThread));assert.notEqual(check.producerThread,check.uiThread);}
const passed=geometryErrors.length===0&&paintErrors.length===0;const audit={passed,scope:packageConsumer?'hidden-footer-package-source-layout-paint-diagnostic':'hidden-footer-source-layout-paint-diagnostic',diagnosticOnly:true,sourceStates:reference.cases.length,cycles:3,frames:report.frames.length,geometryChecks,maximumGeometryError,geometryErrors,paintChecks,paintErrors,ownershipChecks:7,guardChecks:report.guardChecks,activationEvents:0,focusEvents:0,isolated:true,librarySha256:report.librarySha256,hostSha256,packageSha256,packageConsumerRuntimeVerified:packageConsumer,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,geometryErrors:geometryErrors.length,paintErrors:paintErrors.length}));assert(passed,'Hidden Footer geometry/paint differs; see diagnostic.');
