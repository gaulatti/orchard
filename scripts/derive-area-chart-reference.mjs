import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Area Chart capture JSON path.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='885d1f48264dd0b2f55a0fde90c554bd12fa983dee7c2c50b7f794faf8b6fdce'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==80||new Set(capture.cases.map(c=>`${c.scheme}/${c.props.stacked}/${c.id}`)).size!==80)throw new Error('Frozen Area Chart capture differs.');
for(const c of capture.cases){
 if(!['Light','Dark'].includes(c.scheme)||typeof c.props.stacked!=='boolean')throw new Error('Unexpected appearance/layout.');
 c.normal.yMarkup=c.key.yMarkup;
 for(const state of[c.key,c.right]){delete state.markup;delete state.yMarkup;}
 delete c.normal.legendIcons;delete c.normal.accessibility;
}
const bytes=JSON.stringify(capture,null,2)+'\n';
await writeFile(new URL('../tests/Reference/AreaChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
