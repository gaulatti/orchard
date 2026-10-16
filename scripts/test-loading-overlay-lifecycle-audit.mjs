import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-lifecycle-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});
for(const file of['result.json','loading-overlay-lifecycle.json','loading-overlay-lifecycle-desktop.json','catalog-light.png','catalog-dark.png'])await fs.copyFile(path.join(native,file),path.join(workspace,file));
const report=JSON.parse(await fs.readFile(path.join(native,'loading-overlay-lifecycle.json'),'utf8'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay-lifecycle.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Original lifecycle evidence failed.');
const negatives=[];
for(const id of['retained-root-tab-stop','shifted-resize-portal','stale-dark-palette']){
 const changed=structuredClone(report);
 if(id==='retained-root-tab-stop')changed.reports.at(-1).rootTabStop=true;
 if(id==='shifted-resize-portal')changed.reports.find(test=>test.id==='resize-320').portals[0].x+=1;
 if(id==='stale-dark-palette')changed.reports.find(test=>test.id==='live-theme-Dark').portals[0].cardColor='#FFFFFFFF';
 await fs.writeFile(path.join(workspace,'loading-overlay-lifecycle.json'),JSON.stringify(changed));
 const result=execute();assert.equal(result.status,1,`Verifier accepted ${id}.`);negatives.push({id,rejected:true,stderr:result.stderr});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:negatives.map(({id,rejected})=>({id,rejected}))}));
