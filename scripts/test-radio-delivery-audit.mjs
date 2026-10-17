import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),consumer=option('--consumer'),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-radio-audit-'));
const verifier=new URL('./verify-radio-delivery.mjs',import.meta.url);
const nativeFiles=['result.json',...['Light','Dark'].flatMap(theme=>[`radio-runtime-${theme}.json`,`radio-${theme}-held-keys.json`,`radio-${theme}-desktop.json`])];
const consumerFiles=['result.json','cleanup.json','consumer-radio-Light.json','consumer-radio-Dark.json'];
async function copy(){for(const [source,name,files]of [[native,'native',nativeFiles],[consumer,'consumer',consumerFiles]]){await fs.mkdir(path.join(temporary,name),{recursive:true});for(const file of files)await fs.copyFile(path.join(source,file),path.join(temporary,name,file));}}
async function mutate(folder,file,change){const target=path.join(temporary,folder,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));}
const run=()=>spawnSync(process.execPath,[fileURLToPath(verifier),'--native',path.join(temporary,'native'),'--consumer',path.join(temporary,'consumer'),'--output',path.join(temporary,'audit.json')],{stdio:'ignore'});
try{
 await copy();assert.equal(run().status,0,'Known passing receipts failed.');
 const cases=[
  ['native','radio-runtime-Light.json',data=>data.explicitKeyboardFocus=false],
  ['native','radio-runtime-Dark.json',data=>data.requests.pop()],
  ['native','radio-Light-held-keys.json',data=>data.find(row=>row.modifier!==null).modifierUps=0],
  ['native','radio-Dark-desktop.json',data=>data.keyActions[0].targetRetained=false],
  ['consumer','consumer-radio-Light.json',data=>data.keys[0].downs=0],
  ['native','result.json',data=>data.environment.textScale=1.25]
 ];
 for(const [folder,file,change]of cases){await copy();await mutate(folder,file,change);assert.equal(run().status,1,'Corrupt receipt passed: '+folder+'/'+file);}
 console.log(JSON.stringify({passed:true,passingControl:1,rejectedMutations:cases.length}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-radio-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
