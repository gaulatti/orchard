import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

const digest = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const ids = ['dropdown-basic','dropdown-label','dropdown-inset','dropdown-rich-shortcut','dropdown-selection-false','dropdown-selection-true','dropdown-selection-mixed','dropdown-submenu','dropdown-edge','dropdown-unicode','dropdown-empty','context-basic','context-inset','context-edge','context-unicode','context-empty'];

// This checks frozen browser evidence. It makes no claim about native parity.
export function verifyMenuModel(reference) {
  assert.equal(reference.format, 'orchard-menu-reference-v1');
  assert.equal(reference.commit, 'a86059013cd59684ce452849278ea81e734f2066');
  assert.equal(reference.dependencies.find(d => d.path === 'src/components/dropdown-menu.tsx')?.sha256, '36b67f45da3d0981fad9273bf263d118bbdd515f34515923158a935cedb42aa0');
  assert.equal(reference.dependencies.find(d => d.path === 'src/components/context-menu.tsx')?.sha256, '303f20b6e203e09bba9cc46f7b226e1b74d22ad1c2ab1dd650b0f425693ee562');
  assert.equal(reference.cases.length, 32);
  assert.equal(reference.histories.length, 12);
  assert.equal(new Set(reference.cases.map(c => `${c.scheme}:${c.id}`)).size, 32);
  const hash = value => assert.match(value, /^[a-f0-9]{64}$/);
  for (const input of [...reference.dependencies, ...reference.modules.map(m => ({sha256:m.packageSha256})), ...reference.fonts, ...reference.bundledFiles]) hash(input.sha256);
  assert.deepEqual(reference.fonts.map(f => f.weight), [400,500,600]);
  for (const scheme of ['Light','Dark']) {
    const cases = reference.cases.filter(c => c.scheme === scheme);
    assert.deepEqual(cases.map(c => c.id), ids);
    for (const c of cases) {
      assert.equal(path.basename(c.file), c.file);
      hash(c.pngSha256);
      assert.equal(c.spec.id, c.id);
      assert.equal(c.measurement.menus.length, c.spec.submenu ? 2 : 1);
      for (const menu of c.measurement.menus) {
        for (const value of Object.values(menu.box)) assert(Number.isFinite(value));
        assert(menu.box.width > 0 && menu.box.height > 0);
        assert(menu.box.x >= 0 && menu.box.y >= 0 && menu.box.x + menu.box.width <= 900 && menu.box.y + menu.box.height <= 760);
        assert.deepEqual(menu.paint.padding, [6,6,6,6]);
        assert.equal(menu.paint.radius, '7px');
      }
      const menu = c.measurement.menus[0];
      if (c.spec.empty) { assert.equal(menu.items.length, 0); assert.equal(menu.box.height, 14); }
      const disabled = menu.items.find(i => i.id === 'item-disabled');
      if (disabled) { assert(disabled.disabled); assert.equal(disabled.paint.opacity, c.spec.kind === 'context' ? 0.4 : 0.5); }
      if (c.id === 'dropdown-selection-mixed') assert.equal(menu.items.find(i => i.id === 'check-item').checked, 'mixed');
    }
    const basic = cases.find(c => c.id === 'dropdown-basic').measurement.menus[0];
    assert.equal(basic.paint.background, scheme === 'Light' ? 'rgb(255, 255, 255)' : 'rgb(24, 37, 51)');
    assert.deepEqual(basic.box, {x:20,y:60,width:176,height:110});
    assert(basic.items.every(i => i.box.height === 32 && i.paint.fontSize === '14px' && i.paint.lineHeight === '20px'));
    const context = cases.find(c => c.id === 'context-basic').measurement.menus[0];
    assert.equal(context.paint.background, basic.paint.background);
    assert.deepEqual(context.box, {x:103,y:56,width:160,height:122});
    assert(context.items.every(i => i.box.height === 36 && i.paint.fontSize === '13px' && i.paint.lineHeight === '19.5px' && i.paint.radius === '5px'));
    for (const accept of [false,true]) for (const action of ['check','radio','sub-action']) {
      const matches = reference.histories.filter(h => h.scheme === scheme && h.accept === accept && h.action === action);
      assert.equal(matches.length, 1);
      const h = matches[0];
      assert.equal(h.retainedReactInstance, true);
      assert(h.before.menus.length > 0);
      assert.equal(h.after.menus.length, 0);
      assert.equal(h.after.focused, 'menu-trigger');
      assert.deepEqual(h.after.events, action === 'check' ? [{key:'checked',value:true}] : action === 'radio' ? [{key:'radio',value:'b'}] : ['sub-action']);
      assert.equal(h.reopened.menus.length, 1);
      assert.deepEqual(h.reopened.events, h.after.events);
      const items = h.reopened.menus[0].items;
      assert.equal(items.find(i => i.id === 'check-item').checked, String(accept && action === 'check'));
      assert.equal(items.find(i => i.id === 'radio-b').checked, String(accept && action === 'radio'));
    }
  }
}

export async function verifyMenuSource(referenceFile, frames, recapture) {
  const reference = JSON.parse(await fs.readFile(referenceFile));
  verifyMenuModel(reference);
  if (recapture) assert.deepEqual(JSON.parse(await fs.readFile(path.join(recapture,'Menu.json'))), reference);
  let pixels = 0;
  for (const c of reference.cases) {
    const bytes = await fs.readFile(path.join(frames, c.file));
    assert.equal(digest(bytes), c.pngSha256, c.file);
    const png = readRgbaPng(bytes);
    assert.equal(png.width, 900); assert.equal(png.height, 760);
    pixels += png.width * png.height;
    if (recapture) assert.deepEqual(await fs.readFile(path.join(recapture,c.file)), bytes, c.file);
  }
  return {format:'orchard-menu-source-audit-v1',passed:true,sourceCases:32,retainedInstanceHistories:12,pngs:32,pixelsValidated:pixels,independentRecaptureByteIdentical:!!recapture,sourceReferenceSha256:digest(await fs.readFile(referenceFile)),nativeMenuAdapterImplemented:false,nativeComponentAccepted:false};
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const option = name => { const i = process.argv.indexOf(name); return i < 0 ? null : process.argv[i+1]; };
  const reference = option('--reference'), frames = option('--frames'), output = option('--output');
  assert(reference && frames && output, 'Use --reference, --frames and --output, with optional --recapture.');
  const audit = await verifyMenuSource(reference, frames, option('--recapture'));
  await fs.mkdir(path.dirname(path.resolve(output)), {recursive:true});
  await fs.writeFile(output, JSON.stringify(audit,null,2)+'\n');
  console.log(JSON.stringify(audit));
}
