import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import path from 'node:path';
import {assertNativePaint} from './reference/css-paint.mjs';

const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return process.argv[index+1];};
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const source=option('--reference'),directory=option('--capture');
const bytes=await fs.readFile(source),reference=JSON.parse(bytes);
const report=JSON.parse(await fs.readFile(path.join(directory,'hidden-selects.json'))),errors=[];
assert.equal(reference.format,'orchard-select-reference-v1');
assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');
assert.equal(reference.sourceSha256,'874ad668945afb6f045d881447a275f73b9dc84ffbf56f4beed02a83f8d3fecd');
assert.equal(reference.headerSourceSha256,'848685cb40ba19fdcf8991b0332f98f447b8bd2af6e7febcfc7c27e9517d8f2e');
assert.equal(reference.cases.length,292);assert.equal(reference.openCases.length,32);assert.equal(reference.interactions.length,8);
assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-select.cjs',import.meta.url))));
assert.equal(reference.clientSha256,hash(await fs.readFile(new URL('./reference/select-client.tsx',import.meta.url))));
let sourcePngs=0;
for(const row of [...reference.cases,...reference.openCases])if(row.file){assert.equal(row.pngSha256,hash(await fs.readFile(path.join(path.dirname(source),'Select',row.file))));sourcePngs++;}
assert.equal(sourcePngs,92);
assert.equal(report.scope,'hidden-select-contracts');assert.equal(report.referenceSha256,hash(bytes));
assert.equal(report.hiddenWindow,true);assert.equal(report.isolated,true);assert.equal(report.activationEvents,0);
for(const key of ['activateCalled','showCalled','focusCalled','inputSent','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[key],false,key);
assert.equal(report.processArchitecture,report.osArchitecture);assert(['X64','Arm64'].includes(report.processArchitecture));
assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.equal(report.frames.length,292);
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(directory,'app/Orchard.dll'))));assert.deepEqual(report.diagnostics,[]);
for(const error of report.failures)errors.push({error});
let maximumGeometryError=0,paintCases=0,focusedPaintDeferred=0;
const near=(actual,expected,context,geometry=false)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const delta=Math.abs(actual-expected);if(geometry)maximumGeometryError=Math.max(maximumGeometryError,delta);if(delta>=.001)errors.push({context,actual,expected,delta});};
const box=(actual,expected,context)=>{assert(actual&&expected,context);for(const key of ['x','y','width','height'])near(actual[key],expected[key],context+'/'+key,true);};
const key=row=>row.scheme+'/'+row.id;assert.equal(new Set(report.frames.map(key)).size,292);
for(const row of reference.cases)
{
 const frame=report.frames.find(frame=>key(frame)===key(row));assert(frame);
 const a=frame.measurement,e=row.measurement,context=key(row);
 box(a.trigger,e.trigger,context+'/trigger');box(a.value.box,e.value.box,context+'/value');box(a.arrow,e.arrow,context+'/arrow');
 if(e.icon)box(a.icon,e.icon,context+'/icon');else assert.equal(a.icon,null,context+'/icon');
 assert.equal(a.value.text,e.value.text,context+'/text');
 near(a.fontSize,e.fontSize,context+'/font-size');near(a.lineHeight,e.lineHeight,context+'/line-height');
 assert.deepEqual(a.padding,e.padding,context+'/padding');near(a.gap,e.gap,context+'/gap');
 near(a.paint.radius,Number.parseFloat(e.paint.radius),context+'/radius');
 if(!e.focused){assertNativePaint(a.paint.background,e.paint.background,context+'/background');assertNativePaint(a.paint.border,e.paint.border,context+'/border');paintCases++;}else focusedPaintDeferred++;
 assertNativePaint(a.paint.color,e.paint.color,context+'/foreground');near(a.paint.opacity,e.paint.opacity,context+'/opacity');
 assert.equal(a.name,e.name,context+'/name');assert.equal(a.required,e.required,context+'/required');assert.equal(a.error,e.error,context+'/error');assert.equal(a.disabled,e.disabled,context+'/disabled');
 assert.equal(a.rawValue,row.spec.value??'',context+'/raw-value');assert.equal(a.expanded,'Collapsed');assert.equal(a.focused,'Unfocused');assert.equal(a.role,'ComboBox');assert.equal(a.loaded,true);assert.equal(a.hostVisible,false);
 const originals=row.spec.options??[{value:'a'},{value:'b'},{value:'disabled'}];assert.equal(a.optionCount,originals.length+(row.spec.header&&!originals.some(option=>option.value==='')?1:0),context+'/option-count');
}
assert.equal(paintCases,288);assert.equal(focusedPaintDeferred,4);
const historyKey=row=>`${row.scheme}/${row.header}/${row.accept}`;
assert.equal(report.histories.length,8);assert.equal(new Set(report.histories.map(historyKey)).size,8);
for(const row of reference.interactions)
{
 const a=report.histories.find(history=>historyKey(history)===historyKey(row));assert(a);
 for(const stage of ['first','controlled','released'])assert.equal(a[stage],row[stage].value.text,historyKey(row)+'/'+stage);
 assert.deepEqual(a.requests,row.requests);assert.equal(a.bindingRetained,true);assert.equal(a.rawValue,'');assert.equal(a.popupOpen,false);assert.equal(a.focus,'Unfocused');
}
const ids=['default-peer','controlled-reject','controlled-accept-binding','readonly-value','disabled-option','disabled-control','live-option','keyed-option-replacement','stale-option','caller-metadata','caller-description','empty-name-binding','empty-style-name','caller-label','caller-geometry','original-template-model','live-template-model','template-selector-model','template-detach','template-reapply','unload-observers','remount-binding','late-autofocus','legacy-filter-style','legacy-filter-reject','legacy-filter-accept','legacy-filter-empty','legacy-pagination-style','legacy-pagination-request'];
assert.deepEqual(report.checks.map(check=>check.id).sort(),['Light','Dark'].flatMap(scheme=>ids.map(id=>scheme+'/'+id)).sort());
assert(report.guardChecks>=292*4+58*2);for(const check of report.checks)if(!check.passed)errors.push({context:check.id,error:check.error});
const audit={passed:report.passed&&errors.length===0,frames:292,paintCases,focusedPaintDeferred,sourcePngs,requestHistories:8,ownershipChecks:report.checks.length,maximumGeometryError,errors,guardChecks:report.guardChecks,isolated:true,librarySha256:report.librarySha256,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,errors:errors.slice(0,16)}));assert(audit.passed,'Hidden Select contract differs from the pinned source.');
