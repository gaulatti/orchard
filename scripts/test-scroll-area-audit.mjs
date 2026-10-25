import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const input=path.resolve(option('--native')),app=path.resolve(option('--app')),output=path.resolve(option('--output')),host=option('--host'),reference=path.resolve(option('--reference')),architecture=option('--architecture'),pkg=host==='PackageConsumer'?path.resolve(option('--package')):undefined;
const report=JSON.parse(await fs.readFile(path.join(input,'scroll-areas.json')));assert.equal(report.passed,true);await fs.mkdir(output,{recursive:true});
const files=['scroll-areas.json','process-exit.json',...report.captures.map(r=>r.file),...(pkg?['result.json','cleanup.json']:[])];
async function edit(dir,file,mutate){const p=path.join(dir,file),r=JSON.parse(await fs.readFile(p));mutate(r);await fs.writeFile(p,JSON.stringify(r));}
const change=mutate=>dir=>edit(dir,'scroll-areas.json',mutate);
const tests=[
 ['failed-runtime',change(r=>r.passed=false)],
 ['failed-process',dir=>edit(dir,'process-exit.json',r=>r.exitCode=1)],
 ['activation',change(r=>r.activations=1)],
 ['focus',change(r=>r.focus=1)],
 ['input-sent',change(r=>r.inputSent=true)],
 ['missing-layout',change(r=>r.frames.pop())],
 ['wrong-content-position',change(r=>r.frames[0].measurement.content.x++)],
 ['lost-original',change(r=>r.frames[0].measurement.originalRetained=false)],
 ['overlay-gutter',change(r=>r.frames[0].measurement.client.width=256)],
 ['missing-history',change(r=>r.histories.pop())],
 ['disabled-hidden-axis',change(r=>r.histories[0].samples[1].measurement.scroll.left=0)],
 ['lost-editor',change(r=>r.histories[0].originalEditorRetained=false)],
 ['missing-scroll-events',change(r=>r.histories[0].scrollEventsObserved=0)],
 ['wrong-hover-opacity',change(r=>r.paints[1].thumbs[0].fill.opacity=.3)],
 ['wrong-dark-token',change(r=>r.paints[6].thumbs[0].fill.color='#FFE6D5B8')],
 ['wrong-capsule-radius',change(r=>r.paints[0].thumbs[0].radius=99)],
 ['old-elliptical-native-thumb',async dir=>{const bytes=await fs.readFile('tests/Fixtures/ScrollAreaNativeEllipse.png');await fs.writeFile(path.join(dir,'Light-vertical-rest.png'),bytes);await edit(dir,'scroll-areas.json',r=>r.captures.find(c=>c.name==='Light-vertical-rest').sha256=crypto.createHash('sha256').update(bytes).digest('hex'));}],
 ['missing-contract',change(r=>r.checks.pop())],
 ['stale-reference',change(r=>r.referenceSha256='0'.repeat(64))],
 ['stale-library',change(r=>r.librarySha256='0'.repeat(64))],
 ['stale-host',change(r=>r.hostSha256='0'.repeat(64))],
 ['wrong-architecture',change(r=>r.processArchitecture=architecture==='X64'?'Arm64':'X64')],
 ['stale-png-hash',change(r=>r.captures[0].sha256='0'.repeat(64))],
 ['missing-png',dir=>fs.unlink(path.join(dir,report.captures[0].file))],
 ['consistent-wrong-native-pixels',async dir=>{const bytes=await fs.readFile(path.join(dir,'Light-vertical-rest.png'));await fs.writeFile(path.join(dir,'Dark-vertical-rest.png'),bytes);await edit(dir,'scroll-areas.json',r=>r.captures.find(c=>c.name==='Dark-vertical-rest').sha256=crypto.createHash('sha256').update(bytes).digest('hex'));}],
 ['unproven-input-delivery',change(r=>r.nativeInputFocusVerified=true)],
 ['unproven-acceptance',change(r=>r.nativeComponentAccepted=true)],
 ['wrong-injection-scope',change(r=>r.stateInjection='physical-keyboard-and-mouse')]
];
if(pkg)tests.push(['stale-package',dir=>edit(dir,'result.json',r=>r.packageSha256='0'.repeat(64))],['missing-cleanup',dir=>fs.unlink(path.join(dir,'cleanup.json'))],['failed-cleanup',dir=>edit(dir,'cleanup.json',r=>r.removed=false)],['retained-workspace',async dir=>{await edit(dir,'result.json',r=>r.isolatedConsumer=dir);await edit(dir,'cleanup.json',r=>r.isolatedConsumer=dir);}]);
const run=dir=>spawnSync(process.execPath,['scripts/verify-hidden-scroll-areas.mjs','--native',dir,'--app',app,'--host',host,'--architecture',architecture,'--reference',reference,'--output',path.join(dir,'audit.json'),...(pkg?['--package',pkg]:[])],{encoding:'utf8'});
const baseline=run(input);assert.equal(baseline.status,0,baseline.stderr);
const rejected=[];for(const[name,mutate]of tests){const dir=path.join(output,name);await fs.mkdir(dir);for(const file of files)await fs.copyFile(path.join(input,file),path.join(dir,file));await mutate(dir);const result=run(dir);assert.notEqual(result.status,0,'Malformed evidence accepted: '+name);assert.equal(result.signal,null);rejected.push(name);}
const receipt={passed:true,baseline:input,negativeCasesRejected:rejected.length,rejected};await fs.writeFile(path.join(output,'result.json'),JSON.stringify(receipt,null,2)+'\n');console.log(JSON.stringify(receipt));
