import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';

const option = name => {
    const index = process.argv.indexOf(name);
    assert(index >= 0 && process.argv[index + 1], `Pass ${name} <path>.`);
    return path.resolve(process.argv[index + 1]);
};
const directory = option('--native'), source = option('--source');
const baseline = option('--baseline'), output = option('--output');
const verifier = fileURLToPath(new URL('./compare-stepper-raster.mjs', import.meta.url));
const json = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), 'orchard-stepper-raster-'));
const results = [];
try {
    const originalReports = await json(path.join(directory, 'stepper-raster.json'));
    const originalRestoration = await json(path.join(directory, 'stepper-raster-restoration.json'));
    const runtime = await fs.readFile(path.join(directory, 'result.json'));
    for (const [name, mutate, expected] of [
        ['changed-screen', (reports) => { reports[0].after = reports[1].before; }, /on-screen frame changed during bitmap rendering/],
        ['fractional-frame', (reports) => { reports[0].frame.x += .5; }, /Frame coordinates must be integers/],
        ['duplicate-case', (reports) => { reports[1] = structuredClone(reports[0]); }, /Duplicate raster case/],
        ['unrestored-parent', (_, restoration) => { restoration.restored = false; }, /Original catalog parent was not restored/],
    ]) {
        const reports = structuredClone(originalReports), restoration = structuredClone(originalRestoration);
        for (const report of reports) for (const key of ['before','bitmap','after']) {
            report[key] = path.relative(tempRoot, path.resolve(directory, report[key]));
        }
        mutate(reports, restoration);
        await fs.writeFile(path.join(tempRoot, 'result.json'), runtime);
        await fs.writeFile(path.join(tempRoot, 'stepper-raster.json'), JSON.stringify(reports));
        await fs.writeFile(path.join(tempRoot, 'stepper-raster-restoration.json'), JSON.stringify(restoration));
        const processResult = spawnSync(process.execPath, [verifier, '--native', tempRoot, '--source', source,
            '--baseline', baseline, '--output', path.join(tempRoot, 'unexpected-audit.json')], {encoding: 'utf8', timeout: 60000});
        assert.ifError(processResult.error);
        assert.notEqual(processResult.status, 0, `${name} unexpectedly passed.`);
        assert.match(processResult.stderr, expected, `${name} failed for an unrelated reason.`);
        results.push({name, rejected: true, reason: expected.source});
    }
} finally {
    assert.equal(path.dirname(tempRoot), path.resolve(os.tmpdir()));
    assert(path.basename(tempRoot).startsWith('orchard-stepper-raster-'));
    await fs.rm(tempRoot, {recursive: true});
}
const report = {passed: true, rejectedCases: results.length, temporaryFixtureRemoved: true, cases: results};
await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
