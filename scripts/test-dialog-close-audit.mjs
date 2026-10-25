import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const native = option('--native'), source = option('--source'), output = option('--output');
const report = JSON.parse(await fs.readFile(path.join(native, 'dialog-close.json'), 'utf8'));
await fs.mkdir(output, {recursive: true}); const fixture = await fs.mkdtemp(path.join(output, 'fixture-'));
try {
  await fs.copyFile(path.join(native, 'result.json'), path.join(fixture, 'result.json'));
  for (const file of ['dialog-close-desktop.json', 'dialog-close-held-keys.json', 'dialog-close-held-pointer.json']) await fs.copyFile(path.join(native, file), path.join(fixture, file));
  for (const file of await fs.readdir(native)) if (file.endsWith('.png')) await fs.copyFile(path.join(native, file), path.join(fixture, file));
  const run = () => spawnSync(process.execPath, ['scripts/verify-dialog-close-button.mjs', '--native', fixture, '--source', source, '--output', path.join(fixture, 'audit.json')], {encoding: 'utf8'});
  await fs.writeFile(path.join(fixture, 'dialog-close.json'), JSON.stringify(report)); const baseline = run(); assert.equal(baseline.status, 0, baseline.stderr + baseline.stdout);
  const mutations = [
    ['button-width', r => r.frames[0].measurement.box.width += .01],
    ['icon-size', r => r.frames[0].measurement.icon.width += 1],
    ['foreground', r => r.frames[0].measurement.foreground.r ^= 1],
    ['disabled-opacity', r => r.frames.find(row => row.id === 'disabled').measurement.opacity = .5],
    ['pressed-scale', r => r.frames.find(row => row.id === 'pressed').measurement.scale = 1],
    ['keyboard-hover-paint', r => r.frames.find(row => row.id === 'keyboard-pressed').measurement.foreground = structuredClone(r.frames.find(row => row.id === 'hover').measurement.foreground)],
    ['missing-shadow', r => r.frames.find(row => row.id === 'hover').measurement.shadow = null],
    ['missing-state', r => r.frames.pop()],
    ['consumer-dismissal', r => r.interactions[0].consumerContentRetained = false],
    ['stale-theme-color', r => r.interactions[0].livePaint.background.r ^= 1],
    ['leaked-shadow-graph', r => r.interactions[0].releasedShadowGraphs = false]
  ];
  const rejected = [];
  for (const [name, mutate] of mutations) {
    const altered = structuredClone(report); mutate(altered); await fs.writeFile(path.join(fixture, 'dialog-close.json'), JSON.stringify(altered));
    assert.notEqual(run().status, 0, `Audit accepted ${name}`); rejected.push(name);
  }
  await fs.writeFile(path.join(output, 'result.json'), JSON.stringify({passed: true, rejected}, null, 2) + '\n'); console.log(JSON.stringify({passed: true, rejected}));
} finally {
  assert.equal(path.dirname(fixture), output); assert(path.basename(fixture).startsWith('fixture-')); await fs.rm(fixture, {recursive: true, force: true});
}
