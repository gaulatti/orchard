import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),source=option('--source'),output=option('--output');
const report=JSON.parse(await fs.readFile(path.join(native,'panel-layout.json'),'utf8'));await fs.mkdir(output,{recursive:true});const fixture=await fs.mkdtemp(path.join(output,'fixture-'));
try{
 await fs.copyFile(path.join(native,'result.json'),path.join(fixture,'result.json'));for(const file of await fs.readdir(native))if(file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(fixture,file));
 const run=()=>spawnSync(process.execPath,['scripts/verify-panel-layout.mjs','--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
 await fs.writeFile(path.join(fixture,'panel-layout.json'),JSON.stringify(report));const baseline=run();assert.equal(baseline.status,0,baseline.stdout+baseline.stderr);
 const frame=(r,id)=>r.frames.find(row=>row.id===id).measurement;
 const mutations=[
  ['fixed-width',r=>frame(r,'fixed-500.375-ltr').items[0].box.width++],
  ['grow-remainder',r=>frame(r,'three-grow-500.375-ltr').items[1].wrapper.width+=.01],
  ['rtl-order',r=>frame(r,'fixed-500.375-rtl').items[0].box.x++],
  ['column-fixed-height',r=>frame(r,'columns-500.375-ltr').items[0].box.height++],
  ['column-grow-height',r=>frame(r,'columns-500.375-ltr').items[1].box.height++],
  ['padding',r=>frame(r,'padding').items[0].box.x++],
  ['short-column',r=>frame(r,'short-column').items[1].box.height=1],
  ['hidden-child',r=>frame(r,'hidden').items[0].box.width=1],
  ['overflow-extent',r=>frame(r,'overflow-500.375-ltr').extentWidth++],
  ['added-tab-stop',r=>r.frames[0].measurement.passive=false],
  ['missing-frame',r=>r.frames.pop()],
  ['lost-grow-binding',r=>r.interactions[0].growthBinding=false],
  ['replaced-child',r=>r.interactions[0].moveRetained=false],
  ['reset-scroll',r=>r.interactions[0].scrollRetained=false],
  ['duplicate-action',r=>r.interactions[0].actions=3],
  ['stale-template',r=>r.interactions[0].templateReplacement=false],
  ['missing-keyboard-scroll',r=>r.interactions[0].keyboardScroll=0],
  ['unbounded-width',r=>r.interactions[0].intrinsicWidth=false],
  ['collection-move-unloads',r=>r.interactions[0].moveUnloads=1],
  ['collection-move-focus',r=>r.interactions[0].focusRetained=false],
  ['missing-native-hash',r=>delete r.captures[Object.keys(r.captures)[0]]],
  ['false-raster-acceptance',r=>r.sourceRasterParityVerified=!r.sourceRasterParityVerified]
 ];const rejected=[];
 for(const[name,mutate]of mutations){const changed=structuredClone(report);mutate(changed);await fs.writeFile(path.join(fixture,'panel-layout.json'),JSON.stringify(changed));assert.notEqual(run().status,0,'Audit accepted '+name);rejected.push(name);}
 await fs.writeFile(path.join(fixture,'panel-layout.json'),JSON.stringify(report));
 await fs.copyFile(path.join(native,'panel-layout-fixed-500.375-ltr-Light.png'),path.join(fixture,'panel-layout-fixed-grow-500.375-ltr-Light.png'));
 assert.notEqual(run().status,0,'Audit accepted a swapped raw native image');rejected.push('swapped-raw-native-png');
 await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
}finally{assert.equal(path.dirname(fixture),output);assert(path.basename(fixture).startsWith('fixture-'));await fs.rm(fixture,{recursive:true,force:true});}
