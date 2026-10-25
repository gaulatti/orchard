import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const input=path.resolve(option('--native')),app=path.resolve(option('--app')),output=path.resolve(option('--output')),pkg=path.resolve(option('--package'));
const report=JSON.parse(await fs.readFile(path.join(input,'scroll-area-primitive.json')));assert.equal(report.passed,true);await fs.mkdir(output,{recursive:true});
const files=['scroll-area-primitive.json','result.json','cleanup.json','process-exit.json',...report.cases.map(r=>r.file)];
async function edit(dir,file,mutate){const p=path.join(dir,file),r=JSON.parse(await fs.readFile(p));mutate(r);await fs.writeFile(p,JSON.stringify(r));}
const change=mutate=>dir=>edit(dir,'scroll-area-primitive.json',mutate);
const tests=[
 ['failed-runtime',change(r=>r.passed=false)],
 ['failed-process',dir=>edit(dir,'process-exit.json',r=>r.exitCode=1)],
 ['activation',change(r=>r.activations=1)],
 ['input-sent',change(r=>r.inputSent=true)],
 ['missing-case',change(r=>r.cases.pop())],
 ['lost-original',change(r=>r.cases[4].samples[2].originalRetained=false)],
 ['disabled-hidden-axis',change(r=>r.cases[4].samples[1].horizontalOffset=0)],
 ['wrong-native-extent',change(r=>r.cases[4].samples[0].extentWidth=256)],
 ['overlay-gutter',change(r=>r.cases[4].samples[0].viewportWidth=256)],
 ['wrong-rtl-content',change(r=>r.cases[5].samples[0].content.x++)],
 ['wrong-thumb-size',change(r=>r.cases[4].samples[0].verticalThumb.bounds.height++)],
 ['wrong-thumb-position',change(r=>r.cases[4].samples[1].verticalThumb.bounds.y=300)],
 ['missing-scroll-event',change(r=>r.cases[2].completions=[])],
 ['natural-width-unconstrained',change(r=>r.cases[4].samples[2].content.width=596)],
 ['shrink-offset-retained',change(r=>r.cases[4].samples[3].verticalOffset=120)],
 ['stale-library',change(r=>r.librarySha256='0'.repeat(64))],
 ['stale-host',change(r=>r.hostSha256='0'.repeat(64))],
 ['wrong-architecture',change(r=>r.processArchitecture='Arm64')],
 ['stale-png-hash',change(r=>r.cases[4].pngSha256='0'.repeat(64))],
 ['missing-png',dir=>fs.unlink(path.join(dir,report.cases[4].file))],
 ['consistent-wrong-native-pixels',async dir=>{const bytes=await fs.readFile(path.join(dir,'legacy-disabled-axis.png'));await fs.writeFile(path.join(dir,'modern-vertical-content.png'),bytes);await edit(dir,'scroll-area-primitive.json',r=>r.cases[2].pngSha256=crypto.createHash('sha256').update(bytes).digest('hex'));}],
 ['unproven-component',change(r=>r.nativeScrollAreaImplemented=true)],
 ['unproven-source-contract',change(r=>r.sourceContractVerified=true)],
 ['unproven-acceptance',change(r=>r.nativeComponentAccepted=true)],
 ['stale-package',dir=>edit(dir,'result.json',r=>r.packageSha256='0'.repeat(64))],
 ['failed-cleanup',dir=>edit(dir,'cleanup.json',r=>r.removed=false)],
 ['retained-workspace',async dir=>{await edit(dir,'result.json',r=>r.isolatedConsumer=dir);await edit(dir,'cleanup.json',r=>r.isolatedConsumer=dir);}]
];
const run=dir=>spawnSync(process.execPath,['scripts/verify-hidden-scroll-area-primitive.mjs','--native',dir,'--app',app,'--package',pkg,'--output',path.join(dir,'audit.json')],{encoding:'utf8'});
const baseline=run(input);assert.equal(baseline.status,0,baseline.stderr);
const rejected=[];for(const[name,mutate]of tests){const dir=path.join(output,name);await fs.mkdir(dir);for(const file of files)await fs.copyFile(path.join(input,file),path.join(dir,file));await mutate(dir);const result=run(dir);assert.notEqual(result.status,0,'Malformed evidence accepted: '+name);assert.equal(result.signal,null);rejected.push(name);}
const receipt={passed:true,baseline:input,negativeCasesRejected:rejected.length,rejected};await fs.writeFile(path.join(output,'result.json'),JSON.stringify(receipt,null,2)+'\n');console.log(JSON.stringify(receipt));
