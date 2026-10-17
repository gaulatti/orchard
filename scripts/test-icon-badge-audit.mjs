import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),source=option('--source'),output=option('--output');
const report=JSON.parse(await fs.readFile(path.join(native,'icon-badge.json'),'utf8'));
await fs.mkdir(output,{recursive:true});const fixture=await fs.mkdtemp(path.join(output,'fixture-'));
await fs.copyFile(path.join(native,'result.json'),path.join(fixture,'result.json'));
for(const file of await fs.readdir(native))if(file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(fixture,file));
const run=()=>spawnSync(process.execPath,['scripts/verify-icon-badge.mjs','--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
await fs.writeFile(path.join(fixture,'icon-badge.json'),JSON.stringify(report));const baseline=run();assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
const mutations=[
 ['outer-geometry',r=>r.frames[0].measurement.box.width+=.01],
 ['child-geometry',r=>r.frames[0].measurement.child.width+=1],
 ['consumer-child-color',r=>r.frames.find(row=>row.id==='child-color').measurement.childForeground.r^=1],
 ['foreground',r=>r.frames[0].measurement.foreground.r^=1],
 ['fractional-alpha',r=>r.frames.find(row=>row.id==='md-subtle').measurement.background.opacity/=2],
 ['extra-tab-stop',r=>r.frames[0].measurement.IsTabStop=true],
 ['button-semantics',r=>r.frames[0].measurement.hasInvoke=true],
 ['accessible-name',r=>r.frames.find(row=>row.id==='named').measurement.name=''],
 ['negative-zero',r=>r.frames.find(row=>row.id==='number--0').measurement.text='-0'],
 ['missing-lifecycle',r=>r.lifecycle.pop()],
 ['missing-frame',r=>r.frames.pop()]
];
const rejected=[];
for(const[name,mutate]of mutations){const altered=structuredClone(report);mutate(altered);await fs.writeFile(path.join(fixture,'icon-badge.json'),JSON.stringify(altered));const found=run();assert.notEqual(found.status,0,`Audit accepted ${name}`);rejected.push(name);}
await fs.writeFile(path.join(fixture,'icon-badge.json'),JSON.stringify(report));
await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected,fixture,retainedEvidence:true},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
