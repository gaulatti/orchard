import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),output=option('--output'),auditor=fileURLToPath(new URL('./verify-hidden-paginations.mjs',import.meta.url));
await fs.mkdir(output,{recursive:true});const fixture=path.join(output,'fixture');await fs.mkdir(fixture);
const report=JSON.parse(await fs.readFile(path.join(native,'pagination.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'pagination-exit.json')));
const write=async(r,e)=>{await fs.writeFile(path.join(fixture,'pagination.json'),JSON.stringify(r));await fs.writeFile(path.join(fixture,'pagination-exit.json'),JSON.stringify(e));};
const run=()=>spawnSync(process.execPath,[auditor,'--native',fixture,'--app',app,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
await write(report,exit);let response=run();assert.equal(response.status,0,response.stdout+response.stderr);
const visible=r=>r.frames.find(row=>row.measurement.visible).measurement;
const mutations=[['scope',r=>r.scope='foreground-pagination-source-capture'],['failure',r=>r.passed=false],['visible',r=>r.visible=true],['activation',r=>r.activationEvents=1],['focus',r=>r.focusCalled=true],['input',r=>r.inputSent=true],['isolation',r=>r.isolated=false],['guards',r=>r.guardChecks=0],['library',r=>r.librarySha256='0'.repeat(64)],['reference',r=>r.referenceSha256='0'.repeat(64)],['missing-state',r=>r.frames.pop()],['duplicate-state',r=>r.frames[1]=structuredClone(r.frames[0])],['geometry',r=>visible(r).nav.x+=1],['button',r=>visible(r).buttons[1].text='Wrong'],['availability',r=>visible(r).buttons[0].disabled=!visible(r).buttons[0].disabled],['missing-check',r=>r.checks.pop()],['failed-check',r=>r.checks[0].passed=false],['raster',r=>r.sourceRasterParityVerified=true],['acceptance',r=>r.nativeComponentAccepted=true],['architecture',r=>r.osArchitecture='Wrong'],['exit',(_,e)=>e.exitCode=-1]];
mutations.push(['inner-button-fill',r=>visible(r).buttons[0].backgroundSizing='InnerBorderEdge'],['inner-presenter-fill',r=>visible(r).buttons[0].presenterBackgroundSizing='InnerBorderEdge']);
const rejected=[];for(const[name,mutate]of mutations){const r=structuredClone(report),e=structuredClone(exit);mutate(r,e);await write(r,e);response=run();assert.notEqual(response.status,0,'Damaged hidden Pagination accepted: '+name);rejected.push(name);}
const summary={passed:true,baselineFromActualPackageConsumer:true,negativeCasesRejected:rejected.length,rejected,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,rejected:undefined}));
