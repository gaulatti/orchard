import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2]) throw new Error('Pass the frozen Dashboard Section browser capture JSON path.');
const bytes=await readFile(process.argv[2]),capture=JSON.parse(bytes);
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||
   capture.sourceSha256!=='f8af75ecc48d211202e4aa0022e81195259fc934777c28a180ea7f325bce7fe6'||capture.react!=='19.2.4'||capture.cases.length!==50||
   new Set(capture.cases.map(x=>`${x.scheme}/${x.id}`)).size!==50) throw new Error('Frozen Dashboard Section capture differs.');
for(const item of capture.cases){
    for(const name of ['title','description'])if(item.layout[name] && typeof item.layout[name].renderedText!=='string')throw new Error(`Missing rendered text: ${item.scheme}/${item.id}/${name}`);
    if(item.layout.title && typeof item.layout.title.accessibleName!=='string')throw new Error(`Missing heading name: ${item.scheme}/${item.id}`);
    if(item.layout.header && (!Number.isFinite(item.layout.lead?.minWidth)||!Number.isFinite(item.layout.lead?.maxWidth)))throw new Error(`Missing intrinsic widths: ${item.scheme}/${item.id}`);
}
await writeFile(new URL('../tests/Reference/DashboardSectionLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,captureSha256:createHash('sha256').update(bytes).digest('hex')}));
