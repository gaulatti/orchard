import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => {const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]);};
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const native = option('--native'), source = option('--source');
const reference = await read(new URL('../tests/Reference/DetailLayout.json', import.meta.url));
const result = await read(path.join(native, 'result.json')), report = await read(path.join(native, 'detail-layout.json'));
assert(result.passed && report.passed && result.component === 'detail-layout');
assert.equal(result.environment.displayScale, 1); assert.equal(result.environment.textScale, 1);
const key = row => row.id + '/' + row.scheme;
assert.equal(reference.cases.length, 888); assert.deepEqual(report.frames.map(key).sort(), reference.cases.map(key).sort());
let maximumGeometryError = 0; const errors = [], raster = [];
function near(actual, expected, context) {
  assert(Number.isFinite(actual) && Number.isFinite(expected), context);
  const error = Math.abs(actual - expected); maximumGeometryError = Math.max(maximumGeometryError, error);
  if (error >= .001) errors.push({context, actual, expected, error});
}
for (const row of reference.cases) {
  const frame = report.frames.find(frame => key(frame) === key(row)), actual = frame.measurement, expected = row.measurement, context = key(row);
  near(frame.viewport, row.spec.viewport, context + '/viewport'); near(actual.scrollOffset, expected.scrollOffset, context + '/scroll-offset');
  for (const subject of ['layout','main','rail','mainBody','railBody','scrollViewport']) {
    assert.equal(actual[subject] === null, expected[subject] === null, context + '/' + subject + '/presence');
    if (!expected[subject]) continue;
    for (const field of ['x','y','width','height']) near(actual[subject][field], expected[subject][field], context + '/' + subject + '/' + field);
  }
  assert.equal(actual.passive, true); assert.equal(actual.group, true); assert.equal(actual.contentRetained, true); assert.equal(actual.railRetained, true);
  if (expected.rail) {assert.equal(actual.complementary, true); assert.equal(actual.label, expected.label ?? '');}
}
assert.equal(report.interactions.length,2);
for(const theme of ['Light','Dark']){
  const row=report.interactions.find(row=>row.theme===theme);assert(row);
  for(const flag of ['passed','inputsRetained','sourceTabOrder','railRemount','templateReplacement','remount','liveViewport','labelUpdate','templateRetained','selectorRetained','originalTemplateValues','invalidRejected'])assert.equal(row[flag],true,theme+'/'+flag);
  assert.equal(row.nativeActions,2);assert.notEqual(row.mainValue,'Main value');assert.notEqual(row.railValue,'Rail value');
}
assert.equal(report.scrollOwners.length, 2);
for (const theme of ['Light', 'Dark']) {
  const row = report.scrollOwners.find(row => row.theme === theme); assert(row);
  for (const flag of ['nearestViewer','outerScroll','stickyToggle','modernRetarget','oldOwnerDetached','containerClamp','contentRetained']) assert.equal(row[flag], true, theme + '/' + flag);
}
if (errors.length) {
  await fs.writeFile(option('--output'), JSON.stringify({passed:false,frames:report.frames.length,maximumGeometryError,errors},null,2)+'\n');
  assert.fail('DetailLayout geometry differs.');
}
for (const row of reference.cases.filter(row => row.file)) {
  const bytes = await fs.readFile(path.join(source,row.file)); assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),row.pngSha256);
  const wanted = readRgbaPng(bytes), rendered = readRgbaPng(await fs.readFile(path.join(native,row.file)));
  assert.deepEqual([rendered.width,rendered.height],[wanted.width,wanted.height]);
  let differingPixels=0,absoluteRgbaError=0;
  for(let at=0;at<wanted.rgba.length;at+=4){let differs=false;for(let channel=0;channel<4;channel++){const difference=Math.abs(wanted.rgba[at+channel]-rendered.rgba[at+channel]);differs||=difference>0;absoluteRgbaError+=difference;}differingPixels+=Number(differs);}
  raster.push({file:row.file,differingPixels,absoluteRgbaError});
}
const audit={passed:errors.length===0&&raster.every(row=>row.differingPixels===0),frames:report.frames.length,maximumGeometryError,errors,inputThemes:2,raster,masks:0,resampling:false,pixelTolerance:0,
  sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),nativeArm64RuntimeVerified:false,actualScalingVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined}));assert(audit.passed,'DetailLayout reference differs.');
