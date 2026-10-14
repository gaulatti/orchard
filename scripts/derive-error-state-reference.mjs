import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the frozen Error State browser capture JSON path.');
const bytes=await readFile(process.argv[2]);const capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='b1c0e51f0d36ef3e29443393b983f8bf05caaf47d1d666fcf6202092902e033d'||capture.react!=='19.2.4'||capture.lucide!=='0.563.0'||capture.cases.length!==14)throw new Error('Frozen Error State capture differs.');
await writeFile(new URL('../samples/Orchard.Catalog/Reference/ErrorStateLayout.json',import.meta.url),JSON.stringify({...capture,captureSha256:createHash('sha256').update(bytes).digest('hex')},null,2)+'\n');
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
