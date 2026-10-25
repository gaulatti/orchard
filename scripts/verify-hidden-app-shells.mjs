import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import path from 'node:path';
import {assertNativePaint} from './reference/css-paint.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const source=option('--reference'),directory=option('--capture'),bytes=await fs.readFile(source),reference=JSON.parse(bytes),report=JSON.parse(await fs.readFile(path.join(directory,'hidden-app-shells.json'))),errors=[];
assert.equal(reference.format,'orchard-app-shell-reference-v1');assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.sourceSha256,'21cbb676b184bcad28ffb5577b19a1ccc34d18ab420221b08cf936a23919c3e1');assert.equal(reference.cases.length,168);assert.equal(reference.interactions.length,2);
assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-app-shell.cjs',import.meta.url))));assert.equal(reference.clientSha256,hash(await fs.readFile(new URL('./reference/app-shell-client.tsx',import.meta.url))));
let pngs=0;for(const row of reference.cases)if(row.file){assert.equal(row.pngSha256,hash(await fs.readFile(path.join(path.dirname(source),'AppShell',row.file))));pngs++;}assert.equal(pngs,56);
assert.equal(report.scope,'hidden-app-shell-contracts');assert.equal(report.referenceSha256,hash(bytes));assert.equal(report.hiddenWindow,true);assert.equal(report.isolated,true);assert.equal(report.activationEvents,0);
for(const key of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[key],false,key);
assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.equal(report.frames.length,168);assert.equal(report.librarySha256,hash(await fs.readFile(path.join(directory,'app/Orchard.dll'))));assert.deepEqual(report.diagnostics,[]);for(const error of report.failures)errors.push({error});
let maximumGeometryError=0;
const near=(actual,expected,context,geometry=false)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const delta=Math.abs(actual-expected);if(geometry)maximumGeometryError=Math.max(maximumGeometryError,delta);if(delta>=.001)errors.push({context,actual,expected,delta});};
const box=(actual,expected,context)=>{assert(actual&&expected,context);for(const key of ['x','y','width','height'])near(actual[key],expected[key],context+'/'+key,true);};
const css=value=>Number.parseFloat(value),key=row=>row.scheme+'/'+row.id;assert.equal(new Set(report.frames.map(key)).size,168);
for(const row of reference.cases)
{
 const frame=report.frames.find(frame=>key(frame)===key(row));assert(frame);const a=frame.measurement,e=row.measurement,context=key(row);
 for(const part of ['shell','main','content'])box(a[part],e[part],context+'/'+part);
 for(const part of ['header','footer','body'])if(e[part])box(a[part],e[part],context+'/'+part);else assert.equal(a[part],null,context+'/'+part);
 assertNativePaint(a.paint.background,e.paint.shell.background,context+'/background');assertNativePaint(a.paint.foreground,e.paint.shell.color,context+'/foreground');assertNativePaint(a.paint.contentBackground,e.paint.main.background,context+'/main-background');
 assert.deepEqual(a.padding,e.paint.shell.padding.map(css),context+'/padding');assert.deepEqual(a.contentPadding,e.paint.main.padding.map(css),context+'/content-padding');near(a.minimum,css(e.paint.shell.minimum),context+'/minimum');assert.equal(a.text,e.text,context+'/text');assert.equal(a.landmark,'Main');assert.equal(a.tabStop,false);assert.equal(a.loaded,true);assert.equal(a.hostVisible,false);near(a.viewport.width,row.spec.viewport.width,context+'/viewport-width');near(a.viewport.height,row.spec.viewport.height,context+'/viewport-height');
}
const ids=['original-slots','native-landmark','retained-edits-and-controls','actual-viewport','token-binding','zero-padding-binding','clear-padding-restores-recipe','minimum-binding','live-minimum-binding','clear-minimum-restores-viewport','minimum-style','clear-style-restores-viewport','caller-metadata','empty-name-binding','original-template-models','live-template-models','template-selectors','template-detach','template-reapply','unload-observers','remount-state','plain-text-metadata'];assert.deepEqual(report.checks.map(check=>check.id).sort(),['Light','Dark'].flatMap(scheme=>ids.map(id=>scheme+'/'+id)).sort());assert(report.guardChecks>=168*4+44*2);for(const check of report.checks)if(!check.passed)errors.push({context:check.id,error:check.error});
const audit={passed:report.passed&&errors.length===0,frames:168,sourcePngs:56,ownershipChecks:44,maximumGeometryError,errors,guardChecks:report.guardChecks,isolated:true,librarySha256:report.librarySha256,actualHiddenViewportResized:true,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,errors:errors.slice(0,16)}));assert(audit.passed,'Hidden AppShell contract differs from the pinned source.');
