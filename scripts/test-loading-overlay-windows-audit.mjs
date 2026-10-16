import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-windows-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});
for(const file of['result.json','loading-overlay-windows.json','loading-overlay-windows-primary-desktop.json','loading-overlay-windows-secondary-desktop.json','catalog-light.png','catalog-dark.png'])await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async name=>JSON.parse(await fs.readFile(path.join(native,name),'utf8'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay-windows.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Original two-window evidence failed.');const negatives=[];
for(const id of['stale-source-portal','wrong-destination-window','stale-target-palette','missing-owned-key']){
 const file=id==='missing-owned-key'?'loading-overlay-windows-secondary-desktop.json':'loading-overlay-windows.json',original=await read(file),changed=structuredClone(original);
 if(id==='stale-source-portal')changed.reports.find(state=>state.id==='attach-secondary').primaryPortals=1;
 if(id==='wrong-destination-window')changed.reports.find(state=>state.id==='secondary-identical-layout').portal.window=changed.primaryWindow;
 if(id==='stale-target-palette')changed.reports.find(state=>state.id==='attach-secondary').portal.cardColor='#FFFFFFFF';
 if(id==='missing-owned-key')changed.keyActions[0].ups=0;
 await fs.writeFile(path.join(workspace,file),JSON.stringify(changed));const result=execute();assert.equal(result.status,1,`Verifier accepted ${id}.`);
 negatives.push({id,rejected:true,stderr:result.stderr});await fs.writeFile(path.join(workspace,file),JSON.stringify(original));
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:negatives.map(({id,rejected})=>({id,rejected}))}));
