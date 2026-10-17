import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const at=process.argv.indexOf('--native');assert(at>=0&&process.argv[at+1]);
const source=path.resolve(process.argv[at+1]),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-accordion-continuity-audit-')),verifier=fileURLToPath(new URL('./verify-accordion-continuity.mjs',import.meta.url));
const files=['preference-restored.json',...['enabled','reduced'].flatMap(mode=>[`${mode}/result.json`,`${mode}/accordion-continuity.json`])];
async function copy(){for(const file of files){const target=path.join(temporary,file);await fs.mkdir(path.dirname(target),{recursive:true});await fs.copyFile(path.join(source,file),target);}}
const run=()=>spawnSync(process.execPath,[verifier,'--native',temporary,'--output',path.join(temporary,'audit.json')],{stdio:'ignore'}),retained=data=>data.lifecycle.find(row=>row.kind==='move-last-to-first');
try{
 await copy();assert.equal(run().status,0,'Unmodified continuity receipt failed.');
 const cases=[
  ['enabled/accordion-continuity.json',data=>retained(data).after.Height+=1],
  ['enabled/accordion-continuity.json',data=>retained(data).after.Angle+=.0001],
  ['enabled/accordion-continuity.json',data=>retained(data).after.ClipHeight=null],
  ['enabled/accordion-continuity.json',data=>retained(data).after.ExpansionStart+=10],
  ['enabled/accordion-continuity.json',data=>retained(data).retained=false],
  ['enabled/accordion-continuity.json',data=>retained(data).sameNode=false],
  ['enabled/accordion-continuity.json',data=>retained(data).frames=[]],
  ['enabled/accordion-continuity.json',data=>retained(data).frames=Array(4).fill(retained(data).frames[0])],
  ['enabled/accordion-continuity.json',data=>data.lifecycle[1]=data.lifecycle[0]],
  ['enabled/accordion-continuity.json',data=>retained(data).title='Stale title'],
  ['enabled/accordion-continuity.json',data=>retained(data).content='Stale body'],
  ['enabled/accordion-continuity.json',data=>retained(data).automationName='Stale name'],
  ['enabled/accordion-continuity.json',data=>retained(data).order.reverse()],
  ['enabled/accordion-continuity.json',data=>retained(data).unloads=1],
  ['enabled/accordion-continuity.json',data=>data.lifecycle[0].unloads=0],
  ['enabled/accordion-continuity.json',data=>data.lifecycle.find(row=>row.kind==='remove-reinsert').staleRejected=false],
  ['enabled/accordion-continuity.json',data=>data.lifecycle.find(row=>row.kind==='content-grow').after.NaturalHeight=53],
  ['reduced/accordion-continuity.json',data=>data.lifecycle[0].after.Fraction=.5],
  ['enabled/result.json',data=>data.environment.animationsEnabled=false],
  ['preference-restored.json',data=>data.restored=!data.original]
 ];
 for(const[index,[file,change]]of cases.entries()){await copy();const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));assert.equal(run().status,1,'Corrupt continuity receipt passed: '+index+'/'+file);}
 console.log(JSON.stringify({passed:true,passingControls:1,rejectedMutations:cases.length}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-accordion-continuity-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
