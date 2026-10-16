import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const option = name => { const at = process.argv.indexOf(name); assert.ok(at >= 0 && process.argv[at + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[at + 1]); };
const native = option('--native'), output = option('--output');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const bytes = await fs.readFile(new URL('../tests/Reference/StepperTypography.json', import.meta.url));
const reference = JSON.parse(bytes), hash = value => crypto.createHash('sha256').update(value).digest('hex');
assert.equal(hash((await fs.readFile(new URL('./reference/capture-stepper-typography.cjs', import.meta.url), 'utf8')).replace(/\r\n/g, '\n')), reference.scriptSha256);
assert.equal((await read(path.join(native, 'result.json'))).passed, true);
const failures = []; let cases = 0, baselines = 0, maximumBaselineError = 0;
for (const scheme of ['Light', 'Dark']) {
    const actual = await read(path.join(native, `stepper-source-${scheme}.json`));
    const expected = reference.reports.filter(item => item.scheme === scheme);
    assert.equal(actual.cases.length, expected.length); assert.deepEqual(actual.failures, []);
    for (const [index, test] of expected.entries()) {
        const value = actual.cases[index]; assert.equal(value.id, test.id); assert.equal(value.rows.length, test.rows.length);
        for (const [row, text] of test.rows.entries()) for (const key of ['label', 'description', 'button']) {
            if (!text[key] || text[key].firstBaseline == null) continue;
            const found = value.rows[row][key + 'Typography'];
            assert.ok(found && Number.isFinite(found.baseline), 'Missing measured native baseline.');
            const delta = Math.abs(found.baseline - text[key].firstBaseline); maximumBaselineError = Math.max(maximumBaselineError, delta);
            if (delta > .05) failures.push({id: test.id, scheme, row, key, native: found.baseline, source: text[key].firstBaseline});
            if (Math.abs(found.LineHeight - parseFloat(text[key].lineHeight)) > .05) failures.push({id: test.id, scheme, row, key, nativeLineHeight: found.LineHeight, sourceLineHeight: text[key].lineHeight});
            baselines++;
        }
        cases++;
    }
}
assert.equal(cases, 44); assert.equal(baselines, 324);
const report = {passed: failures.length === 0, cases, baselines, maximumBaselineError, geometryTolerance: .05, sourcePixelParityVerified: false, nativeComponentAccepted: false, referenceSha256: hash(bytes), failures};
await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n'); console.log(JSON.stringify({...report, failures: undefined})); if (!report.passed) process.exitCode = 1;
