import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Scatter capture JSON path.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='84972e61d3849694bb5d69367e296b806b886747b1e24af1f252601dc86f7e00'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==64||new Set(capture.cases.map(c=>c.scheme+'/'+c.id)).size!==64)throw new Error('Frozen Scatter capture differs.');
for(const sample of capture.cases){if(!['Light','Dark'].includes(sample.scheme))throw new Error('Unexpected theme.');delete sample.normal.tooltipHtml;for(const state of sample.states){const {tooltip,tooltipVisible,tooltipBox,cursor,points}=state.state;state.state={tooltip,tooltipVisible,tooltipBox,cursor:cursor??null,points};}}
const bytes=JSON.stringify(capture,null,2)+'\n';
await writeFile(new URL('../tests/Reference/ScatterChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
