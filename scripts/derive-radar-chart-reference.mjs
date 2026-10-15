import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
if(!process.argv[2])throw new Error('Pass the pinned Radar capture JSON path.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='62d3df8715257cc90e3230b196de68744252b7aaa381240531d186444faaf969'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==72||new Set(capture.cases.map(c=>c.scheme+'/'+c.id)).size!==72)throw new Error('Frozen Radar capture differs.');
for(const sample of capture.cases){
 if(!['Light','Dark'].includes(sample.scheme)||sample.errors.length)throw new Error('Invalid source Radar case.');
 if(!capture.cases.some(other=>other.id===sample.id&&other.scheme!==sample.scheme))throw new Error('Unpaired source Radar case.');
 delete sample.normal.tooltipHtml;
 for(const shape of sample.normal.polygons)delete shape.attributes.id;
 for(const state of sample.states){const{tooltip,tooltipVisible,tooltipBox,tooltipLines,cursor,activeDots}=state.state;if(!tooltipLines||tooltipVisible&&tooltipLines.some(line=>line.text.trim()&&!line.rendered))throw new Error('Missing rendered tooltip lines.');state.state={tooltip,tooltipVisible,tooltipBox,tooltipLines,cursor,activeDots};}
}
const bytes=JSON.stringify(capture,null,2)+'\n';
await writeFile(new URL('../tests/Reference/RadarChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
