import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';

const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const input=path.resolve(option('--native')),output=path.resolve(option('--output')),app=path.resolve(option('--app')),host=option('--host'),reference=path.resolve(option('--reference'));
const packagePath=process.argv.includes('--package')?path.resolve(option('--package')):undefined;
const architecture=process.argv.includes('--architecture')?option('--architecture'):'X64';assert(['X64','Arm64'].includes(architecture));
const report=JSON.parse(await fs.readFile(path.join(input,'popovers.json')));
assert.equal(report.passed,true,'A verified baseline is required.');
const consumerResult=host==='PackageConsumer'?JSON.parse(await fs.readFile(path.join(input,'result.json'))):undefined;
await fs.mkdir(output,{recursive:true});
const files=['popovers.json','process-exit.json',...report.captures.map(row=>row.file),...(host==='PackageConsumer'?['result.json','cleanup.json']:[])];
const cases=[
 ['failed-runtime',async dir=>edit(dir,'popovers.json',r=>r.passed=false)],
 ['failed-process',async dir=>edit(dir,'process-exit.json',r=>r.exitCode=1)],
 ['activation',async dir=>edit(dir,'popovers.json',r=>r.activations=1)],
 ['focus',async dir=>edit(dir,'popovers.json',r=>r.focus=1)],
 ['missing-frame',async dir=>edit(dir,'popovers.json',r=>r.frames.pop())],
 ['shifted-surface',async dir=>edit(dir,'popovers.json',r=>r.frames[0].popup.box.x++)],
 ['paint-alpha',async dir=>edit(dir,'popovers.json',r=>r.frames[0].popup.paint.border.opacity=.5)],
 ['missing-contract',async dir=>edit(dir,'popovers.json',r=>r.checks.pop())],
 ['stale-reference',async dir=>edit(dir,'popovers.json',r=>r.referenceSha256='0'.repeat(64))],
 ['stale-library',async dir=>edit(dir,'popovers.json',r=>r.librarySha256='0'.repeat(64))],
 ['wrong-architecture',async dir=>edit(dir,'popovers.json',r=>r.processArchitecture=architecture==='X64'?'Arm64':'X64')],
 ['stale-capture',async dir=>edit(dir,'popovers.json',r=>r.captures[0].sha256='0'.repeat(64))],
 ['missing-capture',async dir=>fs.unlink(path.join(dir,report.captures[0].file))],
 ['unproven-acceptance',async dir=>edit(dir,'popovers.json',r=>r.nativeComponentAccepted=true)],
];
if(host==='PackageConsumer')cases.push(
 ['stale-package',async dir=>edit(dir,'result.json',r=>r.packageSha256='0'.repeat(64))],
 ['missing-cleanup',async dir=>fs.unlink(path.join(dir,'cleanup.json'))],
 ['failed-cleanup',async dir=>edit(dir,'cleanup.json',r=>r.removed=false)]
);
if(consumerResult?.runtime)cases.push(
 ['failed-consumer-runtime',async dir=>edit(dir,'result.json',r=>r.runtime.passed=false)],
 ['wrong-consumer-architecture',async dir=>edit(dir,'result.json',r=>r.runtime.architecture=architecture==='X64'?'Arm64':'X64')],
 ['stale-consumer-assembly',async dir=>edit(dir,'result.json',r=>r.runtime.assembly=path.join(dir,'missing-orchard.dll'))]
);
async function edit(dir,file,mutate){const p=path.join(dir,file),row=JSON.parse(await fs.readFile(p));mutate(row);await fs.writeFile(p,JSON.stringify(row));}
const run=dir=>spawnSync(process.execPath,['scripts/verify-hidden-popovers.mjs','--native',dir,'--app',app,'--host',host,'--architecture',architecture,'--reference',reference,'--output',path.join(dir,'audit.json'),...(packagePath?['--package',packagePath]:[])],{encoding:'utf8'});
assert.equal(run(input).status,0,'Baseline audit must pass before rejection testing.');
const rejected=[];
for(const[name,mutate]of cases){const dir=path.join(output,name);await fs.mkdir(dir);for(const file of files)await fs.copyFile(path.join(input,file),path.join(dir,file));await mutate(dir);const result=run(dir);assert.notEqual(result.status,0,'Malformed evidence was accepted: '+name);assert.equal(result.signal,null,'Audit did not finish: '+name);rejected.push(name);}
const receipt={passed:true,baseline:input,negativeCasesRejected:rejected.length,rejected};
await fs.writeFile(path.join(output,'result.json'),JSON.stringify(receipt,null,2)+'\n');console.log(JSON.stringify(receipt));
