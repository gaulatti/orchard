import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditTimeline} from './verify-timeline.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],`Pass ${name} <value>.`);return process.argv[at+1];};
const native=path.resolve(option('--native')),viewport=Number(option('--viewport')),output=path.resolve(option('--output'));
await auditTimeline(native,viewport);
const directory=await fs.mkdtemp(path.join(path.dirname(native),'timeline-negative-'));
const results=[];
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
for(const name of['fractional-viewport','unrestored-parent','wrong-baseline','wrong-client-pixels']){
 const target=path.join(directory,name);await fs.cp(native,target,{recursive:true});
 if(name==='fractional-viewport'||name==='wrong-client-pixels'){
  const file=path.join(target,'timeline-viewport-Light.json'),value=await read(file);
  if(name==='fractional-viewport')value.reports[0].frame.x+=.5;
  else value.reports[0].clientFile=value.reports[1].clientFile;
  await fs.writeFile(file,JSON.stringify(value,null,2)+'\n');
 }else if(name==='unrestored-parent'){
  const file=path.join(target,'timeline-viewport-restored-Light.json'),value=await read(file);value.restored=false;
  await fs.writeFile(file,JSON.stringify(value,null,2)+'\n');
 }else{
  const file=path.join(target,`timeline-source-${viewport}-Light.json`),value=await read(file);value.cases[0].rows[0].titleTypography.baseline+=1;
  await fs.writeFile(file,JSON.stringify(value,null,2)+'\n');
 }
 let failure;try{await auditTimeline(target,viewport);}catch(error){failure=error;}
 assert(failure instanceof assert.AssertionError,`${name} was not rejected by an assertion.`);
 if(name==='wrong-client-pixels')assert(failure.message.includes('Viewport crop changed original client pixels.'));
 results.push({name,rejected:true,message:failure.message.split('\n')[0]});
}
await fs.writeFile(output,JSON.stringify({passed:true,retainedInputs:directory,results},null,2)+'\n');
console.log(JSON.stringify({passed:true,negativeCases:results.length,retainedInputs:directory}));
