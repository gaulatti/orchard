import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=option('--native'),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-radio-motion-audit-')),verifier=fileURLToPath(new URL('./verify-radio-motion.mjs',import.meta.url));
const files=['preference-restored.json',...['enabled','reduced'].flatMap(mode=>[`${mode}/radio-motion/result.json`,`${mode}/radio-motion/radio-motion.json`,`${mode}/radio-group/result.json`,`${mode}/radio-group/radio-runtime-Light.json`,`${mode}/radio-group/radio-runtime-Dark.json`])];
async function copy(){for(const file of files){await fs.mkdir(path.dirname(path.join(temporary,file)),{recursive:true});await fs.copyFile(path.join(source,file),path.join(temporary,file));}}
async function mutate(file,change){const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));}
const run=()=>spawnSync(process.execPath,[verifier,'--native',temporary,'--output',path.join(temporary,'audit.json')],{stdio:'ignore'});
try{
 await copy();assert.equal(run().status,0);
 const cases=[
  ['enabled/radio-motion/radio-motion.json',data=>data.reports[0].frames[0].Border='#FF000000'],
  ['enabled/radio-motion/radio-motion.json',data=>data.reports[0].frames[0].Scale=2],
  ['enabled/radio-motion/radio-motion.json',data=>data.reports[0].frames=[]],
  ['enabled/radio-motion/radio-motion.json',data=>data.availability[0].retained=false],
  ['enabled/radio-motion/radio-motion.json',data=>data.reports[0].frames[0].PaintedAt=null],
  ['enabled/radio-motion/radio-motion.json',data=>data.reports[1]=data.reports[0]],
  ['reduced/radio-group/radio-runtime-Light.json',data=>data.livePalette=false],
  ['preference-restored.json',data=>data.passed=false]
 ];
 for(const[file,change]of cases){await copy();await mutate(file,change);assert.equal(run().status,1,'Corrupt motion receipt passed: '+file);}
 console.log(JSON.stringify({passed:true,passingControl:1,rejectedMutations:cases.length}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-radio-motion-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
