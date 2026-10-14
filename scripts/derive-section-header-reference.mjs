import {readFile, writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';

if (!process.argv[2]) throw new Error('Pass the frozen Section Header browser capture JSON path.');
const bytes = await readFile(process.argv[2]);
const capture = JSON.parse(bytes);
if (capture.sourceCommit !== 'a86059013cd59684ce452849278ea81e734f2066' ||
    capture.sourceSha256 !== 'df2e51474c906760bf20e369256590872f07a13a4e29dca60ee5235bbee638bf' ||
    capture.react !== '19.2.4' || capture.cases.length !== 24 ||
    new Set(capture.cases.map(item => `${item.scheme}/${item.id}`)).size !== 24)
    throw new Error('Frozen Section Header capture differs.');
for (const item of capture.cases) {
    if (item.layout.description && !Number.isFinite(item.layout.descriptionBaseline))
        throw new Error(`Missing source baseline: ${item.scheme}/${item.id}`);
    if (item.id === 'full' && (!item.layout.descriptionInk || item.layout.descriptionInk.first > item.layout.descriptionInk.last))
        throw new Error(`Missing source painted rows: ${item.scheme}/${item.id}`);
}
await writeFile(new URL('../tests/Reference/SectionHeaderLayout.json', import.meta.url), bytes);
console.log(JSON.stringify({cases: capture.cases.length, captureSha256: createHash('sha256').update(bytes).digest('hex')}));
