import{readFile,writeFile}from'node:fs/promises';
import{createHash}from'node:crypto';
if(!process.argv[2])throw Error('Pass the unchanged Funnel capture JSON.');
const capture=JSON.parse(await readFile(process.argv[2],'utf8'));
if(capture.sourceCommit!=='a86059013cd59684ce452849278ea81e734f2066'||capture.sourceSha256!=='eff2a2992f0e3fa08bf78beec46bd4291dc58013fa6cbe3c58cc46c5200384af'||capture.react!=='19.2.4'||capture.recharts!=='3.8.1'||capture.cases.length!==54||new Set(capture.cases.map(c=>c.id+'/'+c.scheme)).size!==54)throw Error('Frozen Funnel capture differs.');
for(const sample of capture.cases){
 if(sample.errors.length||!['Light','Dark'].includes(sample.scheme)||!capture.cases.some(c=>c.id===sample.id&&c.scheme!==sample.scheme))throw Error('Invalid source case.');
 delete sample.normal.tooltipHtml;
 for(const state of sample.states){const{tooltip,tooltipVisible,tooltipBox,tooltipLines,cursor}=state.state;if(tooltipVisible&&tooltipLines.some(l=>l.text.trim()&&!l.rendered))throw Error('Unsettled source tooltip.');state.state={tooltip,tooltipVisible,tooltipBox,tooltipLines,cursor};}
}
const bytes=JSON.stringify(capture,null,2)+'\n';await writeFile(new URL('../tests/Reference/FunnelChartLayout.json',import.meta.url),bytes);
console.log(JSON.stringify({cases:capture.cases.length,sha256:createHash('sha256').update(bytes).digest('hex')}));
