import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-negative-'+Date.now());
await fs.mkdir(workspace,{recursive:true});
for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(path.join(native,'loading-overlay-source.json')),input=await read(path.join(native,'loading-overlay-desktop.json'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Unmodified native evidence did not pass.');
const results=[];
for(const id of['shifted-baseline','shifted-viewport','retained-pointer-focus','missing-pointer-receipt']){
 const changed=structuredClone(source),pointer=structuredClone(input);
 if(id==='shifted-baseline')changed.cases.find(test=>test.id==='default').measurements[0].label.baseline+=.1;
 if(id==='shifted-viewport')changed.cases.find(test=>test.id==='default').measurements[0].portal.X+=1;
 if(id==='retained-pointer-focus')changed.interactions[0].neutralRootFocused=false;
 if(id==='missing-pointer-receipt')pointer.pointerClicks[0].receipts=[];
 await fs.writeFile(path.join(workspace,'loading-overlay-source.json'),JSON.stringify(changed));
 await fs.writeFile(path.join(workspace,'loading-overlay-desktop.json'),JSON.stringify(pointer));
 const result=execute();assert.equal(result.status,1,`Verifier accepted ${id}.`);results.push({id,rejected:true,stderr:result.stderr});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives:results},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:results.map(({id,rejected})=>({id,rejected}))}));
