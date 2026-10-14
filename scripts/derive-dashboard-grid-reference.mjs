import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the frozen Dashboard Grid browser capture JSON path.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='69e25939e26e9fdc1973b6fdfff2924ff3ecd3ab909810a62cefed2fc628dd16'||capture.react!=='19.2.4'||capture.cases.length!==96||new Set(capture.cases.map(x=>`${x.scheme}/${x.id}`)).size!==96)throw new Error('Frozen Dashboard Grid capture differs.');
await writeFile(new URL('../tests/Reference/DashboardGridLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
