import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option = name => {
    const index = process.argv.indexOf(name);
    assert(index >= 0 && process.argv[index + 1], `Pass ${name} <path>.`);
    return path.resolve(process.argv[index + 1]);
};
const directory = option('--native'), sourceDirectory = option('--source');
const baselineDirectory = option('--baseline'), output = option('--output');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const json = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const referenceBytes = await fs.readFile(new URL('../tests/Reference/Stepper.json', import.meta.url));
const reference = JSON.parse(referenceBytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory, 'reference.json'))), hash(referenceBytes));
const runtime = await json(path.join(directory, 'result.json'));
assert(runtime.passed && runtime.captureOnly && !runtime.parityVerified);
assert.equal(runtime.component, 'stepper-raster');
assert.equal(runtime.environment.displayScale, 1);
assert.equal(runtime.environment.textScale, 1);
assert.equal(runtime.environment.highContrast, false);
const restoration = await json(path.join(directory, 'stepper-raster-restoration.json'));
assert(restoration.restored && restoration.overlayRemoved, 'Original catalog parent was not restored.');
const reports = await json(path.join(directory, 'stepper-raster.json'));
assert.equal(reports.length, reference.cases.length);
const seen = new Set();
const image = async file => {
    const bytes = await fs.readFile(file);
    return {...readRgbaPng(bytes), sha256: hash(bytes)};
};
function crop(image, box) {
    const {x, y, width, height} = box;
    assert([x,y,width,height].every(Number.isInteger), 'Frame coordinates must be integers.');
    assert(x >= 0 && y >= 0 && x + width <= image.width && y + height <= image.height);
    const rgba = Buffer.alloc(width * height * 4);
    for (let row = 0; row < height; row++) {
        image.rgba.copy(rgba, row * width * 4, ((y + row) * image.width + x) * 4, ((y + row) * image.width + x + width) * 4);
    }
    return {width, height, rgba};
}
function compare(a, b) {
    assert.equal(a.width, b.width); assert.equal(a.height, b.height);
    let differentPixels = 0, absoluteRgbaError = 0, maxChannel = 0;
    for (let offset = 0; offset < a.rgba.length; offset += 4) {
        let changed = false;
        for (let channel = 0; channel < 4; channel++) {
            const delta = Math.abs(a.rgba[offset + channel] - b.rgba[offset + channel]);
            changed ||= delta !== 0;
            absoluteRgbaError += delta;
            maxChannel = Math.max(maxChannel, delta);
        }
        differentPixels += Number(changed);
    }
    return {differentPixels, absoluteRgbaError, maxChannel};
}
const pairs = [];
for (const report of reports) {
    const key = `${report.id}/${report.scheme}`;
    assert(!seen.has(key), `Duplicate raster case ${key}.`); seen.add(key);
    const spec = reference.cases.find(c => c.id === report.id && c.scheme === report.scheme);
    assert(spec); assert.equal(report.sourceFile, spec.file);
    assert.equal(report.frame.width, 1024); assert.equal(report.frame.height, 745);
    const before = await image(path.join(directory, report.before));
    const after = await image(path.join(directory, report.after));
    assert.equal(before.width, report.client.width); assert.equal(before.height, report.client.height);
    const screen = crop(before, report.frame), screenAfter = crop(after, report.frame);
    const stability = compare(screen, screenAfter);
    assert.equal(stability.differentPixels, 0, `${key}: on-screen frame changed during bitmap rendering.`);
    const bitmap = await image(path.join(directory, report.bitmap));
    const source = await image(path.join(sourceDirectory, spec.file));
    assert.equal(source.sha256, spec.pngSha256);
    const baseline = await image(path.join(baselineDirectory, spec.file));
    pairs.push({id: report.id, scheme: report.scheme, frame: report.frame,
        hashes: {before: before.sha256, after: after.sha256, bitmap: bitmap.sha256, source: source.sha256, baseline: baseline.sha256},
        stability, screenVsBitmap: compare(screen, bitmap), bitmapVsBaseline: compare(bitmap, baseline),
        sourceVsBitmap: compare(source, bitmap), sourceVsScreen: compare(source, screen)});
}
const totals = Object.fromEntries(['screenVsBitmap','bitmapVsBaseline','sourceVsBitmap','sourceVsScreen'].map(key => [key, {
    differingCases: pairs.filter(p => p[key].differentPixels > 0).length,
    differentPixels: pairs.reduce((n,p) => n + p[key].differentPixels, 0),
    absoluteRgbaError: pairs.reduce((n,p) => n + p[key].absoluteRgbaError, 0),
}]));
const audit = {format: 'orchard-stepper-raster-diagnostic-v1', capturedCases: pairs.length,
    stableScreenCases: pairs.length, restoration, referenceSha256: hash(referenceBytes),
    verifierSha256: hash(await fs.readFile(new URL(import.meta.url))),
    masks: 0, tolerance: 0, resampling: false, screenExtraction: 'Exact integer frame bounds; all 1024x745 pixels retained.',
    sourcePixelParityVerified: false, nativeComponentAccepted: false, totals, pairs};
await fs.writeFile(output, JSON.stringify(audit, null, 2) + '\n');
console.log(JSON.stringify({...audit, pairs: undefined}, null, 2));
