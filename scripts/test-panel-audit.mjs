import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),source=option('--source'),output=option('--output');
const report=JSON.parse(await fs.readFile(path.join(native,'panel.json'),'utf8')),reference=JSON.parse(await fs.readFile(new URL('../tests/Reference/Panel.json',import.meta.url),'utf8'));
await fs.mkdir(output,{recursive:true});const fixture=await fs.mkdtemp(path.join(output,'fixture-'));
try{
 await fs.copyFile(path.join(native,'result.json'),path.join(fixture,'result.json'));for(const row of reference.cases.filter(row=>row.file))await fs.copyFile(path.join(native,row.file),path.join(fixture,row.file));
 const run=()=>spawnSync(process.execPath,['scripts/verify-panel.mjs','--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
 await fs.writeFile(path.join(fixture,'panel.json'),JSON.stringify(report));const baseline=run();assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
 const frame=(r,id)=>r.frames.find(row=>row.id===id).measurement;
 const mutations=[
  ['header-height',r=>frame(r,'320-normal-12-all').header.height++],
  ['title-wrap',r=>frame(r,'180-long-12-all').title.box.height++],
  ['title-text',r=>frame(r,'320-normal-12-all').title.painted='OTHER'],
  ['count-zero',r=>frame(r,'320-normal-0-all').count=null],
  ['count-value',r=>frame(r,'count-fraction').count.value='2'],
  ['count-kerning',r=>frame(r,'count-infinity').count.box.width+=.01],
  ['filter-presence',r=>frame(r,'false-filter').filter={x:0,y:0,width:1,height:1}],
  ['toolbar-spacing',r=>frame(r,'320-normal-12-all').toolbar.y++],
  ['body-height',r=>frame(r,'height-40').feed.height=1],
  ['scroll-offset',r=>frame(r,'scroll-400').scroll.offset++],
  ['content-width',r=>frame(r,'500.375-normal-12-all').bodies.content.width+=.01],
  ['accent-paint',r=>frame(r,'accent').paint.left.color='#FF000000'],
  ['count-alpha',r=>frame(r,'320-normal-12-all').paint.countBackground.opacity=.5],
  ['dot-opacity',r=>frame(r,'320-normal-12-all').paint.dotOpacity=1],
  ['title-name',r=>frame(r,'scalar').name='Consumer body'],
  ['content-identity',r=>r.frames[0].measurement.contentRetained=false],
  ['viewport-focus',r=>r.frames[0].measurement.viewportFocusable=false],
  ['missing-state',r=>r.frames.pop()],
  ['count-binding',r=>r.interactions[0].countBindingRetained=false],
  ['input-retention',r=>r.interactions[0].inputsRetained=false],
  ['duplicate-action',r=>r.interactions[0].actions++],
  ['template-value',r=>r.interactions[0].originalTemplateValues=false],
  ['selector',r=>r.interactions[0].selectorRetained=false],
  ['live-scroll',r=>r.interactions[0].liveScrollRetained=false],
  ['viewport-keyboard',r=>r.interactions[0].viewportKeyboard=false],
  ['keyboard-home',r=>r.interactions[0].home=10],
  ['remount',r=>r.interactions[0].remount=false],
  ['intrinsic-parent',r=>r.interactions[0].intrinsicWidth=false]
 ];
 const rejected=[];for(const [name,mutate]of mutations){const altered=structuredClone(report);mutate(altered);await fs.writeFile(path.join(fixture,'panel.json'),JSON.stringify(altered));assert.notEqual(run().status,0,`Audit accepted ${name}`);rejected.push(name);}
 await fs.writeFile(path.join(fixture,'panel.json'),JSON.stringify(report));const images=reference.cases.filter(row=>row.file);await fs.copyFile(path.join(native,images.at(-1).file),path.join(fixture,images[0].file));assert.notEqual(run().status,0,'Audit accepted a swapped raw capture');rejected.push('swapped-native-capture');
 await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
}finally{assert.equal(path.dirname(fixture),output);assert(path.basename(fixture).startsWith('fixture-'));await fs.rm(fixture,{recursive:true,force:true});}
