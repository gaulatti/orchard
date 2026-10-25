import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),audit=fileURLToPath(new URL('./verify-hidden-slider-contracts.mjs',import.meta.url)),report=JSON.parse(await fs.readFile(path.join(native,'hidden-slider-contracts.json'),'utf8'));
await fs.mkdir(output,{recursive:true});try{await fs.access(path.join(output,'summary.json'));throw new Error('Preserve completed regression evidence and choose a fresh directory.');}catch(error){if(error.code!=='ENOENT')throw error;}
const invoke=(directory,file)=>spawnSync(process.execPath,[audit,'--native',directory,'--output',file],{encoding:'utf8'});
const baseline=invoke(native,path.join(output,'actual-baseline-audit.json'));assert.equal(baseline.status,0,baseline.stdout+baseline.stderr);
const mutations=[
 ['failed',r=>r.passed=false],['isolation',r=>r.isolated=false],['activate',r=>r.activateCalled=true],['show',r=>r.showCalled=true],['focus',r=>r.focusCalled=true],['input',r=>r.inputSent=true],['visible',r=>r.visible=true],['native-visible',r=>r.nativeVisible=true],['activation-event',r=>r.activationEvents=1],['guards',r=>r.guardChecks=0],
 ['source-hash',r=>r.referenceSha256='0'.repeat(64)],['assembly-hash',r=>r.librarySha256='0'.repeat(64)],['architecture',r=>r.osArchitecture=r.processArchitecture==='X64'?'Arm64':'X64'],
 ['missing-frame',r=>r.frames.pop()],['duplicate-frame',r=>r.frames[0]=structuredClone(r.frames[1])],['thumb-geometry',r=>r.frames[0].measurement.thumb.box.x+=1],['fill-geometry',r=>r.frames[0].measurement.fill.width+=1],
 ['late-frame-fallback',r=>{r.frames[0].lateMeasurement=structuredClone(r.frames[0].measurement);r.frames[0].measurement.fill.width+=1;}],
 ['header-presence',r=>r.frames.find(f=>f.measurement.header).measurement.header=null],['display-text',r=>r.frames.find(f=>f.measurement.display).measurement.display.text+=' bad'],['range-value',r=>r.frames[0].measurement.actualValue+=1],['name',r=>r.frames[0].measurement.name+=' bad'],['role',r=>r.frames[0].measurement.role='Text'],['tab-stop',r=>r.frames[0].measurement.tabStop=false],['read-only',r=>r.frames[0].measurement.readOnly=true],
 ['track-color',r=>r.frames[0].measurement.trackPaint.r^=1],['fill-alpha',r=>r.frames[0].measurement.fillPaint.opacity=.1],['thumb-opacity',r=>r.frames.find(f=>f.measurement.disabled).measurement.thumb.opacity=1],['host-visible',r=>r.frames[0].measurement.hostVisible=true],['hidden-timeline',r=>r.frames[0].measurement.renderingActive=true],
 ['missing-history',r=>r.history.pop()],['history-width',r=>r.history[0].fillWidth+=1],['history-value',r=>r.history[0].actualValue+=1],['history-timeline',r=>r.history[0].renderingActive=true],['missing-check',r=>r.checks.pop()],['duplicate-check',r=>r.checks[0].id=r.checks[1].id],['failed-check',r=>r.checks[0].passed=false],['request-count',r=>r.enabledRangeRequests=3],
 ['failure',r=>r.failure='Injected failure'],['raster-claim',r=>r.sourceRasterParityVerified=true],['visible-claim',r=>r.visibleRuntimeVerified=true],['input-claim',r=>r.physicalInputVerified=true],['motion-claim',r=>r.normalMotionVerified=true],['acceptance-claim',r=>r.nativeComponentAccepted=true]
];
const results=[];for(const[name,mutate]of mutations){const directory=path.join(output,name);await fs.mkdir(path.join(directory,'app'),{recursive:true});await fs.link(path.join(native,'app/Orchard.dll'),path.join(directory,'app/Orchard.dll'));const corrupted=structuredClone(report);mutate(corrupted);await fs.writeFile(path.join(directory,'hidden-slider-contracts.json'),JSON.stringify(corrupted,null,2)+'\n');const result=invoke(directory,path.join(directory,'audit.json'));assert.notEqual(result.status,0,'Damaged receipt was accepted: '+name);results.push({name,rejected:true});}
const summary={passed:true,negativeCasesRejected:results.length,baselineFromActualNative:true,results,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,results:undefined}));
