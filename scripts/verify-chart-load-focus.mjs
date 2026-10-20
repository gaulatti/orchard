import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const option = (name, required = true) => {
    const index = process.argv.indexOf(name);
    if (!required && index < 0) return null;
    assert(index >= 0 && process.argv[index + 1], `Pass ${name} <path>.`);
    return path.resolve(process.argv[index + 1]);
};
const directory = option('--native'), beforeDirectory = option('--before', false), output = option('--output');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const runtime = await read(path.join(directory, 'result.json'));
assert(runtime.passed); assert.equal(runtime.component, 'chart-load-focus'); assert.equal(runtime.cases, 24);
const report = await read(path.join(directory, 'chart-load-focus.json'));
assert.equal(report.reports.length, 24); assert.deepEqual(report.failures, []);
assert.equal(report.traces.length, 4);
let staleBlurAfterLoaded = 0, genuineBlurChecks = 0;
for (const profile of ['bar','sankey']) for (const theme of ['Light','Dark']) {
    const cases = report.reports.filter(c => c.profile === profile && c.theme === theme);
    assert.equal(cases.length, 6);
    const trace = report.traces.find(t => t.profile === profile && t.theme === theme).trace;
    for (const point of cases) {
        assert(point.acceptedFocus && point.focusedBeforePresentation);
        assert.equal(point.loadingEvents, 1); assert.equal(point.loadedEvents, 1);
        assert.equal(point.firstHelp, profile === 'bar' ? 'A;  : 10' : 'A : 10');
        assert.equal(point.afterRight, profile === 'bar' ? 'B;  : 20' : 'B : 10');
        const events = trace.filter(e => e.cycle === point.cycle);
        const got = events.findIndex(e => e.phase === 'got-focus'), loaded = events.findIndex(e => e.phase === 'loaded');
        assert(got >= 0 && loaded >= 0);
        if (point.mode === 'eager-reparent') {
            assert(events[loaded].focused);
            assert.equal(events[loaded].help, point.expectedFirstHelp);
            assert(point.gotFocusBeforeLoaded); assert(!events[got].IsLoaded);
        } else {
            assert.equal(point.mode, 'loaded-then-tab');
            assert(!events[loaded].focused && events[loaded].focusState === 'Unfocused');
            assert.equal(events[loaded].help, '');
            assert(!point.gotFocusBeforeLoaded); assert(events[got].IsLoaded);
            const unfocused = events.find(e => e.phase === 'loaded-before-focus');
            assert(unfocused && !unfocused.focused && unfocused.help === '');
            assert.equal(events[got].help, point.expectedFirstHelp);
            const tab = report.tabEntries.filter(e => e.profile === profile && e.theme === theme && e.cycle === point.cycle);
            assert.equal(tab.length, 1);
            assert.equal(tab[0].key, 0x09); assert.equal(tab[0].downs, 1); assert.equal(tab[0].ups, 1);
            assert.equal(tab[0].focusEvents, 1); assert(tab[0].downOwned && tab[0].upOwned && tab[0].loadedBeforeInput);
        }
        assert.equal(point.gotFocusBeforeLoaded, got < loaded);
        assert(['eager-reparent', 'loaded-then-tab'].includes(point.mode));
        for (const event of events.slice(loaded + 1)) if (event.phase === 'lost-focus' && event.focused) {
            staleBlurAfterLoaded++; assert.notEqual(event.help, '');
        }
        if (point.mode === 'loaded-then-tab') {
            const blur = events.find(e => e.phase === 'after-blur'); assert(blur);
            assert(!blur.focused && blur.focusState === 'Unfocused'); assert.equal(blur.help, ''); genuineBlurChecks++;
        }
    }
}
assert.equal(report.tabEntries.length, 12);
assert.equal(genuineBlurChecks, 12);
const input = await read(path.join(directory, 'chart-load-focus-desktop.json'));
assert.equal(input.keyActions.length, 24);
for (const key of input.keyActions) { assert.equal(key.key, 0x27); assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert(key.targetRetained); }
for (const gate of input.pointerPresentations) { assert(gate.passed && gate.stable >= 2 && gate.milliseconds < 2000); }
let negative = null;
if (beforeDirectory) {
    assert.equal((await read(path.join(beforeDirectory, 'result.json'))).passed, false);
    const before = await read(path.join(beforeDirectory, 'chart-load-focus.json'));
    assert.equal(before.reports.length, 24); assert.equal(before.failures.length, 24);
    const eager = before.reports.filter(c => c.mode === 'eager-reparent'), loading = before.reports.filter(c => c.mode === 'loaded-then-tab');
    assert.equal(eager.length, 12); assert.equal(loading.length, 12);
    assert(eager.every(c => c.gotFocusBeforeLoaded && c.acceptedFocus && c.focusedBeforePresentation && c.firstHelp === '' && c.afterRight === c.expectedFirstHelp));
    assert(loading.every(c => !c.gotFocusBeforeLoaded && c.acceptedFocus && c.focusedBeforePresentation && c.firstHelp === c.expectedFirstHelp && c.afterRight === (c.profile === 'bar' ? 'B;  : 20' : 'B : 10')));
    negative = {cases: 24, eagerFailures: 12, loadingPasses: 12, firstAndNextFailures: 24};
}
const eagerBeforeLoadedCases = report.reports.filter(c => c.mode === 'eager-reparent' && c.gotFocusBeforeLoaded).length;
assert.equal(eagerBeforeLoadedCases, 12, 'Every eager case must prove its focus-before-Loaded boundary.');
const gotFocusBeforeLoadedCases = report.reports.filter(c => c.gotFocusBeforeLoaded).length;
assert.equal(gotFocusBeforeLoadedCases, 12, 'Exactly12 eager and12 loaded-first native event orders must be exercised.');
const audit = {passed: true, cases: 24, eagerCases: 12, loadingCases: 12, eagerBeforeLoadedCases,
    gotFocusBeforeLoadedCases, loadedBeforeGotFocusCases: 24 - gotFocusBeforeLoadedCases,
    staleBlurAfterLoaded, genuineBlurChecks,
    ownedRightPairs: input.keyActions.length, ownedLoadedFirstTabPairs: report.tabEntries.length, profiles: ['bar','sankey'], themes: ['Light','Dark'], negative,
    nativeComponentAccepted: false, verifierSha256: crypto.createHash('sha256').update(await fs.readFile(new URL(import.meta.url))).digest('hex')};
await fs.writeFile(output, JSON.stringify(audit, null, 2) + '\n'); console.log(JSON.stringify(audit, null, 2));
