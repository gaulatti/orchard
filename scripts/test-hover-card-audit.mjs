import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const input=path.resolve(option('--native')),output=path.resolve(option('--output')),app=path.resolve(option('--app')),host=option('--host'),reference=path.resolve(option('--reference')),architecture=option('--architecture');
assert(['X64','Arm64'].includes(architecture));const packagePath=process.argv.includes('--package')?path.resolve(option('--package')):undefined;
const report=JSON.parse(await fs.readFile(path.join(input,'hover-cards.json')));assert.equal(report.passed,true);await fs.mkdir(output,{recursive:true});
const files=['hover-cards.json','process-exit.json',...report.captures.map(r=>r.file),...(host==='PackageConsumer'?['result.json','cleanup.json']:[])];
async function edit(dir,file,mutate){const p=path.join(dir,file),r=JSON.parse(await fs.readFile(p));mutate(r);await fs.writeFile(p,JSON.stringify(r));}
const tests=[
 ['failed-runtime',dir=>edit(dir,'hover-cards.json',r=>r.passed=false)],
 ['failed-process',dir=>edit(dir,'process-exit.json',r=>r.exitCode=1)],
 ['activation',dir=>edit(dir,'hover-cards.json',r=>r.activations=1)],
 ['focus',dir=>edit(dir,'hover-cards.json',r=>r.focus=1)],
 ['missing-frame',dir=>edit(dir,'hover-cards.json',r=>r.frames.pop())],
 ['surface-offset',dir=>edit(dir,'hover-cards.json',r=>r.frames[0].surface.y++)],
 ['paint-alpha',dir=>edit(dir,'hover-cards.json',r=>r.frames[0].paint.border.opacity=.5)],
 ['missing-contract',dir=>edit(dir,'hover-cards.json',r=>r.checks.pop())],
 ['stale-reference',dir=>edit(dir,'hover-cards.json',r=>r.referenceSha256='0'.repeat(64))],
 ['stale-library',dir=>edit(dir,'hover-cards.json',r=>r.librarySha256='0'.repeat(64))],
 ['wrong-architecture',dir=>edit(dir,'hover-cards.json',r=>r.processArchitecture=architecture==='X64'?'Arm64':'X64')],
 ['stale-capture',dir=>edit(dir,'hover-cards.json',r=>r.captures[0].sha256='0'.repeat(64))],
 ['missing-capture',dir=>fs.unlink(path.join(dir,report.captures[0].file))],
 ['missing-timeline',dir=>edit(dir,'hover-cards.json',r=>r.timelines.pop())],
 ['opacity-sample',dir=>edit(dir,'hover-cards.json',r=>r.timelines[0].samples[1].opacity=.2)],
 ['animated-translation',dir=>edit(dir,'hover-cards.json',r=>r.timelines[0].samples[1].surface.y++)],
 ['hit-testing',dir=>edit(dir,'hover-cards.json',r=>r.timelines[0].samples[1].hitTestVisible=false)],
 ['unproven-input',dir=>edit(dir,'hover-cards.json',r=>r.nativeInputFocusVerified=true)],
 ['unproven-acceptance',dir=>edit(dir,'hover-cards.json',r=>r.nativeComponentAccepted=true)],
 ['wrong-injection-scope',dir=>edit(dir,'hover-cards.json',r=>r.stateInjection='public-open-prop')],
 ['wrong-native-pixels',async dir=>{const data=await fs.readFile(path.join(dir,'Light-ltr-top-open.png'));await fs.writeFile(path.join(dir,'Dark-ltr-top-open.png'),data);await edit(dir,'hover-cards.json',r=>r.captures.find(c=>c.name==='Dark-ltr-top-open').sha256=crypto.createHash('sha256').update(data).digest('hex'));}]
];
if(host==='PackageConsumer')tests.push(
 ['stale-package',dir=>edit(dir,'result.json',r=>r.packageSha256='0'.repeat(64))],
 ['missing-cleanup',dir=>fs.unlink(path.join(dir,'cleanup.json'))],
 ['failed-cleanup',dir=>edit(dir,'cleanup.json',r=>r.removed=false)],
 ['retained-temporary-workspace',async dir=>{await edit(dir,'result.json',r=>r.isolatedConsumer=dir);await edit(dir,'cleanup.json',r=>r.isolatedConsumer=dir);}]
);
const run=dir=>spawnSync(process.execPath,['scripts/verify-hidden-hover-cards.mjs','--native',dir,'--app',app,'--host',host,'--reference',reference,'--architecture',architecture,'--output',path.join(dir,'audit.json'),...(packagePath?['--package',packagePath]:[])],{encoding:'utf8'});
assert.equal(run(input).status,0,'A verified baseline is required before rejection testing.');
const rejected=[];for(const[name,mutate]of tests){const dir=path.join(output,name);await fs.mkdir(dir);for(const file of files)await fs.copyFile(path.join(input,file),path.join(dir,file));await mutate(dir);const r=run(dir);assert.notEqual(r.status,0,'Malformed evidence was accepted: '+name);assert.equal(r.signal,null,'Audit did not finish: '+name);rejected.push(name);}
const receipt={passed:true,baseline:input,negativeCasesRejected:rejected.length,rejected};await fs.writeFile(path.join(output,'result.json'),JSON.stringify(receipt,null,2)+'\n');console.log(JSON.stringify(receipt));
