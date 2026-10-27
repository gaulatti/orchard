import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import path from 'node:path';
import {assertNativePaint} from './reference/css-paint.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const source=option('--reference'),directory=option('--capture'),bytes=await fs.readFile(source),reference=JSON.parse(bytes),report=JSON.parse(await fs.readFile(path.join(directory,'hidden-feed-columns.json'))),errors=[];
assert.equal(reference.sourceSha256,'981a086c1bbf1ccd1f98399cef9bd097992e4e50f2dcd27beae354beac0ac2a3');assert.equal(reference.cases.length,154);assert.equal(report.scope,'hidden-feed-column-contracts');assert.equal(report.referenceSha256,crypto.createHash('sha256').update(bytes).digest('hex'));
assert.equal(report.hiddenWindow,true);assert.equal(report.isolated,true);assert.equal(report.activationEvents,0);for(const key of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[key],false,key);
assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.equal(report.frames.length,154);
assert.equal(report.librarySha256,crypto.createHash('sha256').update(await fs.readFile(path.join(directory,'app/Orchard.dll'))).digest('hex'));assert.deepEqual(report.diagnostics,[]);for(const error of report.failures)errors.push({error});
let maximumGeometryError=0;
const near=(actual,expected,context)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const delta=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,delta);if(delta>=.001)errors.push({context,actual,expected,delta});};
const box=(actual,expected,context)=>{assert(actual&&expected,context);for(const key of ['x','y','width','height'])near(actual[key],expected[key],context+'/'+key);};
const key=row=>row.scheme+'/'+row.id;assert.equal(new Set(report.frames.map(key)).size,154);
for(const row of reference.cases){const frame=report.frames.find(frame=>key(frame)===key(row));assert(frame);const a=frame.measurement,e=row.measurement,context=key(row);for(const part of ['column','header','row','dot','feed'])box(a[part],e[part],context+'/'+part);box(a.title.box,e.title.box,context+'/title');
 if(e.count){assert(a.count);box(a.count.box,e.count.box,context+'/count');assert.equal(a.count.text,e.count.text,context+'/count-text');}else assert.equal(a.count,null);
 if(e.filter)box(a.filter,e.filter,context+'/filter');if(e.content)box(a.content,e.content,context+'/content');
 assert.equal(a.title.text.replace(/\s+/g,' ').trim(),(row.spec.title??'Live coverage').toUpperCase().replace(/\s+/g,' ').trim(),context+'/title-text');
 // DOM client/scroll dimensions are integers; native viewport/extent retain DIPs.
 near(Math.round(a.scroll.viewportHeight),e.scroll.viewportHeight,context+'/viewport-height');near(Math.round(a.scroll.extentHeight),e.scroll.extentHeight,context+'/extent-height');near(a.scroll.offset,e.scroll.offset,context+'/offset');
 // The Feed Column keeps its dark surface in a Light host and now uses the
 // source's dark accent token there. Caller-supplied accents remain unchanged.
 const accentReference=row.scheme==='Light'&&!row.spec.accent?reference.cases.find(candidate=>candidate.scheme==='Dark'&&candidate.id===row.id):row;
 assert(accentReference,context+'/accent-reference');
 assertNativePaint(a.border,accentReference.measurement.paint.column.border,context+'/border');assertNativePaint(a.headerBorder,e.paint.header.border,context+'/header-border');assertNativePaint(a.dotPaint,accentReference.measurement.paint.dot.background,context+'/dot-paint');if(e.count){assertNativePaint(a.countBackground,e.paint.count.background,context+'/count-background');assertNativePaint(a.countBorder,e.paint.count.border,context+'/count-border');}
 near(a.radius,Number.parseFloat(e.paint.column.radius),context+'/radius');assert.equal(a.name,row.spec.title??'Live coverage');assert.equal(a.role,'Group');assert.equal(a.tabStop,false);assert.equal(a.loaded,true);assert.equal(a.hostVisible,false);
}
const ids=['default-peer','retained-controls-and-scroll','bound-header-and-accent','name-binding','empty-name-binding','empty-style-name','caller-label','original-template-model','live-template-model','template-selector-model','template-detach','template-reapply','unload-observers','remount-binding'];
assert.deepEqual(report.checks.map(check=>check.id).sort(),['Light/dark-feed-surface-contrast',...['Light','Dark'].flatMap(scheme=>ids.map(id=>scheme+'/'+id))].sort());assert(report.guardChecks>=154*4+29*2);for(const check of report.checks)if(!check.passed)errors.push({context:check.id,error:check.error});
const audit={passed:report.passed&&errors.length===0,frames:154,ownershipChecks:report.checks.length,maximumGeometryError,errors,guardChecks:report.guardChecks,isolated:true,librarySha256:report.librarySha256,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,errors:errors.slice(0,16)}));assert(audit.passed,'Hidden FeedColumn contract differs from the pinned source.');
