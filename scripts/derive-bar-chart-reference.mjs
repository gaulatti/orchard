import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Bar Chart capture JSON path.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='dcd8c302e50d0b8255207a57603999a5498cf4361034021d71aab96cd6f65235'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==68||new Set(capture.cases.map(c=>`${c.scheme}/${c.props.layout}/${c.id}`)).size!==68)throw new Error('Frozen Bar Chart capture differs.');
for(const c of capture.cases){
 if(!['Light','Dark'].includes(c.scheme)||!['horizontal','vertical'].includes(c.props.layout))throw new Error('Unexpected appearance/layout.');
 c.normal.yMarkup=c.key.yMarkup;
 for(const state of[c.key,c.right]){delete state.markup;delete state.yMarkup;}
 delete c.normal.legendIcons;delete c.normal.accessibility;
}
const bytes=JSON.stringify(capture,null,2)+'\n';
await writeFile(new URL('../tests/Reference/BarChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
