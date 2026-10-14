import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';

const input = process.argv[2];
if (!input) throw new Error('Pass the full frozen browser capture JSON path.');
const bytes = await readFile(input);
const capture = JSON.parse(bytes);
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' ||
    capture.sourceSha256 !== '673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a' ||
    capture.recharts !== '3.8.1' || capture.results.length !== 22) {
  throw new Error('The capture does not match the frozen Line Chart boundary.');
}
const cases = capture.results.map(({ id, scheme, props, normal }) => ({
  id, scheme, props, width: normal.width, height: normal.height,
  curves: normal.lines, grid: normal.grid,
  xTicks: normal.xTicks.map(({ text, x, y, fill }) => ({ text, x: Number(x), y: Number(y), fill })),
  yTicks: normal.yTicks.map(({ text, x, y, fill }) => ({ text, x: Number(x), y: Number(y), fill })),
  legend: normal.legend.map(({ text }) => text),
}));
const reference = {
  sourceCommit: capture.sourceCommit, sourceSha256: capture.sourceSha256,
  captureSha256: createHash('sha256').update(bytes).digest('hex'),
  browser: capture.browser, recharts: capture.recharts, curvePrecision: 3, cases,
};
await writeFile(new URL('../samples/Orchard.Catalog/Reference/LineLayout.json', import.meta.url), JSON.stringify(reference, null, 2) + '\n');
console.log(JSON.stringify({ cases: cases.length, captureSha256: reference.captureSha256 }));
