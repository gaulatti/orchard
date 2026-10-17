import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const at=process.argv.indexOf('--native');assert(at>=0&&process.argv[at+1]);const source=path.resolve(process.argv[at+1]),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-accordion-audit-'));
const runtime=fileURLToPath(new URL('./verify-accordion-runtime.mjs',import.meta.url)),layout=fileURLToPath(new URL('./verify-accordion.mjs',import.meta.url)),goldens=fileURLToPath(new URL('../tests/Reference/Accordion',import.meta.url));
const folders=['enabled','reduced',...['320','639','640'].map(width=>'reference/'+width)],files=['preference-restored.json','reference/preference-restored.json'];
for(const folder of folders)for(const name of await fs.readdir(path.join(source,folder)))if(/\.(json|png)$/.test(name))files.push(folder+'/'+name);
async function copy(){for(const file of files){const target=path.join(temporary,file);await fs.mkdir(path.dirname(target),{recursive:true});await fs.copyFile(path.join(source,file),target);}}
const run=kind=>spawnSync(process.execPath,kind==='runtime'?[runtime,'--native',temporary,'--output',path.join(temporary,'runtime-audit.json')]:[layout,'--native',path.join(temporary,'reference'),'--source',goldens,'--output',path.join(temporary,'layout-audit.json')],{stdio:'ignore'});
try{
 await copy();assert.equal(run('runtime').status,0);assert.equal(run('layout').status,0);
 const frame='reference/320/accordion-reference.json';
 const cases=[
  ['layout',frame,data=>data.frames[0].rows[0].body.height=null],
  ['layout',frame,data=>data.frames[0].rows[0].title.box.width+=1],
  ['layout',frame,data=>data.frames[1]=data.frames[0]],
  ['layout',frame,data=>data.frames[0].rows[0].name='Stale label'],
  ['layout',frame,data=>data.frames[0].rows[0].subtitle.text='Stale subtitle'],
  ['layout',frame,data=>data.frames[0].border.opacity=.8],
  ['runtime','enabled/accordion-runtime-Light.json',data=>data.phases.pop()],
  ['runtime','enabled/accordion-runtime-Dark.json',data=>data.actions=2],
  ['runtime','enabled/accordion-runtime-Light.json',data=>data.templateEvidence.modelValue='Not the visible input'],
  ['runtime','reduced/accordion-runtime-Dark.json',data=>data.templateEvidence.commandInvocations=0],
  ['runtime','enabled/accordion-Light-held-keys.json',data=>data[0].downs=0],
  ['runtime','enabled/accordion-Dark-desktop.json',data=>data.keyActions[2].targetRetained=false],
  ['runtime','reduced/result.json',data=>data.environment.animationsEnabled=true],
  ['runtime','preference-restored.json',data=>data.passed=false]
 ];
 for(const[kind,file,change]of cases){await copy();const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));assert.equal(run(kind).status,1,'Corrupt Accordion receipt passed: '+kind+'/'+file);}
 console.log(JSON.stringify({passed:true,passingControls:2,rejectedMutations:cases.length}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-accordion-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
