import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),audit=fileURLToPath(new URL('./verify-hidden-component-contracts.mjs',import.meta.url)),report=JSON.parse(await fs.readFile(path.join(native,'hidden-component-contracts.json'),'utf8'));
await fs.mkdir(output,{recursive:true});
try{await fs.access(path.join(output,'summary.json'));throw new Error('Preserve the completed regression evidence and choose a fresh output directory.');}catch(error){if(error.code!=='ENOENT')throw error;}
const invoke=(directory,file)=>spawnSync(process.execPath,[audit,'--native',directory,'--output',file],{encoding:'utf8'});
const baseline=invoke(native,path.join(output,'actual-baseline-audit.json'));assert.equal(baseline.status,0,baseline.stderr+baseline.stdout);
const changes=[
 ['passed',r=>r.passed=false],['hidden-window',r=>r.hiddenWindow=false],['isolation',r=>r.isolated=false],
 ['activate',r=>r.activateCalled=true],['show',r=>r.showCalled=true],['focus',r=>r.focusCalled=true],['input',r=>r.inputSent=true],['enabled-invoke',r=>r.enabledButtonInvoked=true],
 ['visible',r=>r.visible=true],['native-visible',r=>r.nativeVisible=true],['activation-event',r=>r.activationEvents=1],['no-guards',r=>r.guardChecks=0],
 ['library-hash',r=>r.librarySha256='0'.repeat(64)],['source-hash',r=>r.referenceSha256='0'.repeat(64)],['architecture',r=>r.osArchitecture=r.processArchitecture==='X64'?'Arm64':'X64'],
 ['missing-frame',r=>r.frames.pop()],['duplicate-frame',r=>r.frames[0]=structuredClone(r.frames[1])],['geometry',r=>r.frames[0].measurement.box.width+=1],
 ['name',r=>r.frames[0].measurement.label+=' corrupted'],['title',r=>r.frames[0].measurement.title+=' corrupted'],['role',r=>r.frames[0].measurement.role='Text'],
 ['stroke-color',r=>r.frames[0].measurement.strokeRgba[0]^=1],['stroke-opacity',r=>r.frames[0].measurement.strokeOpacity=.5],['stroke-width',r=>r.frames[0].measurement.strokeWidth=2],['disabled-opacity',r=>r.frames.find(f=>f.measurement.disabled).measurement.iconOpacity=1],['glyph-parts',r=>r.frames[0].measurement.paths++],
 ['missing-check',r=>r.checks.pop()],['failed-check',r=>r.checks[0].passed=false],['duplicate-check',r=>r.checks[0].id=r.checks[1].id],['extra-check',r=>r.checks.push({id:'unknown',passed:true})],
 ['failure',r=>r.failure='Injected failure'],['raster-claim',r=>r.sourceRasterParityVerified=true],['visible-claim',r=>r.visibleRuntimeVerified=true],['acceptance-claim',r=>r.nativeComponentAccepted=true]
];
const results=[];
for(const[name,mutate]of changes){const directory=path.join(output,name);await fs.mkdir(path.join(directory,'app'),{recursive:true});await fs.link(path.join(native,'app/Orchard.dll'),path.join(directory,'app/Orchard.dll'));const corrupted=structuredClone(report);mutate(corrupted);await fs.writeFile(path.join(directory,'hidden-component-contracts.json'),JSON.stringify(corrupted,null,2)+'\n');const result=invoke(directory,path.join(directory,'audit.json'));assert.notEqual(result.status,0,'Damaged receipt was accepted: '+name);results.push({name,rejected:true});}
const summary={passed:true,negativeCasesRejected:results.length,baselineFromActualNative:true,results,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,results:undefined}));
