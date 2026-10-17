import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const at=process.argv.indexOf('--native');assert(at>=0&&process.argv[at+1]);
const source=path.resolve(process.argv[at+1]),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-radio-continuity-audit-'));
const verifier=fileURLToPath(new URL('./verify-radio-continuity.mjs',import.meta.url));
const files=['preference-restored.json',...['enabled','reduced'].flatMap(mode=>[`${mode}/radio-continuity/result.json`,`${mode}/radio-continuity/radio-continuity.json`])];
async function copy(){for(const file of files){const target=path.join(temporary,file);await fs.mkdir(path.dirname(target),{recursive:true});await fs.copyFile(path.join(source,file),target);}}
const run=()=>spawnSync(process.execPath,[verifier,'--native',temporary,'--output',path.join(temporary,'audit.json')],{stdio:'ignore'});
try{
 await copy();assert.equal(run().status,0,'Unmodified continuity receipt failed');
 const enabled='enabled/radio-continuity/radio-continuity.json',reduced='reduced/radio-continuity/radio-continuity.json';
 const cases=[
  [enabled,data=>data.lifecycle[1].retained=false],
  [enabled,data=>data.lifecycle[1].after.DotStart+=1],
  [enabled,data=>data.lifecycle[0].sameNode=false],
  [enabled,data=>data.lifecycle[0].order.reverse()],
  [enabled,data=>data.lifecycle[2].label='Stale daily'],
  [enabled,data=>data.lifecycle[2].automationName='Stale daily'],
  [enabled,data=>data.lifecycle[2].selected=false],
  [enabled,data=>data.lifecycle[0].loads=0],
  [enabled,data=>data.lifecycle[1]=data.lifecycle[0]],
  [enabled,data=>data.requests=1],
  [enabled,data=>data.reversals[0].steps[0].immediate.DotDuration+=1],
  [enabled,data=>data.reversals[0].steps[0].frames=[]],
  [enabled,data=>data.reversals[0].steps[0].frames[0].PaintedAt=null],
  [enabled,data=>data.reversals[0].steps[0].frames[0].Border='#FF000000'],
  [reduced,data=>data.lifecycle[0].after.Animating=true],
  ['enabled/radio-continuity/result.json',data=>data.environment.animationsEnabled=false],
  ['preference-restored.json',data=>data.passed=false]
 ];
 for(const[file,change]of cases){await copy();const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));assert.equal(run().status,1,'Corrupt continuity receipt passed: '+file);}
 console.log(JSON.stringify({passed:true,passingControl:1,rejectedMutations:cases.length}));
}finally{
 assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-radio-continuity-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});
}
