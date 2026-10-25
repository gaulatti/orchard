import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';

export function verifyPageFrameOverrides(report, identity) {
  assert.equal(report.format, 'orchard-page-frame-overrides-v1');
  assert.equal(report.passed, true);
  assert.equal(report.librarySha256, identity.librarySha256);
  assert.equal(report.hostSha256, identity.hostSha256);
  assert.equal(report.focus, 0); assert.equal(report.activations, 0); assert(report.guards > 80);
  for (const flag of ['systemTextScalingVerified', 'displayScalingVerified', 'physicalInputVerified', 'nativeArm64RuntimeVerified',
    'nativeComponentAccepted', 'focusCalled', 'showWindowCalled', 'hardwareInputSent', 'osSettingsChanged']) assert.equal(report[flag], false, flag);
  assert.equal(report.componentFontOverridesVerified, true);
  const ids = [];
  function check(id) { ids.push(id); const row = report.checks.find(x => x.id === id); assert(row?.passed, id); return row.observed; }
  for (const theme of ['Light', 'Dark']) for (const flow of ['LeftToRight', 'RightToLeft']) for (const width of [280, 900]) {
    const prefix = `${theme}/${flow}/${width}`; let previousHeight = 0;
    for (const size of [16, 24, 32]) {
      const x = check(`${prefix}/font-${size}`);
      assert.equal(x.requestedFont, size); assert.equal(x.renderedFont, size); assert.equal(x.contentRetained, true);
      assert(Math.abs(x.frameWidth - width) < .01); assert(x.textWidth > 0 && x.textWidth <= width - 24 + .01);
      assert(x.textHeight > 0 && x.frameHeight >= x.textHeight + 24 - .01 && x.frameHeight >= previousHeight);
      previousHeight = x.frameHeight;
    }
    const local = check(prefix + '/child-font-override'); assert.equal(local.frameFont, 40); assert.equal(local.childFont, 11);
    assert.equal(check(prefix + '/restore-inherited-font').childFont, 40);
    const wide = check(prefix + '/oversized-content'); assert.equal(wide.childWidth, 1200); assert.equal(wide.width, Math.min(width, 768)); assert(wide.height >= 88);
    for (const alignment of ['Left', 'Center', 'Right']) {
      const x = check(prefix + '/alignment-' + alignment), expected = alignment === 'Left' ? 0 : alignment === 'Right' ? width - 200 : (width - 200) / 2;
      assert.equal(x.expected, expected); assert(Math.abs(x.x - expected) < .01); assert(Math.abs(x.width - 200) < .01);
    }
    const peer = check(prefix + '/passive-metadata'); assert.equal(peer.tabStop, false); assert.equal(peer.role, 'Group');
  }
  assert.deepEqual(report.checks.map(x => x.id).sort(), ids.sort());
  return {passed: true, checks: ids.length, themes: 2, directions: 2, hostWidths: 2, inheritedFontSizes: [16, 24, 32],
    originalContentRetained: true, localFontPrecedence: true, oversizedContentVerified: true, alignmentVerified: true,
    systemTextScalingVerified: false, displayScalingVerified: false, nativeComponentAccepted: false};
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const option = name => { const at = process.argv.indexOf(name); assert(at >= 0 && process.argv[at + 1], name); return path.resolve(process.argv[at + 1]); };
  const report = JSON.parse(await fs.readFile(option('--report'), 'utf8'));
  const sha = async file => crypto.createHash('sha256').update(await fs.readFile(file)).digest('hex');
  const app = option('--app');
  const hostAt = process.argv.indexOf('--host-assembly');
  const host = hostAt >= 0 ? process.argv[hostAt + 1] : 'Orchard.Catalog.dll';
  assert(['Orchard.Catalog.dll', 'PackageConsumer.dll'].includes(host), 'Known validation host required');
  const audit = verifyPageFrameOverrides(report, {librarySha256: await sha(path.join(app, 'Orchard.dll')), hostSha256: await sha(path.join(app, host))});
  await fs.writeFile(option('--output'), JSON.stringify(audit, null, 2) + '\n'); console.log(JSON.stringify(audit));
}
