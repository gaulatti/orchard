import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Pie/Donut capture JSON path.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='985309bd4131c154f88fcca7f03839519da66192e49bd940b92e3307a9ddf0ac'||capture.donutSha256!=='dc5a9151b7501563651ba025c82da3b0b976e12fcc5fef0a50720aa6f00874f8'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==84||new Set(capture.cases.map(c=>`${c.scheme}/${c.props.mode}/${c.id}`)).size!==84)throw new Error('Frozen Pie/Donut capture differs.');
for(const c of capture.cases){
 if(!['Light','Dark'].includes(c.scheme)||!['pie','donut'].includes(c.props.mode))throw new Error('Unexpected theme or component.');
 delete c.normal.svg;delete c.normal.roles;
 for(const state of[c.normal,c.focus,c.right,c.left,c.enter])delete state.tooltipMarkup;
}
const bytes=JSON.stringify(capture,null,2)+'\n';
await writeFile(new URL('../tests/Reference/PieChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
