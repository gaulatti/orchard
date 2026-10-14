import { readFile, writeFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';

if (!process.argv[2]) throw new Error('Pass the pinned Stat Card typography capture JSON path.');
const bytes = await readFile(process.argv[2]), capture = JSON.parse(bytes);
const ids = ['default', 'description', 'trend', 'long', 'slots', 'zero', 'footer-wrap', 'trend-wrap'];
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' ||
    capture.sourceSha256 !== '7085e7d88f1109a0d1ca93885fc0de01aa735fd7ca973de1bdb63cac333ffb06' ||
    capture.react !== '19.2.4' || capture.recharts !== '3.8.1' || capture.deviceScale !== 1 ||
    capture.cases.length !== 16 || new Set(capture.cases.map(item => `${item.scheme}/${item.id}`)).size !== 16 ||
    capture.cases.some(item => !ids.includes(item.id) || !['Light', 'Dark'].includes(item.scheme) ||
      Object.values(item.layout.texts).some(text => !Number.isFinite(text.baseline) || !text.ink.rows.length) ||
      ['title', 'value'].some(key => item.faces[key].some(face => !face.isCustomFont || face.postScriptName !== 'EncodeSans-SmBold'))))
  throw new Error('Frozen Stat Card typography capture differs.');
const expectedFonts = {
  'EncodeSans-Regular.ttf': '756006a45004ac1fe39ed907c7d71d1c721524d4b1c87ed0a479cb913aeba182',
  'EncodeSans-Medium.ttf': '40a222af6b6dcd6940866415df454f2e27e798f5dc16539d72c67449c0eeca98',
  'EncodeSans-SemiBold.ttf': '9972f69621823bdd3c67738c02c813c8a52067871e92e467880cb2fa0a47ced5'
};
if (capture.fontFiles.length !== 3 || new Set(capture.fontFiles.map(font => font.file)).size !== 3)
  throw new Error('Expected the three pinned Encode Sans faces.');
for (const font of capture.fontFiles) {
  if (font.sha256 !== expectedFonts[font.file]) throw new Error('Unexpected reference font.');
  const actual = await readFile(new URL(`../src/Orchard/Assets/Fonts/${font.file}`, import.meta.url));
  if (createHash('sha256').update(actual).digest('hex') !== font.sha256) throw new Error(`Bundled font differs: ${font.file}`);
}
await writeFile(new URL('../tests/Reference/StatCardTypography.json', import.meta.url), bytes);
console.log(JSON.stringify({ cases: capture.cases.length, captureSha256: createHash('sha256').update(bytes).digest('hex') }));
