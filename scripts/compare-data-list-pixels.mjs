import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option = name => { const i = process.argv.indexOf(name); assert(i >= 0 && process.argv[i + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[i + 1]); };
const sourceDirectory = option('--source'), output = option('--output');
const directories = new Map([1024,639,640].map(width => [width, option(`--native-${width}`)]));
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const json = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const referenceBytes = await fs.readFile(new URL('../tests/Reference/DataList.json', import.meta.url));
const reference = JSON.parse(referenceBytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory, 'reference.json'))), hash(referenceBytes));
assert.equal(hash((await fs.readFile(new URL('./reference/capture-data-list.cjs', import.meta.url), 'utf8')).replace(/\r\n/g, '\n')), reference.scriptSha256);
for (const [viewport, directory] of directories) {
    const runtime = await json(path.join(directory, 'result.json'));
    assert(runtime.passed); assert.equal(runtime.component, 'data-list');
    assert.equal(runtime.environment.displayScale, 1); assert.equal(runtime.environment.textScale, 1); assert.equal(runtime.environment.highContrast, false);
    for (const scheme of ['Light','Dark']) {
        const report = await json(path.join(directory, `data-list-source-${viewport}-${scheme}.json`));
        assert.deepEqual(report.failures, []); assert.equal(report.cases.length, 12);
        const expected = reference.cases.filter(c => c.viewportWidth === viewport && c.scheme === scheme);
        assert.deepEqual(report.cases.map(c => c.id), expected.map(c => c.id));
        assert(report.cases.every(c => c.errors.length === 0));
        const input = await json(path.join(directory, `data-list-input-${scheme}.json`));
        assert(input.passed && input.originalContent && input.keyedReorder && input.unloadRemount && input.templateReplacement && input.liveTemplateBinding && input.liveBreakpoint);
        assert.equal(input.physicalActivations, 3); assert.equal(input.contentTemplates, 3);
    }
}
const pairs = [];
for (const point of reference.cases) {
    const sourceBytes = await fs.readFile(path.join(sourceDirectory, point.file));
    assert.equal(hash(sourceBytes), point.pngSha256);
    const nativeBytes = await fs.readFile(path.join(directories.get(point.viewportWidth), point.file));
    const browser = readRgbaPng(sourceBytes), native = readRgbaPng(nativeBytes);
    assert.equal(native.width, browser.width, point.file); assert.equal(native.height, browser.height, point.file);
    let differentPixels = 0, absoluteRgbaError = 0, maxChannel = 0;
    for (let offset = 0; offset < browser.rgba.length; offset += 4) {
        let changed = false;
        for (let channel = 0; channel < 4; channel++) {
            const delta = Math.abs(browser.rgba[offset + channel] - native.rgba[offset + channel]);
            changed ||= delta !== 0; absoluteRgbaError += delta; maxChannel = Math.max(maxChannel, delta);
        }
        differentPixels += Number(changed);
    }
    pairs.push({id: point.id, viewportWidth: point.viewportWidth, scheme: point.scheme,
        sourceSha256: point.pngSha256, nativeSha256: hash(nativeBytes), differentPixels, absoluteRgbaError, maxChannel});
}
assert.equal(pairs.length, 72);
const audit = {format: 'orchard-data-list-pixel-audit-v1', checkedNativeGatesPassed: true,
    referenceSha256: hash(referenceBytes), verifierSha256: hash(await fs.readFile(new URL(import.meta.url))),
    sourcePixelParityVerified: pairs.every(pair => pair.differentPixels === 0), nativeComponentAccepted: false,
    masks: 0, tolerance: 0, resampling: false, cases: pairs.length,
    differingCases: pairs.filter(pair => pair.differentPixels > 0).length,
    differentPixels: pairs.reduce((n,p) => n + p.differentPixels, 0),
    absoluteRgbaError: pairs.reduce((n,p) => n + p.absoluteRgbaError, 0), pairs};
await fs.writeFile(output, JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify({...audit, pairs: undefined}, null, 2));
