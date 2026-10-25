import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const unicode=process.argv.includes('--text');
const native=option('--native'),source=option('--source'),output=option('--output');const report=JSON.parse(await fs.readFile(path.join(native,'progress-stack.json'),'utf8'));
await fs.mkdir(output,{recursive:true});const fixture=await fs.mkdtemp(path.join(output,'fixture-'));await fs.copyFile(path.join(native,'result.json'),path.join(fixture,'result.json'));for(const file of await fs.readdir(native))if(file.endsWith('.png'))await fs.copyFile(path.join(native,file),path.join(fixture,file));
const run=()=>spawnSync(process.execPath,['scripts/verify-progress-stack.mjs',...(unicode?['--text']:[]),'--native',fixture,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
await fs.writeFile(path.join(fixture,'progress-stack.json'),JSON.stringify(report));const baseline=run();assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
const mutations=[['track-width',r=>r.frames[0].measurement.bar.box.width+=.01],['segment-width',r=>r.frames[0].measurement.segments[0].box.width+=1],['segment-color',r=>r.frames[0].measurement.segments[0].background.r^=1],['numeric-title',r=>r.frames.find(row=>row.id==='value--0').measurement.segments[0].title='Published: -0'],['nonbreaking-space',r=>r.frames.find(row=>row.id==='nonbreaking-space').measurement.items[0].label.text='Published'],['hidden-legend',r=>r.frames.find(row=>row.id==='hidden').measurement.legend={box:{x:20,y:44,width:280,height:16}}],['legend-wrap',r=>r.frames.find(row=>row.id==='width-100').measurement.items[1].box.y-=28],['empty-spacing',r=>r.frames.find(row=>row.id==='empty').measurement.root.box.height=12],['value-weight',r=>r.frames[0].measurement.items[0].value.weight=400],['label-text',r=>r.frames[0].measurement.items[0].label.text='Changed'],['interactive-pattern',r=>r.frames[0].measurement.hasRange=true],['missing-lifecycle',r=>r.lifecycle.pop()],['missing-frame',r=>r.frames.pop()]];
if(unicode){
 mutations.splice(3,5,
  ['consumer-label',r=>r.frames.find(row=>row.id==='word-joiner').labels[0]='Reviewqueue'],
  ['original-tooltip',r=>r.frames.find(row=>row.id==='zero-width-space').measurement.segments[0].title='Consumerownedpublication: 6'],
  ['grapheme-line',r=>r.frames.find(row=>row.id==='emoji').measurement.items[0].label.renderedText+='\n'],
  ['discretionary-hyphen',r=>r.frames.find(row=>row.id==='soft-hyphen').measurement.items[0].label.renderedText=r.frames.find(row=>row.id==='soft-hyphen').measurement.items[0].label.renderedText.replace('\u2010','')],
  ['cjk-min-content',r=>r.frames.find(row=>row.id==='cjk-zero').measurement.items[0].label.box.width+=1]
 );
}
const rejected=[];for(const[name,mutate]of mutations){const altered=structuredClone(report);mutate(altered);await fs.writeFile(path.join(fixture,'progress-stack.json'),JSON.stringify(altered));assert.notEqual(run().status,0,`Audit accepted ${name}`);rejected.push(name);}
await fs.writeFile(path.join(fixture,'progress-stack.json'),JSON.stringify(report));await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected,fixture,independentCaptureCopies:true},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
