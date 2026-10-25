import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--capture'),output=option('--output'),reference=option('--reference');
const audit=fileURLToPath(new URL('./verify-hidden-selects.mjs',import.meta.url));
const report=JSON.parse(await fs.readFile(path.join(native,'hidden-selects.json')));
await fs.mkdir(output,{recursive:true});try{await fs.access(path.join(output,'summary.json'));throw new Error('Preserve completed evidence and choose a fresh directory.');}catch(error){if(error.code!=='ENOENT')throw error;}
const invoke=(directory,file)=>spawnSync(process.execPath,[audit,'--capture',directory,'--reference',reference,'--output',file],{encoding:'utf8'});
const baseline=invoke(native,path.join(output,'actual-baseline-audit.json'));assert.equal(baseline.status,0,baseline.stdout+baseline.stderr);
const mutations=[
 ['failed',r=>r.passed=false],['isolation',r=>r.isolated=false],['activate',r=>r.activateCalled=true],['show',r=>r.showCalled=true],['focus',r=>r.focusCalled=true],['input',r=>r.inputSent=true],['visible',r=>r.visible=true],['native-visible',r=>r.nativeVisible=true],['activation-event',r=>r.activationEvents=1],['guards',r=>r.guardChecks=0],['source-hash',r=>r.referenceSha256='0'.repeat(64)],['library-hash',r=>r.librarySha256='0'.repeat(64)],['architecture',r=>r.osArchitecture='Wrong'],['missing-frame',r=>r.frames.pop()],['duplicate-frame',r=>r.frames[0]=structuredClone(r.frames[1])],
 ['trigger',r=>r.frames[0].measurement.trigger.height+=1],['value-box',r=>r.frames[0].measurement.value.box.width+=1],['value-text',r=>r.frames[0].measurement.value.text='Wrong'],['arrow',r=>r.frames[0].measurement.arrow.x+=1],['icon',r=>r.frames.find(f=>f.measurement.icon).measurement.icon.width+=1],['border',r=>r.frames[0].measurement.paint.border.opacity=0],['background',r=>r.frames[0].measurement.paint.background.color='#ff000000'],['opacity',r=>r.frames[0].measurement.paint.opacity=0],['font',r=>r.frames[0].measurement.fontSize+=1],['padding',r=>r.frames[0].measurement.padding[0]+=1],['name',r=>r.frames[0].measurement.name='Wrong'],['required',r=>r.frames[0].measurement.required=true],['error',r=>r.frames[0].measurement.error=true],['role',r=>r.frames[0].measurement.role='Button'],['raw-value',r=>r.frames[0].measurement.rawValue='Wrong'],['option-count',r=>r.frames[0].measurement.optionCount+=1],['expanded',r=>r.frames[0].measurement.expanded='Expanded'],['native-focus',r=>r.frames[0].measurement.focused='Programmatic'],['host-visible',r=>r.frames[0].measurement.hostVisible=true],
 ['missing-history',r=>r.histories.pop()],['duplicate-history',r=>r.histories[0]=structuredClone(r.histories[1])],['request',r=>r.histories[0].requests[0]='a'],['internal-history',r=>r.histories[0].released='Select…'],['history-binding',r=>r.histories[0].bindingRetained=false],['history-popup',r=>r.histories[0].popupOpen=true],['missing-check',r=>r.checks.pop()],['duplicate-check',r=>r.checks[0].id=r.checks[1].id],['failed-check',r=>r.checks[0].passed=false],['diagnostic',r=>r.diagnostics.push('Injected')],['failure',r=>r.failure='Injected'],['raster-claim',r=>r.sourceRasterParityVerified=true],['visible-claim',r=>r.visibleRuntimeVerified=true],['acceptance-claim',r=>r.nativeComponentAccepted=true]
];
const results=[];
for(const[name,mutate]of mutations)
{
 const directory=path.join(output,name);await fs.mkdir(path.join(directory,'app'),{recursive:true});await fs.link(path.join(native,'app/Orchard.dll'),path.join(directory,'app/Orchard.dll'));
 const corrupted=structuredClone(report);mutate(corrupted);await fs.writeFile(path.join(directory,'hidden-selects.json'),JSON.stringify(corrupted));
 const result=invoke(directory,path.join(directory,'audit.json'));assert.notEqual(result.status,0,'Damaged receipt accepted: '+name);results.push({name,rejected:true});
}
const summary={passed:true,negativeCasesRejected:results.length,baselineFromActualNative:true,results,visibleRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,results:undefined}));
