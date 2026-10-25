import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {verifySwitchLifecycleModel} from './verify-switch-lifecycle.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
export async function auditSwitchJapaneseFallback(folder, app, baseline, hostAssembly = 'PackageConsumer.dll') {
  assert(['PackageConsumer.dll', 'Orchard.Catalog.dll'].includes(hostAssembly));
  const bytes = await fs.readFile(path.join(folder, 'switch-lifecycle.json'));
  const report = JSON.parse(bytes); verifySwitchLifecycleModel(report);
  assert((await read(path.join(folder, 'audit.json'))).passed);
  const exit = await read(path.join(folder, 'process-exit.json'));
  assert.equal(exit.exited, true); assert.equal(exit.exitCode, 0); assert.equal(exit.observationExpired, false);
  for (const [file, sha] of [['Orchard.dll', report.librarySha256], [hostAssembly, report.hostSha256]]) {
    assert.equal(hash(await fs.readFile(path.join(folder, 'app-identity', file))), sha);
    assert.equal(hash(await fs.readFile(path.join(app, file))), sha);
  }
  const mediumSha = '40a222af6b6dcd6940866415df454f2e27e798f5dc16539d72c67449c0eeca98';
  assert.equal(hash(await fs.readFile(path.join(app, 'Orchard/Assets/Fonts/EncodeSans-Medium.ttf'))), mediumSha);
  const referenceBytes = await fs.readFile('tests/Reference/SwitchFonts.json');
  const reference = JSON.parse(referenceBytes);
  assert.equal(reference.canonical.japanese, 111.453125);
  assert.equal(reference.canonical.latin, 125.296875);
  const baselineBytes = await fs.readFile(path.join(baseline, 'switch-lifecycle.json'));
  assert.equal(hash(baselineBytes), 'a4130371d64b442319cae57929aed784c33fae6c395405ba4744846aeaf507bd');
  const original = JSON.parse(baselineBytes); verifySwitchLifecycleModel(original);
  assert.equal(original.librarySha256, '274f751ba677d65125465bb3c2d3c205242792b1c7cce35ff284dc7ce9d4147a');
  const stages = ['japanese', 'latin', 'japanese-rebound', 'caller-font', 'font-restored', 'reloaded', 'template-restored'];
  assert.deepEqual(report.fontObservations.map(row => row.id), ['Light', 'Dark'].flatMap(theme => stages.map(stage => `${theme}/${stage}`)));
  const failures = [];
  const near = (actual, expected, id, tolerance = 1 / 64 + .0001) => {
    assert(Number.isFinite(actual));
    if (Math.abs(actual - expected) > tolerance) failures.push({id, actual, expected});
  };
  const families = ['Microsoft YaHei', 'Yu Gothic'];
  const expectedRuns = [{text: '通知', family: families[0]}, {text: 'を', family: families[1]}, {text: '有効', family: families[0]}, {text: 'にする', family: families[1]}];
  for (const row of report.fontObservations) {
    const stage = row.id.split('/')[1], latin = stage === 'latin', custom = stage === 'caller-font';
    assert.equal(row.labelBindingRetained, true); assert.equal(row.fontBindingRetained, true); assert.equal(row.toggles, 0);
    assert.equal(row.label, latin ? 'Enable notifications' : '通知を有効にする');
    assert.equal(row.accessibleName, row.label); assert.equal(row.labelWeight, 500);
    assert.equal(row.unloadedEditObserved, stage === 'reloaded');
    near(row.labelHeight, 20, row.id + '/label-height');
    near(row.ownerHeight, 20, row.id + '/owner-height');
    if (custom) {
      assert.equal(row.ownerFamily, 'Segoe UI'); assert.equal(row.labelFamily, 'Segoe UI');
      if (row.runs.some(run => run.explicitFamily && families.includes(run.family))) failures.push({id: row.id + '/caller-font-overridden'});
      continue;
    }
    assert(row.ownerFamily.includes('/EncodeSans-Medium.ttf#Encode Sans'), 'Measure the mounted default style, not a pre-style font');
    assert.equal(row.labelFamily, row.ownerFamily);
    if (latin) {
      const previous = original.fontObservations.find(item => item.id === row.id);
      near(row.labelWidth, previous.labelWidth, row.id + '/unchanged-latin-label-width', .0001);
      near(row.ownerWidth, previous.ownerWidth, row.id + '/unchanged-latin-owner-width', .0001);
    } else {
      near(row.labelWidth, reference.canonical.japanese, row.id + '/label-width');
      near(row.ownerWidth, reference.canonical.japanese + 48, row.id + '/owner-width');
    }
    if (!latin && JSON.stringify(row.runs.map(({text, family, explicitFamily}) => ({text, family, explicitFamily}))) !== JSON.stringify(expectedRuns.map(run => ({...run, explicitFamily: true})))) failures.push({id: row.id + '/script-faces', actual: row.runs});
    if (latin && row.runs.some(run => run.explicitFamily && families.includes(run.family))) failures.push({id: row.id + '/stale-script-faces'});
  }
  assert.equal(report.fontImages.length, 2);
  const renderedLabels = [];
  for (const capture of report.fontImages) {
    const image = await fs.readFile(path.join(folder, capture.file)); assert.equal(hash(image), capture.sha256);
    const png = readRgbaPng(image); assert.deepEqual([png.width, png.height], [capture.width, capture.height]);
    assert(png.rgba.some(value => value !== 0));
    const theme = capture.file.split('-')[0], observation = report.fontObservations.find(row => row.id === `${theme}/japanese`);
    assert(observation); assert.equal(png.width, Math.ceil(observation.ownerWidth)); assert.equal(png.height, Math.ceil(observation.ownerHeight));
    const background = Array.from(png.rgba.subarray((Math.floor(png.height / 2) * png.width + 47) * 4, (Math.floor(png.height / 2) * png.width + 47) * 4 + 4));
    let inkPixels = 0;
    for (let y = 0; y < png.height; y++) for (let x = 48; x < png.width; x++) if ([0, 1, 2, 3].some(channel => png.rgba[(y * png.width + x) * 4 + channel] !== background[channel])) inkPixels++;
    assert(inkPixels > 8, 'The resized label must contain visible glyph paint');
    renderedLabels.push({file: capture.file, width: png.width, height: png.height, inkPixels});
  }
  const latinSourceDifferences = report.fontObservations.filter(row => row.id.endsWith('/latin')).map(row => ({id: row.id, nativeWidth: row.labelWidth, sourceWidth: reference.canonical.latin, difference: row.labelWidth - reference.canonical.latin}));
  return {passed: failures.length === 0, captureValid: true, observations: 14, lifecycleChecks: report.checks.length, failures, renderedLabels, latinSourceDiagnosticPassed: latinSourceDifferences.every(row => Math.abs(row.difference) <= 1 / 64 + .0001), latinSourceDifferences, originalLatinGeometryPreserved: !failures.some(row => row.id.includes('unchanged-latin')), fontSha256: mediumSha, referenceSha256: hash(referenceBytes), baselineSha256: hash(baselineBytes), reportSha256: hash(bytes), librarySha256: report.librarySha256, hostSha256: report.hostSha256, nativeComponentAccepted: false};
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const option = key => { const at = process.argv.indexOf(key); assert(at >= 0 && process.argv[at + 1]); return process.argv[at + 1]; };
  const hostAt = process.argv.indexOf('--host-assembly');
  const audit = await auditSwitchJapaneseFallback(option('--native'), option('--app'), option('--baseline'), hostAt < 0 ? undefined : process.argv[hostAt + 1]);
  await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n');
  console.log(JSON.stringify({...audit, failures: audit.failures.length}));
  process.exitCode = audit.passed ? 0 : 1;
}
