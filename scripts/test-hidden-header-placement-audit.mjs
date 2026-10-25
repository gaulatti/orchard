import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return path.resolve(process.argv[at+1]);};
const native=option('--native'),app=option('--app'),packagePath=option('--package'),output=option('--output'),fixture=path.join(output,'fixture');
await fs.mkdir(fixture,{recursive:true});await fs.cp(path.join(native,'app-identity'),path.join(fixture,'app-identity'),{recursive:true});await fs.mkdir(path.join(fixture,'rejected'),{recursive:true});
const filenames=['header-placement.json','process-exit.json','result.json','cleanup.json','rejected/header-placement.json','rejected/process-exit.json','margin-control.png','margin-control-fixed.png','rejected/panel.png','rejected/control.png','rejected/control-fixed.png'];
const originals=new Map(await Promise.all(filenames.map(async file=>[file,await fs.readFile(path.join(native,file))])));
const restore=()=>Promise.all([...originals].map(([file,bytes])=>fs.writeFile(path.join(fixture,file),bytes)));
const mutate=async(file,change)=>{const value=JSON.parse(originals.get(file));change(value);await fs.writeFile(path.join(fixture,file),JSON.stringify(value));};
const run=()=>spawnSync(process.execPath,[fileURLToPath(new URL('./verify-hidden-header-placement.mjs',import.meta.url)),'--native',fixture,'--app',app,'--package',packagePath,'--output',path.join(fixture,'audit.json')],{encoding:'utf8',maxBuffer:1024*1024,timeout:15000});
await restore();let result=run();assert.equal(result.status,0,result.stdout+result.stderr);
const changes=[
 ['failed','header-placement.json',r=>r.passed=false],['scope','header-placement.json',r=>r.scope='wrong'],['failure','header-placement.json',r=>r.failure='unexpected'],
 ['diagnostic','header-placement.json',r=>r.diagnosticOnly=false],['guards','header-placement.json',r=>r.guardChecks=0],['activation','header-placement.json',r=>r.activations=1],['focus','header-placement.json',r=>r.focus=1],
 ...['activateCalled','showCalled','focusCalled','inputSent','nativeHeaderImplemented','sourceRasterParityVerified','nativeComponentAccepted','visibleRuntimeVerified'].map(field=>[field,'header-placement.json',r=>r[field]=true]),
 ['process','header-placement.json',r=>r.processArchitecture='Arm64'],['os','header-placement.json',r=>r.osArchitecture='Arm64'],['host','header-placement.json',r=>r.hostSha256='0'.repeat(64)],['library','header-placement.json',r=>r.librarySha256='0'.repeat(64)],
 ['missing-check','header-placement.json',r=>r.checks.pop()],['duplicate-check','header-placement.json',r=>r.checks[1]=r.checks[0]],['missing-scenario','header-placement.json',r=>r.scenarios.pop()],['surface','header-placement.json',r=>r.scenarios[0].surface.width=400],['flow','header-placement.json',r=>r.scenarios[0].desired.Height=73],
 ['visible-root','header-placement.json',r=>r.checks[0].actual.IsHostVisible=true],['scaled-root','header-placement.json',r=>r.checks[0].actual.RasterizationScale=2],['hit-test','header-placement.json',r=>r.checks[7].actual=[]],['raster-hash','header-placement.json',r=>r.scenarios[0].raster.sha256='0'.repeat(64)],['png-hash','header-placement.json',r=>r.scenarios[0].raster.pngSha256='0'.repeat(64)],['pixel','header-placement.json',r=>r.scenarios[0].raster.headerPixel='00000000'],['png-path','header-placement.json',r=>r.scenarios[0].raster.file='../elsewhere.png'],
 ['false-baseline-success','rejected/header-placement.json',r=>r.passed=true],['missing-baseline-failure','rejected/header-placement.json',r=>r.checks.find(row=>!row.passed).passed=true],['baseline-exit','rejected/process-exit.json',r=>r.exitCode=0],['exit','process-exit.json',r=>r.exitCode=1],['package','result.json',r=>r.packageSha256='0'.repeat(64)],['build','result.json',r=>r.buildPassed=false],['cleanup','cleanup.json',r=>r.removed=false]
];
const rejected=[];
for(const[name,file,change]of changes){await restore();await mutate(file,change);result=run();assert.notEqual(result.status,0,'Damaged Header investigation accepted: '+name);rejected.push(name);}
await restore();const corrupted=Buffer.from(originals.get('margin-control.png'));corrupted[50]^=1;await fs.writeFile(path.join(fixture,'margin-control.png'),corrupted);result=run();assert.notEqual(result.status,0,'Damaged native PNG accepted');rejected.push('damaged-png');
await restore();const summary={passed:true,baselineFromActualRuntime:true,negativeCasesRejected:rejected.length,rejected,nativeHeaderImplemented:false,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,rejected:undefined}));
