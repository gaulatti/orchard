import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => { const index = process.argv.indexOf(name); assert.ok(index >= 0 && process.argv[index + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[index + 1]); };
const sourceDirectory = option('--source'), nativeDirectory = option('--native'), output = option('--output');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const bytes = await fs.readFile(new URL('../tests/Reference/Stepper.json', import.meta.url));
const reference = JSON.parse(bytes), referenceSha256 = hash(bytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory, 'reference.json'))), referenceSha256);
assert.equal(hash((await fs.readFile(new URL('./reference/capture-stepper.cjs', import.meta.url), 'utf8')).replace(/\r\n/g, '\n')), reference.scriptSha256);
const runtime = await read(path.join(nativeDirectory, 'result.json'));
assert.equal(runtime.passed, true);
assert.equal(runtime.environment.displayScale, 1); assert.equal(runtime.environment.textScale, 1); assert.equal(runtime.environment.highContrast, false);
const pairs = [];
for (const scheme of ['Light', 'Dark']) {
    const nativeReport = await read(path.join(nativeDirectory, `stepper-source-${scheme}.json`));
    assert.deepEqual(nativeReport.failures, []);
    const expected = reference.cases.filter(item => item.scheme === scheme);
    assert.equal(nativeReport.cases.length, expected.length);
    assert.equal((await read(path.join(nativeDirectory, `stepper-input-${scheme}.json`))).passed, true);
    for (const [index, point] of expected.entries()) {
        assert.equal(nativeReport.cases[index].id, point.id); assert.deepEqual(nativeReport.cases[index].errors, []);
        const sourceBytes = await fs.readFile(path.join(sourceDirectory, point.file)); assert.equal(hash(sourceBytes), point.pngSha256);
        const nativeBytes = await fs.readFile(path.join(nativeDirectory, point.file));
        const browser = readRgbaPng(sourceBytes), native = readRgbaPng(nativeBytes);
        assert.equal(browser.width, 1024); assert.equal(browser.height, 745);
        assert.equal(native.width, browser.width); assert.equal(native.height, browser.height);
        let differentPixels = 0, absoluteRgbaError = 0, maxChannel = 0;
        for (let offset = 0; offset < browser.rgba.length; offset += 4) {
            let changed = false;
            for (let channel = 0; channel < 4; channel++) {
                const delta = Math.abs(browser.rgba[offset + channel] - native.rgba[offset + channel]);
                changed ||= delta !== 0; absoluteRgbaError += delta; maxChannel = Math.max(maxChannel, delta);
            }
            if (changed) differentPixels++;
        }
        pairs.push({id: point.id, scheme, sourceSha256: point.pngSha256, nativeSha256: hash(nativeBytes), differentPixels, absoluteRgbaError, maxChannel});
    }
}
assert.equal(pairs.length, 44);
const report = {format: 'orchard-stepper-pixel-audit-v1', checkedNativeGatesPassed: true, nativeComponentAccepted: false, sourcePixelParityVerified: pairs.every(pair => pair.differentPixels === 0), referenceSha256, verifierSha256: hash(await fs.readFile(new URL(import.meta.url))), masks: 0, tolerance: 0, resampling: false, pairs};
await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, pairs: undefined, cases: pairs.length, differingCases: pairs.filter(pair => pair.differentPixels > 0).length, differentPixels: pairs.reduce((sum, pair) => sum + pair.differentPixels, 0)}));
