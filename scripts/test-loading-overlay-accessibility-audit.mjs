import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-accessibility-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});
for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async name=>JSON.parse(await fs.readFile(path.join(native,name),'utf8'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay-accessibility.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Original accessibility evidence failed.');
const negatives=[];
for(const id of['stale-external-name','lost-original-focus','retained-hidden-peer']){
 const file=id==='lost-original-focus'?'loading-overlay-accessibility.json':id==='stale-external-name'?'uia-live-bound-name-Light.json':'uia-hidden-Dark.json';
 const original=await read(file),changed=structuredClone(original);
 if(id==='stale-external-name')changed.progress[0].name='Stale operation';
 if(id==='lost-original-focus')changed.reports.find(state=>state.id==='explicit-name').focusPreserved=false;
 if(id==='retained-hidden-peer')changed.progress=(await read('uia-default-Dark.json')).progress;
 await fs.writeFile(path.join(workspace,file),JSON.stringify(changed));
 const result=execute();assert.equal(result.status,1,`Verifier accepted ${id}.`);negatives.push({id,rejected:true,stderr:result.stderr});
 await fs.writeFile(path.join(workspace,file),JSON.stringify(original));
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:negatives.map(({id,rejected})=>({id,rejected}))}));
