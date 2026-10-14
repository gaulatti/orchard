import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Stat Card content capture JSON path.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='7085e7d88f1109a0d1ca93885fc0de01aa735fd7ca973de1bdb63cac333ffb06'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==36||new Set(capture.cases.map(x=>`${x.scheme}/${x.id}`)).size!==36)throw new Error('Frozen Stat Card content capture differs.');
await writeFile(new URL('../tests/Reference/StatCardContent.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
