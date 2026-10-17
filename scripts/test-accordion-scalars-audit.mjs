import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const source=option('--native'),goldens=option('--source'),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-accordion-scalars-audit-')),verifier=fileURLToPath(new URL('./verify-accordion-scalars.mjs',import.meta.url));
const files=['preference-restored.json'];for(const width of[320,640])for(const name of await fs.readdir(path.join(source,String(width))))if(/\.(json|png)$/.test(name))files.push(width+'/'+name);
async function copy(){for(const file of files){const target=path.join(temporary,file);await fs.mkdir(path.dirname(target),{recursive:true});await fs.copyFile(path.join(source,file),target);}}
const run=()=>spawnSync(process.execPath,[verifier,'--native',temporary,'--source',goldens,'--output',path.join(temporary,'audit.json')],{stdio:'ignore'}),frame=(data,id)=>data.frames.find(row=>row.scheme==='Light'&&row.id===id).measurement;
try{
 await copy();assert.equal(run().status,0,'Unmodified scalar receipt failed.');
 const cases=[
  ['320/accordion-scalars.json',data=>frame(data,'title-4').title.text='-0'],
  ['320/accordion-scalars.json',data=>frame(data,'title-4').name='-0 Consumer text'],
  ['320/accordion-scalars.json',data=>frame(data,'title-10').title.text='1E-06'],
  ['320/accordion-scalars.json',data=>frame(data,'actions-11').actions.text='1E+20'],
  ['320/accordion-scalars.json',data=>frame(data,'actions-14').header.height=468],
  ['320/accordion-scalars.json',data=>frame(data,'actions-14').title.box.height+=20],
  ['320/accordion-scalars.json',data=>frame(data,'subtitle-3').subtitle=frame(data,'subtitle-11').subtitle],
  ['320/accordion-scalars.json',data=>frame(data,'content-10').body.text='1E-06'],
  ['320/accordion-scalars.json',data=>frame(data,'content-3').body.box.height=null],
  ['320/accordion-scalars.json',data=>frame(data,'title-4').expanded=false],
  ['320/accordion-scalars.json',data=>data.frames.pop()],
  ['320/accordion-scalars.json',data=>data.frames[1]=data.frames[0]],
  ['640/accordion-scalars.json',data=>data.frames[0].width=320],
  ['640/accordion-scalars.json',data=>frame(data,'actions-2').actions=null],
  ['320/result.json',data=>data.environment.animationsEnabled=true],
  ['640/result.json',data=>data.environment.textScale=1.25],
  ['preference-restored.json',data=>data.restored=!data.original]
 ];
 for(const[index,[file,change]]of cases.entries()){await copy();const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));assert.equal(run().status,1,'Corrupt scalar receipt passed: '+index+'/'+file);}
 await copy();await fs.unlink(path.join(temporary,'320/accordion-scalar-title-4-320-Light.png'));assert.equal(run().status,1,'Missing scalar capture passed.');
 console.log(JSON.stringify({passed:true,passingControls:1,rejectedMutations:cases.length+1}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-accordion-scalars-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
