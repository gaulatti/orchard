import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option = name => {const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]);};
const native = option('--native'), source = option('--source'), output = option('--output');
const report = JSON.parse(await fs.readFile(path.join(native, 'page-frame.json'), 'utf8'));
await fs.mkdir(output, {recursive: true}); const fixture = await fs.mkdtemp(path.join(output, 'fixture-'));
try {
  await fs.copyFile(path.join(native, 'result.json'), path.join(fixture, 'result.json'));
  for (const file of await fs.readdir(native)) if (file.endsWith('.png')) await fs.copyFile(path.join(native, file), path.join(fixture, file));
  const run = () => spawnSync(process.execPath, ['scripts/verify-page-frame.mjs', '--native', fixture, '--source', source, '--output', path.join(fixture, 'audit.json')], {encoding: 'utf8'});
  await fs.writeFile(path.join(fixture, 'page-frame.json'), JSON.stringify(report)); const baseline = run(); assert.equal(baseline.status, 0, baseline.stderr + baseline.stdout);
  const frame = (r, id) => r.frames.find(row => row.id === id).measurement;
  const mutations = [
    ['centering', r => frame(r, 'defaults').frame.x += .01],
    ['child-padding', r => frame(r, 'defaults').body.x += .01],
    ['viewport-basis', r => r.frames[0].viewport++],
    ['empty-child', r => frame(r, 'empty').body = structuredClone(frame(r, 'defaults').body)],
    ['maximum-override', r => frame(r, 'maximum-override').maximum = 1152],
    ['padding-override', r => frame(r, 'padding-override').padding[0]++],
    ['fractional-width', r => frame(r, 'fractional-host').frame.width += .01],
    ['main-landmark', r => frame(r, 'element-main').landmark = 'None'],
    ['region-name', r => frame(r, 'element-section').name = ''],
    ['added-tab-stop', r => frame(r, 'defaults').passive = false],
    ['child-replacement', r => frame(r, 'defaults').contentRetained = false],
    ['missing-state', r => r.frames.pop()],
    ['lost-binding', r => r.interactions[0].boundOverridesRetained = false],
    ['stale-viewport', r => r.interactions[0].liveViewport = false],
    ['clear-default', r => r.interactions[0].clearRestoresRecipe = false],
    ['style-overwritten', r => r.interactions[0].stylePrecedence = false],
    ['selector-replaced', r => r.interactions[0].templateSelectorRetained = false],
    ['remount-value', r => r.interactions[0].remount = false],
    ['duplicate-action', r => r.interactions[0].nativeActions = 3]
  ];
  const rejected = [];
  for (const [name, mutate] of mutations) {
    const altered = structuredClone(report); mutate(altered); await fs.writeFile(path.join(fixture, 'page-frame.json'), JSON.stringify(altered));
    assert.notEqual(run().status, 0, `Audit accepted ${name}`); rejected.push(name);
  }
  await fs.writeFile(path.join(fixture, 'page-frame.json'), JSON.stringify(report));
  await fs.copyFile(path.join(native, 'page-frame-1536-full-comfortable-comfortable-Light.png'), path.join(fixture, 'page-frame-1536-reading-comfortable-comfortable-Light.png'));
  assert.notEqual(run().status, 0, 'Audit accepted a swapped complete frame'); rejected.push('swapped-full-frame');
  await fs.writeFile(path.join(output, 'result.json'), JSON.stringify({passed: true, rejected}, null, 2) + '\n'); console.log(JSON.stringify({passed: true, rejected}));
} finally {
  assert.equal(path.dirname(fixture), output); assert(path.basename(fixture).startsWith('fixture-')); await fs.rm(fixture, {recursive: true, force: true});
}
