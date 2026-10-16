import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const option = name => { const at = process.argv.indexOf(name); assert.ok(at >= 0 && process.argv[at + 1], `Pass ${name} <value>.`); return process.argv[at + 1]; };
const native = path.resolve(option('--native')), output = path.resolve(option('--output')), viewport = Number(option('--viewport'));
const fullSuite = process.argv.includes('--full-suite');
assert.ok([639, 640, 1024].includes(viewport));
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const bytes = await fs.readFile(new URL('../tests/Reference/DataListTypography.json', import.meta.url)), reference = JSON.parse(bytes);
assert.equal(hash(await fs.readFile(new URL('../tests/Reference/DataList.json', import.meta.url))), reference.normalReferenceSha256);
assert.equal(hash((await fs.readFile(new URL('./reference/capture-data-list-typography.cjs', import.meta.url), 'utf8')).replace(/\r\n/g, '\n')), reference.scriptSha256);
const runtime = await read(path.join(native, 'result.json'));
assert.equal(runtime.passed, true);
if (fullSuite) {
    assert.equal(runtime.component, undefined); assert.equal(runtime.systemAppearance, true);
    assert.equal(runtime.actionCount, 2); assert.equal(runtime.captures.length, 2);
} else assert.equal(runtime.component, 'data-list');
assert.equal(runtime.environment.displayScale, 1); assert.equal(runtime.environment.textScale, 1); assert.equal(runtime.environment.highContrast, false);
const failures = [], consumerContent = []; let cases = 0, baselines = 0, maximumBaselineError = 0;
for (const scheme of ['Light', 'Dark']) {
    const actual = await read(path.join(native, `data-list-source-${viewport}-${scheme}.json`));
    const expected = reference.reports.filter(test => test.viewportWidth === viewport && test.scheme === scheme);
    assert.equal(actual.cases.length, expected.length); assert.deepEqual(actual.failures, []);
    for (const [index, test] of expected.entries()) {
        const value = actual.cases[index]; assert.equal(value.id, test.id); assert.equal(value.rows.length, test.rows.length);
        for (const [row, text] of test.rows.entries()) for (const key of ['label', 'value', 'description']) {
            if (!text[key] || text[key].firstBaseline == null) continue;
            const found = value.rows[row][key + 'Typography'];
            assert.ok(found && Number.isFinite(found.baseline), 'Missing measured native baseline.');
            const delta = Math.abs(found.baseline - text[key].firstBaseline);
            const measurement = {id: test.id, scheme, row, key, native: found.baseline, source: text[key].firstBaseline, delta};
            // UIElement/DataTemplate content retains its own typography. Record
            // the original action's text without accepting it as library text.
            if (text[key].rich) { consumerContent.push(measurement); continue; }
            maximumBaselineError = Math.max(maximumBaselineError, delta);
            assert.equal(found.text, text[key].text.replace(/[ \t\r\n\f]+/g, ' ').trim());
            if (delta > .05) failures.push(measurement);
            if (Math.abs(found.LineHeight - parseFloat(text[key].lineHeight)) > .05) failures.push({...measurement, nativeLineHeight: found.LineHeight, sourceLineHeight: text[key].lineHeight});
            baselines++;
        }
        cases++;
    }
}
assert.equal(cases, 24); assert.equal(baselines, 132); assert.equal(consumerContent.length, 2);
const report = {passed: failures.length === 0, mode: fullSuite ? 'full-suite' : 'data-list', viewport, cases, baselines, maximumBaselineError, geometryTolerance: .05,
    sourcePixelParityVerified: false, nativeComponentAccepted: false, referenceSha256: hash(bytes), consumerContent, failures};
await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({...report, failures: undefined, consumerContent: undefined}));
if (!report.passed) process.exitCode = 1;
