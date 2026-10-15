import {readFile,writeFile,unlink} from 'node:fs/promises';
import {createHash,randomUUID} from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import path from 'node:path';

if(!process.argv[2])throw Error('Pass the unchanged Sidebar browser capture JSON.');
const input=path.resolve(process.argv[2]),capture=JSON.parse(await readFile(input,'utf8'));
const repo=fileURLToPath(new URL('../../',import.meta.url)),derive=fileURLToPath(new URL('../derive-sidebar-reference.mjs',import.meta.url));
const reference=fileURLToPath(new URL('../../tests/Reference/SidebarLayout.json',import.meta.url));
const hash=async()=>createHash('sha256').update(await readFile(reference)).digest('hex'),before=await hash(),report=[];
const run=file=>spawnSync(process.execPath,[derive,file],{cwd:repo,encoding:'utf8',windowsHide:true});
const valid=run(input);if(valid.status!==0||await hash()!==before)throw Error('Valid capture does not regenerate the checked-in reference unchanged: '+valid.stderr);
for(const[name,change,error]of[
 ['missing-case',c=>c.cases.pop(),'Frozen Sidebar capture differs'],
 ['source-hash',c=>c.sourceSha256='0'.repeat(64),'Frozen Sidebar capture differs'],
 ['runtime-integrity',c=>c.bundledRuntime[0].integrity='sha512-invalid','Capture runtime does not match'],
 ['font-hash',c=>c.fontFiles[0].sha256='0'.repeat(64),'Capture font differs'],
 ['untrusted-input',c=>c.cases.find(s=>s.id==='default').states[0].state.inputEvents[0].trusted=false,'Source identity or actual input receipt failed'],
 ['changed-record',c=>c.cases.find(s=>s.id==='disabled-link').states.find(s=>s.action==='link-Enter').state.events[0].same=false,'Source identity or actual input receipt failed'],
 ['cloned-content',c=>c.cases.find(s=>s.id==='custom-link').normal.renderCalls[0].iconSame=false,'Source identity or actual input receipt failed'],
 ['missing-motion',c=>c.cases.find(s=>s.id==='motion').motionFrames=[],'Source width motion was not presented'],
 ['style-table-collision',c=>c.cases[0].normal.surface.styleIndex=0,'Capture collides with the style-table format']
]){
 const corrupted=structuredClone(capture);change(corrupted);
 const file=path.join(path.dirname(input),'derivation-guard-'+randomUUID()+'.json');
 try{
  await writeFile(file,JSON.stringify(corrupted));const result=run(file);
  if(result.status===0||!result.stderr.includes(error)||await hash()!==before)throw Error('Derivation guard failed: '+name+' '+result.stderr);
  report.push({name,rejected:true,referenceUnchanged:true});
 }finally{await unlink(file);}
}
const output=path.join(path.dirname(input),'derivation-guards-report.json');
await writeFile(output,JSON.stringify({validRegenerationUnchanged:true,referenceSha256:before,guards:report},null,2)+'\n');
console.log(JSON.stringify({validRegenerationUnchanged:true,rejected:report.length,referenceSha256:before}));
