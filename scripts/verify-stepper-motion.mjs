import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const option = name => { const at = process.argv.indexOf(name); assert.ok(at >= 0 && process.argv[at + 1], `Pass ${name} <path>.`); return path.resolve(process.argv[at + 1]); };
const native = option('--native'), output = option('--output');
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const reference = await read(new URL('../tests/Reference/StepperMotion.json', import.meta.url));
let transitions = 0, frames = 0, heldKeys = 0, maximumTrajectoryError = 0;
for (const [mode, enabled] of [['enabled', true], ['reduced', false]]) {
    const runtime = await read(path.join(native, mode, 'result.json')); assert.equal(runtime.passed, true); assert.equal(runtime.environment.animationsEnabled, enabled);
    for (const scheme of ['Light', 'Dark']) {
        const data = await read(path.join(native, mode, `stepper-motion-${scheme}.json`));
        assert.equal(data.passed, true); assert.equal(data.enabled, enabled); assert.equal(data.instantScale, true); assert.deepEqual(data.failures, []);
        assert.equal(data.callbackRemovalPreservedMotion, true);
        const expected = reference.transitions.filter(item => item.scheme === scheme && item.reduced !== enabled);
        assert.equal(data.reports.length, expected.length);
        for (const [index, value] of data.reports.entries()) {
            assert.equal(value.kind, expected[index].kind); assert.equal(value.from, expected[index].from); assert.equal(value.to, expected[index].to); assert.equal(value.status, expected[index].status); assert.deepEqual(value.errors, []);
            for (const color of ['Background', 'Foreground']) {
                const first = value.before[color], last = value.after[color];
                const axis = [0, 1, 2].sort((a, b) => Math.abs(last[b] - first[b]) - Math.abs(last[a] - first[a]))[0], range = last[axis] - first[axis];
                for (const sample of value.samples) {
                    if (range !== 0) {
                        const progress = (sample[color][axis] - first[axis]) / range;
                        for (let channel = 0; channel < 3; channel++) {
                            const error = Math.abs(sample[color][channel] - (first[channel] + (last[channel] - first[channel]) * progress));
                            maximumTrajectoryError = Math.max(maximumTrajectoryError, error);
                            assert.ok(error <= 1.5, `${mode}/${scheme}/${value.kind}/${value.from}->${value.to}/${color}: wrong color space (${error})`);
                        }
                    }
                    if (!enabled) assert.deepEqual(sample[color], last, 'Reduced motion retained an intermediate color.');
                }
            }
            transitions++; frames += value.samples.length;
        }
        // WinUI exposes the composition transform's single-precision value.
        assert.deepEqual(data.presses.map(item => [item.IsPressed, item.ScaleX, item.ScaleY]), [[true, Math.fround(.94), Math.fround(.94)], [false, 1, 1]]);
        const keys = await read(path.join(native, mode, `stepper-motion-${scheme}-held-keys.json`));
        assert.equal(keys.length, 1); for (const key of keys) { assert.equal(key.downs, 1); assert.equal(key.ups, 1); assert.equal(key.downOwned, true); assert.equal(key.upOwned, true); heldKeys++; }
    }
}
assert.equal(transitions, 42); assert.equal(heldKeys, 4);
const restored = await read(path.join(native, 'preference-restored.json')); assert.equal(restored.passed, true); assert.equal(restored.original, restored.restored);
const report = {passed: true, transitions, frames, heldKeys, maximumTrajectoryError, instantScale: true, windowsPreferenceRestored: true, nativeComponentAccepted: false, sourcePixelParityVerified: false, referenceSha256: crypto.createHash('sha256').update(await fs.readFile(new URL('../tests/Reference/StepperMotion.json', import.meta.url))).digest('hex')};
await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n'); console.log(JSON.stringify(report));
