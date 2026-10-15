import{readFile,writeFile}from'node:fs/promises';
import path from'node:path';
import{validateCapture,internCapture}from'../derive-data-table-toolbar-reference.mjs';
if(!process.argv[2])throw Error('Pass the unchanged full toolbar browser capture JSON.');
const file=path.resolve(process.argv[2]),dir=path.dirname(file),capture=JSON.parse(await readFile(file,'utf8'));
await validateCapture(capture,dir);internCapture(capture);
const find=(value,id)=>value.cases.find(c=>c.id===id&&c.scheme==='Light');
function* nodes(node){yield node;for(const child of node.children??[])yield*nodes(child);}
const mutations=[
 ['partial-probe',c=>c.cases.splice(10)],
 ['source-pin',c=>c.sourceSha256='0'.repeat(64)],
 ['duplicate-case',c=>c.cases[1]=structuredClone(c.cases[0])],
 ['fixture-props',c=>find(c,'default').props.searchColumn='status'],
 ['runtime-table',c=>c.bundledRuntime=c.bundledRuntime.filter(p=>p.name!=='@tanstack/react-table')],
 ['untrusted-input',c=>find(c,'default').states.find(s=>s.state.inputEvents.length).state.inputEvents[0].trusted=false],
 ['cloned-row',c=>find(c,'default').normal.table.rows[0].same=false],
 ['wrong-visible-rows',c=>find(c,'default').normal.visibleRows='invented'],
 ['controlled-input',c=>[...nodes(find(c,'default').normal.surface)].find(n=>n.tag==='INPUT').value='invented'],
 ['global-reset',c=>find(c,'global-filter').states.find(s=>s.action==='reset-release').state.table.state.globalFilter={$undefined:true}],
 ['initial-reset',c=>find(c,'initial-filter').states.find(s=>s.action==='reset-release').state.table.state.columnFilters=[]],
 ['paint-gate',c=>find(c,'default').normalPresentation.stable=0],
 ['screenshot',c=>find(c,'default').screenshots[0].sha256='0'.repeat(64)]
];
const results=[];
for(const[name,change]of mutations){const candidate=structuredClone(capture);change(candidate);let reason;try{await validateCapture(candidate,dir);}catch(error){reason=error.message;}if(!reason)throw Error('Accepted invalid toolbar capture: '+name);results.push({name,rejected:true,reason});}
await writeFile(path.join(dir,'derivation-guards-report.json'),JSON.stringify({passed:true,guards:results},null,2)+'\n');console.log(JSON.stringify({passed:true,guards:results}));
