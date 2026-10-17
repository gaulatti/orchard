import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';

const option = name => {const index = process.argv.indexOf(name); assert(index >= 0 && process.argv[index + 1], name); return path.resolve(process.argv[index + 1]);};
const read = async file => JSON.parse((await fs.readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
const root = option('--native-root');
const source = await read(new URL('../tests/Reference/DateTypography.json', import.meta.url));
assert.equal(source.measurementPixelsUnchanged, true); assert.equal(source.cases.length, 188);
let frames = 0, texts = 0, tabularDays = 0; const errors = [];
const compare = (context, field, actual, expected) => {if (!Number.isFinite(actual) || Math.abs(actual - expected) >= .001) errors.push({context, field, actual, expected});};
for (const width of [320, 640]) {
  const calendar = await read(path.join(root, `dates-native-${width}-current/date-controls.json`));
  const presentation = await read(path.join(root, `dates-presentation-${width}/date-presentation.json`));
  assert.equal(calendar.passed, true); assert.equal(presentation.passed, true);
  const actual = [...calendar.frames.map(frame => ({...frame, kind:'calendar'})), ...presentation.frames];
  const wanted = source.cases.filter(frame => frame.width === width); assert.equal(actual.length, wanted.length);
  for (const expected of wanted) {
    assert.equal(expected.pngSha256, expected.measurementPngSha256);
    const matches = actual.filter(frame => frame.kind === expected.kind && frame.id === expected.id && frame.state === expected.state && frame.scheme === expected.scheme);
    assert.equal(matches.length, 1, expected.file); const frame = matches[0];
    assert.equal(frame.typography.length, expected.texts.length, expected.file);
    for (const wantedText of expected.texts) {
      const context = expected.file + '/' + wantedText.role, found = frame.typography.filter(text => text.role === wantedText.role); assert.equal(found.length, 1, context);
      const text = found[0]; assert.equal(text.text, wantedText.displayText, context);
      for (const field of ['baseline','fontSize','fontWeight','lineHeight']) compare(context, field, text[field], wantedText[field]);
      compare(context, 'letterSpacing', text.letterSpacing, wantedText.letterSpacing === 'normal' ? 0 : parseFloat(wantedText.letterSpacing));
      assert.equal(text.numerals, wantedText.numerals === 'tabular-nums' ? 'Tabular' : 'Normal', context);
      if (wantedText.role.startsWith('day-')) tabularDays++;
      texts++;
    }
    frames++;
  }
}
const result = {passed:errors.length === 0, frames, textEntries:texts, tabularDays, errors, sourcePngHashesUnchanged:188,
  sourceRasterParityVerified:false, sourceShadowVerified:false, normalMotionVerified:false, nativeComponentAccepted:false};
await fs.writeFile(option('--output'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({...result, errors:errors.slice(0,10)})); assert.equal(errors.length, 0, 'Date typography differs from the frozen source.');
