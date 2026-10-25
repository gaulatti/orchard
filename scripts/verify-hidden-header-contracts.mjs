import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1], name); return process.argv[at + 1]; };
const native = path.resolve(option('--native')), app = path.resolve(option('--app')), host = option('--host');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = async file => JSON.parse(await fs.readFile(path.join(native, file)));
const referencePath = path.resolve(option('--reference')), referenceBytes = await fs.readFile(referencePath), reference = JSON.parse(referenceBytes);
const architecture = process.argv.includes('--architecture') ? option('--architecture') : 'X64';
assert(['Orchard.Catalog', 'PackageConsumer'].includes(host)); assert(['X64', 'Arm64'].includes(architecture));
const report = await read('header-contracts.json'), exit = await read('process-exit.json');
assert.equal(report.passed, true); assert.equal(report.diagnosticOnly, true); assert.equal(report.scope, 'hidden-header-public-contracts'); assert.deepEqual(report.failures, []); assert.equal(exit.exited, true); assert.equal(exit.exitCode, 0);
for (const field of ['activateCalled', 'showCalled', 'focusCalled', 'inputSent', 'nativeInputFocusVerified', 'visibleRuntimeVerified', 'sourceRasterParityVerified', 'nativeComponentAccepted']) assert.equal(report[field], false, field);
assert.equal(report.actualNativeScrollVerified, true); assert.equal(report.processArchitecture, architecture); assert.equal(report.osArchitecture, architecture); assert.equal(report.activations, 0); assert.equal(report.focus, 0); assert(report.guardChecks >= 700);
const ids = ['caller-history-Light-False', 'caller-history-Light-True', 'caller-history-Dark-False', 'caller-history-Dark-True', 'model-template-selector-original-data', 'shared-renderer-original-item-command-and-brand', 'worker-command-notification-disabled-and-retired', 'template-replacement-preserves-editors-and-retires-links', 'bound-brand-and-explicit-null-logo', 'mobile-actions-use-source-conditional-truthiness', 'live-navigation-producer-and-replacement', 'actual-scroll-viewer-Fixed', 'actual-scroll-viewer-Sticky', 'actual-scroll-viewer-Static', 'actual-sticky-parent-bottom', 'actual-scroll-view-Fixed', 'actual-scroll-view-Sticky', 'actual-scroll-view-Static', 'actual-scroll-view-sticky-parent-bottom', 'reparent-scroll-owner-and-live-position', 'app-shell-header-composition-Light', 'app-shell-header-composition-Dark'];
ids.push('caller-paint-identity-live-alpha-and-retired-producers');
assert.deepEqual(report.checks.map(row => row.id), ids); assert(report.checks.every(row => row.passed === true));
const librarySha256 = hash(await fs.readFile(path.join(app, 'Orchard.dll'))), hostSha256 = hash(await fs.readFile(path.join(app, host + '.dll')));
assert.equal(report.librarySha256, librarySha256); assert.equal(report.hostSha256, hostSha256);
for (const [file, expected] of [['Orchard.dll', librarySha256], [host + '.dll', hostSha256]]) assert.equal(hash(await fs.readFile(path.join(native, 'app-identity', file))), expected);
let packageSha256;
if (host === 'PackageConsumer') {
  packageSha256 = hash(await fs.readFile(path.resolve(option('--package')))); const build = await read('result.json'), cleanup = await read('cleanup.json');
  assert.equal(build.buildPassed, true); assert.equal(build.runtimeVerified, false); assert.equal(build.platform.toLowerCase(), architecture.toLowerCase()); assert.equal(build.packageSha256, packageSha256); assert.equal(cleanup.removed, true); assert.equal(build.isolatedConsumer, cleanup.isolatedConsumer); await assert.rejects(fs.access(cleanup.isolatedConsumer), {code: 'ENOENT'});
}
const operations = [['scroll-view-fixed', 140], ['scroll-view-fixed-return', 0], ['scroll-view-sticky', 140], ['scroll-view-sticky-return', 0], ['scroll-view-static', 140], ['scroll-view-static-return', 0], ['scroll-view-sticky-parent-end', 500], ['scroll-view-reparented', 150]];
assert.deepEqual(report.scrollOperations.map(row => row.id), operations.map(row => row[0]));
for (const [index, row] of report.scrollOperations.entries()) { assert(Number.isInteger(row.correlation) && row.correlation >= 0); assert.equal(row.completed, true); assert.equal(row.requestedOffset, operations[index][1]); assert.equal(row.actualOffset, row.requestedOffset); assert.equal(row.ExtentHeight, 2400); assert.equal(row.ViewportHeight, 900); }
assert.equal(report.compositionReferenceSha256, hash(referenceBytes)); assert.equal(reference.format, 'orchard-header-composition-reference-v1'); assert.equal(reference.commit, 'a86059013cd59684ce452849278ea81e734f2066'); assert.equal(reference.cases.length, 6);
assert.deepEqual(report.compositionEvidence.map(row => row.scheme + '/' + row.id), reference.cases.map(row => row.scheme + '/' + row.id));
let compositionGeometryChecks = 0;
for (const [index, row] of report.compositionEvidence.entries()) {
  const source = reference.cases[index]; assert.equal(row.actualTheme, row.scheme); assert.equal(row.menuOpen, source.spec.open); assert.equal(row.flowHeight, 0); assert.equal(row.originalBody, true);
  for (const field of ['text', 'selectionStart', 'selectionLength']) assert.equal(row[field], source.measurement[field]);
  assert.deepEqual(Object.keys(row.measurement), ['header', 'main', 'content', 'body', 'footer']);
  for (const node of Object.keys(row.measurement)) for (const field of ['x', 'y', 'width', 'height']) { const actual = row.measurement[node][field]; assert.equal(typeof actual, 'number'); assert(Number.isFinite(actual)); assert(Math.abs(actual - source.measurement[node][field]) <= .000001, row.scheme + '/' + row.id + '/' + node + '/' + field); compositionGeometryChecks++; }
}
const sourceById = new Map();
for (const scheme of ['Light', 'Dark']) { const rows = reference.cases.filter(row => row.scheme === scheme); for (const row of rows) sourceById.set('app-shell-' + scheme.toLowerCase() + '-' + row.id, row); sourceById.set('app-shell-' + scheme.toLowerCase() + '-restored', rows.find(row => row.id === 'wide')); }
const scrollIds = ['fixed', 'sticky', 'static', 'sticky-parent-end', 'scroll-view-fixed', 'scroll-view-sticky', 'scroll-view-static', 'scroll-view-sticky-parent-end', 'scroll-view-reparented', ...sourceById.keys()];
assert.deepEqual(report.scrollEvidence.map(row => row.id), scrollIds);
let pngs = 0, maximumCompositionProbeError = 0; const compositionProbeDifferences = [];
for (const row of report.scrollEvidence) {
  assert.equal(row.file, row.id + '.png'); const bytes = await fs.readFile(path.join(native, row.file)); assert.equal(row.pngSha256, hash(bytes)); const image = readRgbaPng(bytes);
  const source = sourceById.get(row.id), width = source ? source.spec.width : 1280, point = source ? source.measurement.point : {x: 1100, y: 30};
  assert.equal(image.width, width); assert.equal(image.height, 900); assert.equal(row.pixelWidth, width); assert.equal(row.pixelHeight, 900); assert.equal(row.probeX, point.x); assert.equal(row.probeY, point.y);
  assert(Array.isArray(row.renderFrames) && row.renderFrames.length >= 2 && row.renderFrames.length <= 6);
  for (const [index, frame] of row.renderFrames.entries()) {
    assert.equal(frame.file, row.id + '-render-' + index + '.png'); const frameBytes = await fs.readFile(path.join(native, frame.file)); assert.equal(hash(frameBytes), frame.pngSha256);
    const captured = readRgbaPng(frameBytes); assert.equal(captured.width, frame.pixelWidth); assert.equal(captured.height, frame.pixelHeight);
    assert.equal(frame.requestedWidth, source ? 0 : width); assert.equal(frame.requestedHeight, source ? 0 : 900);
    assert.equal(frame.actualRootWidth, width); assert.equal(frame.actualRootHeight, 900); assert.equal(frame.rasterizationScale, 1);
    assert.equal(frame.captureTarget, source ? 'BleeckerAppShell' : 'Canvas'); assert.equal(frame.captureWidth, width); assert.equal(frame.captureHeight, 900); assert.equal(frame.captureX, 0); assert.equal(frame.captureY, 0);
    assert(Number.isFinite(frame.measuredMinX) && Number.isFinite(frame.measuredMinY)); assert(frame.measuredMinX <= 0 && frame.measuredMinY <= 0);
    assert.equal(frame.cropX, Math.abs(Math.floor(frame.measuredMinX))); assert.equal(frame.cropY, Math.abs(Math.floor(frame.measuredMinY))); assert.equal(frame.viewportWidth, width); assert.equal(frame.viewportHeight, 900);
    assert(captured.width >= frame.cropX + width && captured.height >= frame.cropY + 900);
    if (source && frame.cropX > 0) assert.equal(captured.rgba[((500 + frame.cropY) * captured.width + frame.cropX - 1) * 4 + 3], 0, 'Crop starts inside painted client');
    if (index >= row.renderFrames.length - 2) {
      assert.equal(frame.viewportSha256, row.pngSha256);
      for (let y = 0; y < 900; y++) {
        const from = ((y + frame.cropY) * captured.width + frame.cropX) * 4, to = y * width * 4;
        assert.equal(Buffer.compare(captured.rgba.subarray(from, from + width * 4), image.rgba.subarray(to, to + width * 4)), 0, 'Viewport pixels were changed or rescaled');
      }
    }
  }
  const present = !row.id.endsWith('sticky-parent-end'); assert.equal(row.present, present); assert.equal(row.hit, present); assert.equal(row.ownsTopHit, present);
  const at = (point.y * width + point.x) * 4, rgba = [...image.rgba.subarray(at, at + 4)]; assert.equal(row.alpha, rgba[3]); assert.equal(rgba[3] > 0, present); assert.equal(row.rgba.length, 4); assert.equal(row.rgba[3], rgba[3]);
  for (let channel = 0; channel < 3; channel++) assert(Math.abs(row.rgba[channel] - Math.round(rgba[channel] * rgba[3] / 255)) <= 1, row.id + ' premultiplied channel differs');
  if (source) {
    const sourceBytes = await fs.readFile(path.join(path.dirname(referencePath), 'HeaderComposition', source.file)); assert.equal(hash(sourceBytes), source.pngSha256); const sourceImage = readRgbaPng(sourceBytes); assert.equal(sourceImage.width, width); assert.equal(sourceImage.height, 900);
    for (let alpha = 3; alpha < image.rgba.length; alpha += 4) { assert.equal(image.rgba[alpha], 255, row.id + ' transparent client padding'); assert.equal(sourceImage.rgba[alpha], 255); }
    for (const sample of [point, {x: Math.min(width - 80, 400), y: 72}, {x: width - 80, y: 500}]) {
      const offset = (sample.y * width + sample.x) * 4;
      const actual = [...image.rgba.subarray(offset, offset + 4)], expected = [...sourceImage.rgba.subarray(offset, offset + 4)], difference = Math.max(...actual.map((value, channel) => Math.abs(value - expected[channel])));
      maximumCompositionProbeError = Math.max(maximumCompositionProbeError, difference); if (difference > 1) compositionProbeDifferences.push({id: row.id, point: sample, actual, expected, difference});
    }
  }
  pngs++;
}
const sourcePaintProbeGatePassed = maximumCompositionProbeError <= 1;
const audit = {passed: sourcePaintProbeGatePassed, structuralContractChecksPassed: true, sourcePaintProbeGatePassed, scope: 'hidden-header-public-contracts', checks: ids.length, scrollPresentationPngs: pngs, guardChecks: report.guardChecks, librarySha256, hostSha256, packageSha256, actualNativeScrollViewerVerified: true, actualNativeScrollViewVerified: true, completedScrollViewOperations: operations.length, compositionReferenceSha256: hash(referenceBytes), compositionStates: reference.cases.length, compositionGeometryChecks, maximumCompositionGeometryError: 0, compositionPaintSamples: sourceById.size * 3, completeClientAlphaVerified: true, maximumCompositionProbeError, compositionProbeDifferences, topmostHeaderHitsVerified: true, stableNativeFramesVerified: true, exactViewportExtractionVerified: true, nativeInputFocusVerified: false, visibleRuntimeVerified: false, sourceRasterParityVerified: false, nativeComponentAccepted: false};
await fs.writeFile(path.resolve(option('--output')), JSON.stringify(audit, null, 2) + '\n'); console.log(JSON.stringify(audit));
assert(sourcePaintProbeGatePassed, 'Header/AppShell source probe paint differs: ' + maximumCompositionProbeError);
