import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),source=option('--source'),output=option('--output'),auditor=fileURLToPath(new URL('./verify-pagination.mjs',import.meta.url));
await fs.mkdir(output,{recursive:true});const fixture=path.join(output,'fixture');await fs.mkdir(fixture);
const report=JSON.parse(await fs.readFile(path.join(native,'pagination.json'))),result=JSON.parse(await fs.readFile(path.join(native,'result.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));
for(const file of Object.keys(report.captures))await fs.link(path.join(native,file),path.join(fixture,file));
const write=async(r,s,e)=>{for(const[name,value]of[['pagination.json',r],['result.json',s],['process-exit.json',e]])await fs.writeFile(path.join(fixture,name),JSON.stringify(value));};
const run=()=>spawnSync(process.execPath,[auditor,'--native',fixture,'--app',app,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
await write(report,result,exit);let response=run();assert.equal(response.status,0,response.stdout+response.stderr);
const visible=r=>r.frames.find(row=>row.measurement.visible).measurement;
const mutations=[['scope',r=>r.scope='hidden-pagination'],['failed',r=>r.passed=false],['raw',r=>r.rawClientFrames=false],['foreground',r=>r.foregroundCapture=false],['source-pixels',r=>r.sourcePixelsPainted=true],['acceptance',r=>r.nativeComponentAccepted=true],['library',r=>r.librarySha256='0'.repeat(64)],['catalog',r=>r.catalogSha256='0'.repeat(64)],['reference',r=>r.referenceSha256='0'.repeat(64)],['missing-state',r=>r.frames.pop()],['duplicate-state',r=>r.frames[1]=structuredClone(r.frames[0])],['geometry',r=>visible(r).nav.x+=1],['button-geometry',r=>visible(r).buttons[0].box.width+=1],['label',r=>visible(r).label='Wrong'],['availability',r=>visible(r).buttons[0].disabled=!visible(r).buttons[0].disabled],['page-text',r=>visible(r).buttons[1].text='Wrong'],['png',r=>r.captures[Object.keys(r.captures)[0]]='0'.repeat(64)],['raster-claim',r=>{const key=Object.keys(r.rasterMatches)[0];r.rasterMatches[key]=!r.rasterMatches[key];}],['requests',r=>r.interactions[0].samples[0].requests[0]=99],['binding',r=>r.interactions[0].bindingRetained=false],['focus',r=>r.interactions[0].focusedButtonRetained=false],['disabled',r=>r.interactions[0].disabledRejected=false],['detached',r=>r.interactions[0].detachedRejected=false],['empty',r=>r.interactions[0].emptySpaceRemoved=false],['inferred-availability',r=>r.interactions[0].contradictoryAvailabilityRequest=1],['scale',(_,s)=>s.environment.displayScale=2],['architecture',(_,s)=>s.environment.osArchitecture='Wrong'],['exit',(_,__,e)=>e.exitCode=-1]];
for(const field of ['foreground','border','background'])mutations.push([field+'-paint',r=>visible(r).buttons[0].paint[field].color='#ff000000']);
const rejected=[];for(const[name,mutate]of mutations){const r=structuredClone(report),s=structuredClone(result),e=structuredClone(exit);mutate(r,s,e);await write(r,s,e);response=run();assert.notEqual(response.status,0,'Damaged Pagination accepted: '+name);rejected.push(name);}
// Replay a real pre-fix native PNG, keeping its hash and mismatch receipt
// internally consistent. Metadata alone must not admit the missing fill.
const previousFile=fileURLToPath(new URL('../tests/Reference/PaginationNativeRegressions/inner-edge-current-4-Dark.png',import.meta.url));
const previousBytes=await fs.readFile(previousFile),previousHash=crypto.createHash('sha256').update(previousBytes).digest('hex');
assert.equal(previousHash,'e81b84cf0dfe18b922110b2c8f8b561c5f9992bb4c3bed487b24ba913c61425e');
const replacement='pagination-current-4-Dark.png',damaged=structuredClone(report);damaged.captures[replacement]=previousHash;damaged.rasterMatches[replacement]=false;
await fs.unlink(path.join(fixture,replacement));await fs.copyFile(previousFile,path.join(fixture,replacement));await write(damaged,result,exit);response=run();assert.notEqual(response.status,0,'Actual missing-fill PNG was accepted.');assert.match(response.stderr,/transparent-border-fill/);rejected.push('actual-native-missing-fill-png');
await fs.unlink(path.join(fixture,replacement));await fs.link(path.join(native,replacement),path.join(fixture,replacement));await write(report,result,exit);response=run();assert.equal(response.status,0,response.stdout+response.stderr);
const summary={passed:true,baselineFromActualNative:true,actualPreviousNativePaintRejected:true,negativeCasesRejected:rejected.length,rejected,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,rejected:undefined}));
