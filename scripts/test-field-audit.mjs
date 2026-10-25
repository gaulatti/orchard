import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]); };
const native = option('--native'), source = option('--source'), output = option('--output');
const report = JSON.parse(await fs.readFile(path.join(native, 'field.json'), 'utf8'));
await fs.mkdir(output, {recursive: true}); const fixture = await fs.mkdtemp(path.join(output, 'fixture-'));
try {
  await fs.copyFile(path.join(native, 'result.json'), path.join(fixture, 'result.json'));
  for (const file of await fs.readdir(native)) if (file.endsWith('.png')) await fs.copyFile(path.join(native, file), path.join(fixture, file));
  const run = () => spawnSync(process.execPath, ['scripts/verify-field.mjs', '--native', fixture, '--source', source, '--output', path.join(fixture, 'audit.json')], {encoding: 'utf8'});
  await fs.writeFile(path.join(fixture, 'field.json'), JSON.stringify(report)); const baseline = run(); assert.equal(baseline.status, 0, baseline.stderr + baseline.stdout);
  const frame = (r, id) => r.frames.find(row => row.id === id).measurement;
  const mutations = [
    ['flow-gap', r => frame(r, 'normal').control.box.y += .01],
    ['rich-label-width', r => frame(r, 'rich-label').label.box.width += .01],
    ['required-marker-position', r => frame(r, 'required').required.fontBox.x += .01],
    ['description-paint', r => frame(r, 'description').description.foreground.r ^= 1],
    ['description-order', r => frame(r, 'existing-metadata').describedBy.reverse()],
    ['external-uia-description-order', r => frame(r, 'existing-metadata').uia.describedBy.reverse()],
    ['required-semantic-leak', r => frame(r, 'required').nativeRequired = true],
    ['error-validity', r => frame(r, 'error').uia.valid = true],
    ['explicit-empty-id', r => frame(r, 'empty-id').controlId = 'generated-id'],
    ['child-value', r => frame(r, 'normal').value = 'Overwritten'],
    ['action-content', r => frame(r, 'action').right.text = 'Optional'],
    ['optional-paint', r => frame(r, 'optional').right.foreground.r ^= 1],
    ['marker-in-label-name', r => frame(r, 'required').uia.name += '*'],
    ['missing-state', r => r.frames.pop()],
    ['lost-binding', r => r.interactions[0].boundMetadataRestored = false],
    ['scale-preference-leak', r => r.interactions[0].ownedTextScaleRestored = false],
    ['live-child-replacement', r => r.interactions[0].added.runtimeId[3]++],
    ['action-double-dispatch', r => r.interactions[0].actions = 2],
    ['error-removal-retains-invalid', r => r.interactions[0].removedError.valid = false]
  ];
  const rejected = [];
  for (const [name, mutate] of mutations) {
    const altered = structuredClone(report); mutate(altered); await fs.writeFile(path.join(fixture, 'field.json'), JSON.stringify(altered));
    assert.notEqual(run().status, 0, `Audit accepted ${name}`); rejected.push(name);
  }
  await fs.writeFile(path.join(output, 'result.json'), JSON.stringify({passed: true, rejected}, null, 2) + '\n'); console.log(JSON.stringify({passed: true, rejected}));
} finally {
  assert.equal(path.dirname(fixture), output); assert(path.basename(fixture).startsWith('fixture-')); await fs.rm(fixture, {recursive: true, force: true});
}
