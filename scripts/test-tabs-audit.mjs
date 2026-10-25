import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const native = option('--native'), source = option('--source'), output = option('--output');
const report = JSON.parse(await fs.readFile(path.join(native, 'tabs-reference.json'), 'utf8'));
await fs.mkdir(output, {recursive: true});
const fixture = await fs.mkdtemp(path.join(output, 'fixture-'));
try {
  await fs.copyFile(path.join(native, 'result.json'), path.join(fixture, 'result.json'));
  for (const file of await fs.readdir(native)) if (file.endsWith('.png')) await fs.copyFile(path.join(native, file), path.join(fixture, file));
  const run = () => spawnSync(process.execPath, ['scripts/verify-tabs.mjs', '--native', fixture, '--source', source, '--output', path.join(fixture, 'audit.json')], {encoding: 'utf8'});
  await fs.writeFile(path.join(fixture, 'tabs-reference.json'), JSON.stringify(report));
  const baseline = run(); assert.equal(baseline.status, 0, baseline.stderr + baseline.stdout);
  const mutations = [
    ['root-width', r => r.frames[0].measurement.root.box.width += .01],
    ['item-width', r => r.frames[0].measurement.items[0].box.width += .01],
    ['border-thickness', r => r.frames[0].measurement.items[0].borderThickness.Bottom += 1],
    ['padding', r => r.frames[0].measurement.items[0].padding.Left += 1],
    ['foreground', r => r.frames[0].measurement.items[0].foreground.r ^= 1],
    ['font-weight', r => r.frames[0].measurement.items[0].Weight = 400],
    ['caption', r => r.frames[0].measurement.items[0].text = 'Changed'],
    ['missing-state', r => r.frames.pop()],
    ['duplicate-state', r => r.frames[1] = structuredClone(r.frames[0])]
  ];
  const rejected = [];
  for (const [name, mutate] of mutations) {
    const altered = structuredClone(report); mutate(altered);
    await fs.writeFile(path.join(fixture, 'tabs-reference.json'), JSON.stringify(altered));
    assert.notEqual(run().status, 0, `Audit accepted ${name}`); rejected.push(name);
  }
  await fs.writeFile(path.join(output, 'result.json'), JSON.stringify({passed: true, rejected, fixtureRemoved: true}, null, 2) + '\n');
  console.log(JSON.stringify({passed: true, rejected}));
} finally {
  assert.equal(path.dirname(fixture), output);
  assert(path.basename(fixture).startsWith('fixture-'));
  await fs.rm(fixture, {recursive: true, force: true});
}
