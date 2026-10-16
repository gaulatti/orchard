import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),workspace=path.join(path.dirname(output),'loading-overlay-contrast-entry-negative-'+Date.now());await fs.mkdir(workspace,{recursive:true});
for(const file of await fs.readdir(native))if(file.endsWith('.json')||file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(workspace,file));
const read=async name=>JSON.parse(await fs.readFile(path.join(native,name),'utf8'));
const verifier=fileURLToPath(new URL('./verify-loading-overlay-contrast.mjs',import.meta.url));
const execute=()=>spawnSync(process.execPath,[verifier,'--native',workspace,'--output',path.join(workspace,'audit.json')],{encoding:'utf8'});
assert.equal(execute().status,0,'Original contrast-entry evidence failed.');const negatives=[];
for(const id of['missing-baseline-shadow','wrong-ordinary-label-color','entry-cannot-prove-live-contrast']){
 const report=await read('loading-overlay-contrast.json'),result=await read('result.json');
 if(id==='missing-baseline-shadow')report.reports[0].hasShadow=false;
 if(id==='wrong-ordinary-label-color')report.reports[0].palette.Text.Color='#FFFFFFFF';
 if(id==='entry-cannot-prove-live-contrast'){report.entryOnly=false;result.component='loading-overlay-contrast';}
 await fs.writeFile(path.join(workspace,'loading-overlay-contrast.json'),JSON.stringify(report));await fs.writeFile(path.join(workspace,'result.json'),JSON.stringify(result));
 const failed=execute();assert.equal(failed.status,1,`Verifier accepted ${id}.`);negatives.push({id,rejected:true,stderr:failed.stderr});
}
await fs.writeFile(output,JSON.stringify({passed:true,workspace,negatives},null,2)+'\n');console.log(JSON.stringify({passed:true,negatives:negatives.map(({id,rejected})=>({id,rejected}))}));
