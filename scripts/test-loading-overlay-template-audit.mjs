import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-template-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});
for(const file of['result.json','loading-overlay-template.json','loading-overlay-template-desktop.json','catalog-light.png','catalog-dark.png'])await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async name=>JSON.parse(await fs.readFile(path.join(native,name),'utf8'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay-template.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Original custom-template evidence failed.');
const negatives=[];
for(const id of['stale-replacement-palette','missing-custom-visual','missing-pointer-receipt','retained-root-tab-stop']){
 const file=id==='missing-pointer-receipt'?'loading-overlay-template-desktop.json':'loading-overlay-template.json';
 const original=await read(file),changed=structuredClone(original);
 if(id==='stale-replacement-palette')changed.reports.find(state=>state.id==='replaced-dictionary').cardColor='#FF483D8B';
 if(id==='missing-custom-visual')changed.reports.find(state=>state.id==='custom-light').customVisual=false;
 if(id==='missing-pointer-receipt')changed.pointerClicks.filter(click=>click.target==='PART_Portal')[0].receipts=[];
 if(id==='retained-root-tab-stop')changed.focusTrace.find(event=>event.kind==='focus-presented').IsTabStop=true;
 await fs.writeFile(path.join(workspace,file),JSON.stringify(changed));const result=execute();assert.equal(result.status,1,`Verifier accepted ${id}.`);
 negatives.push({id,rejected:true,stderr:result.stderr});await fs.writeFile(path.join(workspace,file),JSON.stringify(original));
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:negatives.map(({id,rejected})=>({id,rejected}))}));
