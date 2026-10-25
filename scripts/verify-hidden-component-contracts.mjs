import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const native=option('--native'),referenceBytes=await fs.readFile(new URL('../tests/Reference/ThemeToggle.json',import.meta.url)),reference=JSON.parse(referenceBytes),report=JSON.parse(await fs.readFile(path.join(native,'hidden-component-contracts.json'),'utf8'));
assert.equal(report.scope,'hidden-component-contracts');
for(const field of['passed','hiddenWindow','isolated'])assert.equal(report[field],true,field);
for(const field of['activateCalled','showCalled','focusCalled','inputSent','enabledButtonInvoked','visible','nativeVisible','visibleRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[field],false,field);
assert.equal(report.activationEvents,0);assert.equal(report.failure,null);assert.equal(report.isolationFailure,null);assert.deepEqual(report.failures,[]);
assert(['X64','Arm64'].includes(report.processArchitecture));assert.equal(report.processArchitecture,report.osArchitecture);assert.equal(report.displayScale,1);
assert.equal(report.referenceSha256,hash(referenceBytes));assert.equal(report.librarySha256,hash(await fs.readFile(path.join(native,'app/Orchard.dll'))));
const rows=reference.cases.filter(row=>row.spec.state==='normal'),key=row=>row.id+'/'+row.scheme;
assert.equal(rows.length,116);assert.deepEqual(report.frames.map(key).sort(),rows.map(key).sort());
const expectedChecks=['default-name-tip','style-empty-name','style-tooltip','style-mode-update','local-bindings-win','bound-empty-values','clear-restores-style','style-empty-tooltip','invalid-null-style-setter','style-removal-restores-default','local-null-tooltip','clear-restores-default-tip','caller-label','shared-style-metadata','content-binding','empty-name-with-content','style-empty-name-with-content','disabled-invoke','template-binding','detached-invoke','remount-binding'];
assert.deepEqual(report.checks.map(check=>check.id).sort(),['Light','Dark'].flatMap(scheme=>expectedChecks.map(id=>scheme+'/'+id)).sort());
for(const check of report.checks)assert.equal(check.passed,true,check.id);
assert(report.guardChecks>=rows.length*3+report.checks.length*2);
let maximumGeometryError=0,maximumPropertyError=0;const errors=[];
function near(a,b,context,geometry=false){assert(Number.isFinite(a)&&Number.isFinite(b),context);const error=Math.abs(a-b);if(geometry)maximumGeometryError=Math.max(maximumGeometryError,error);else maximumPropertyError=Math.max(maximumPropertyError,error);if(error>=.001)errors.push({context,actual:a,expected:b,error});}
for(const row of rows){const actual=report.frames.find(frame=>key(frame)===key(row)).measurement,expected=row.measurement,context=key(row);
 for(const part of['box','icon'])for(const field of['x','y','width','height'])near(actual[part][field],expected[part][field],context+'/'+part+'/'+field,true);
 for(const field of['mode','label','title','disabled'])assert.deepEqual(actual[field],expected[field],context+'/'+field);
 assert.equal(actual.loaded,true);assert.equal(actual.role,'Button');assert.equal(actual.tabStop,true);assert.equal(actual.paths,expected.paths.length);assert.deepEqual(actual.strokeRgba,expected.strokeRgba);near(actual.strokeWidth,1.5,context+'/stroke-width');near(actual.strokeOpacity,1,context+'/stroke-opacity');near(actual.iconOpacity,expected.disabled?.45:1,context+'/disabled-opacity');
}
const audit={passed:errors.length===0,normalStateFrames:report.frames.length,nativeOwnershipChecks:report.checks.length,maximumGeometryError,maximumPropertyError,errors,isolated:true,guardChecks:report.guardChecks,librarySha256:report.librarySha256,visibleRuntimeVerified:false,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));assert(audit.passed,'Hidden component source geometry differs.');
