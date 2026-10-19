import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),source=option('--source'),output=option('--output');
const report=JSON.parse(await fs.readFile(path.join(native,'empty.json'),'utf8'));
await fs.mkdir(output,{recursive:true});const fixture=await fs.mkdtemp(path.join(output,'fixture-'));
await fs.copyFile(path.join(native,'result.json'),path.join(fixture,'result.json'));for(const file of await fs.readdir(native))if(file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(fixture,file));
const run=()=>spawnSync(process.execPath,['scripts/verify-empty.mjs','--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
await fs.writeFile(path.join(fixture,'empty.json'),JSON.stringify(report));const baseline=run();assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
const mutations=[
 ['outer-width',r=>r.frames[0].measurement.root.box.width+=.01],
 ['icon-child',r=>r.frames[0].measurement.icon.child.width+=1],
 ['body-spacing',r=>r.frames[0].measurement.body.box.y+=1],
 ['title-weight',r=>r.frames[0].measurement.title.weight=400],
 ['heading-role',r=>r.frames[0].measurement.title.heading='None'],
 ['icon-zero',r=>r.frames.find(row=>row.id==='icon-zero').measurement.icon.text=''],
 ['true-action-presence',r=>r.frames.find(row=>row.id==='action-true').measurement.action=null],
 ['whitespace-description',r=>r.frames.find(row=>row.id==='space-description').measurement.description=null],
 ['zero-parent-width',r=>r.frames.find(row=>row.id==='width-0').measurement.root.box.width=0],
 ['foreground',r=>r.frames[0].measurement.icon.color.r^=1],
 ['missing-lifecycle',r=>r.lifecycle.pop()],
 ['missing-frame',r=>r.frames.pop()]
];
const rejected=[];for(const[name,mutate]of mutations){const altered=structuredClone(report);mutate(altered);await fs.writeFile(path.join(fixture,'empty.json'),JSON.stringify(altered));assert.notEqual(run().status,0,`Audit accepted ${name}`);rejected.push(name);}
await fs.writeFile(path.join(fixture,'empty.json'),JSON.stringify(report));await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected,fixture,independentCaptureCopies:true},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
