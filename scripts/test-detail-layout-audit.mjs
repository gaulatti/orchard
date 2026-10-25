import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option = name => {const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1]); return path.resolve(process.argv[at + 1]);};
const native = option('--native'), source = option('--source'), output = option('--output');
const report = JSON.parse(await fs.readFile(path.join(native, 'detail-layout.json'), 'utf8'));
const reference = JSON.parse(await fs.readFile(new URL('../tests/Reference/DetailLayout.json', import.meta.url), 'utf8'));
await fs.mkdir(output, {recursive: true}); const fixture = await fs.mkdtemp(path.join(output, 'fixture-'));
try {
  await fs.copyFile(path.join(native, 'result.json'), path.join(fixture, 'result.json'));
  for (const row of reference.cases.filter(row => row.file)) await fs.copyFile(path.join(native, row.file), path.join(fixture, row.file));
  const run = () => spawnSync(process.execPath, ['scripts/verify-detail-layout.mjs','--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')], {encoding:'utf8'});
  await fs.writeFile(path.join(fixture,'detail-layout.json'),JSON.stringify(report)); const baseline=run(); assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
  const frame = (r, predicate) => r.frames.find(row => predicate(reference.cases.find(ref => ref.id === row.id && ref.scheme === row.scheme))).measurement;
  const rail = r => frame(r, row => row.spec.rail === 'box' && row.spec.viewport === 1280 && !row.spec.scroll);
  const mutations = [
    ['main-width', r => rail(r).main.width += .01],
    ['rail-minimum', r => frame(r,row=>row.spec.viewport===1280&&row.spec.width===240&&row.spec.ratio==='primary'&&row.spec.rail==='box').rail.width--],
    ['rail-order', r => rail(r).rail.x++],
    ['fractional-width', r => frame(r,row=>row.spec.width===900.375).layout.width += .01],
    ['stacked-gap', r => frame(r,row=>row.spec.viewport===1023&&row.spec.rail==='box').rail.y++],
    ['sticky-top', r => frame(r,row=>row.spec.scroll&&row.spec.sticky&&row.measurement.scrollOffset===160&&row.spec.viewport===1280).rail.y++],
    ['container-clamp', r => frame(r,row=>row.spec.scroll&&row.spec.sticky&&row.measurement.scrollOffset===400&&row.spec.viewport===1280).rail.y++],
    ['scroll-offset', r => frame(r,row=>row.spec.scroll).scrollOffset++],
    ['absent-rail', r => frame(r,row=>row.spec.rail===null).rail={x:0,y:0,width:0,height:0}],
    ['rail-label', r => frame(r,row=>Boolean(row.spec.label)).label=''],
    ['rail-semantics', r => rail(r).complementary=false],
    ['tab-stop', r => rail(r).passive=false],
    ['content-identity', r => rail(r).contentRetained=false],
    ['rail-identity', r => rail(r).railRetained=false],
    ['missing-state', r => r.frames.pop()],
    ['stale-viewport', r => r.frames[0].viewport++],
    ['tab-order', r => r.interactions[0].sourceTabOrder=false],
    ['input-retention', r => r.interactions[0].inputsRetained=false],
    ['template-replacement', r => r.interactions[0].templateReplacement=false],
    ['selector-retention', r => r.interactions[0].selectorRetained=false],
    ['converted-template-model', r => r.interactions[0].originalTemplateValues=false],
    ['live-viewport', r => r.interactions[0].liveViewport=false],
    ['duplicate-action', r => r.interactions[0].nativeActions++],
    ['nearest-scroll-owner', r => r.scrollOwners[0].nearestViewer=false],
    ['scrollview-retarget', r => r.scrollOwners[0].modernRetarget=false],
    ['detached-owner', r => r.scrollOwners[0].oldOwnerDetached=false]
  ];
  const rejected=[];
  for(const [name,mutate] of mutations){const altered=structuredClone(report);mutate(altered);await fs.writeFile(path.join(fixture,'detail-layout.json'),JSON.stringify(altered));assert.notEqual(run().status,0,`Audit accepted ${name}`);rejected.push(name);}
  await fs.writeFile(path.join(fixture,'detail-layout.json'),JSON.stringify(report));
  const light=reference.cases.find(row=>row.file&&row.scheme==='Light'),dark=reference.cases.find(row=>row.file&&row.scheme==='Dark');
  await fs.copyFile(path.join(native,dark.file),path.join(fixture,light.file)); assert.notEqual(run().status,0,'Audit accepted a swapped full frame');rejected.push('swapped-full-frame');
  await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
} finally {
  assert.equal(path.dirname(fixture),output);assert(path.basename(fixture).startsWith('fixture-'));await fs.rm(fixture,{recursive:true,force:true});
}
