import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2]) throw new Error('Pass the frozen Dashboard Section browser capture JSON path.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||
   capture.sourceSha256!=='f8af75ecc48d211202e4aa0022e81195259fc934777c28a180ea7f325bce7fe6'||capture.react!=='19.2.4'||capture.cases.length!==30||
   new Set(capture.cases.map(x=>`${x.scheme}/${x.id}`)).size!==30) throw new Error('Frozen Dashboard Section capture differs.');
await writeFile(new URL('../tests/Reference/DashboardSectionLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
