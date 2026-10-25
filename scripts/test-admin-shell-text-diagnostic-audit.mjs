import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const native=option('--native'),app=option('--app'),source=option('--source'),output=option('--output'),audit=fileURLToPath(new URL('./verify-admin-shell-text-diagnostic.mjs',import.meta.url));
await fs.mkdir(output,{recursive:true});const fixture=path.join(output,'fixture');await fs.mkdir(fixture);
const report=JSON.parse(await fs.readFile(path.join(native,'admin-shell-text-diagnostic.json'))),result=JSON.parse(await fs.readFile(path.join(native,'result.json'))),exit=JSON.parse(await fs.readFile(path.join(native,'process-exit.json')));
for(const file of Object.keys(report.captures))await fs.link(path.join(native,file),path.join(fixture,file));
const run=()=>spawnSync(process.execPath,[audit,'--native',fixture,'--app',app,'--source',source,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
const write=async(r,s,e)=>{for(const [name,value]of [['admin-shell-text-diagnostic.json',r],['result.json',s],['process-exit.json',e]])await fs.writeFile(path.join(fixture,name),JSON.stringify(value));};
await write(report,result,exit);let response=run();assert.equal(response.status,0,response.stdout+response.stderr);
const mutations=[['scope',r=>r.scope='foreground-admin-shell-source-capture'],['failed',r=>r.passed=false],['diagnostic',r=>r.diagnosticOnly=false],['raw',r=>r.rawClientFrames=false],['foreground',r=>r.foregroundCapture=false],['opaque',r=>r.opaqueTextProbe=false],['source-pixels',r=>r.sourcePixelsPainted=true],['production-change',r=>r.productionLibraryChanged=true],['os-settings',r=>r.osSettingsChanged=true],['narrator',r=>r.narratorLaunched=true],['raster-claim',r=>r.sourceRasterParityVerified=true],['acceptance-claim',r=>r.nativeComponentAccepted=true],['library',r=>r.librarySha256='0'.repeat(64)],['catalog',r=>r.catalogSha256='0'.repeat(64)],['reference',r=>r.referenceSha256='0'.repeat(64)],['missing-mode',r=>r.variants.pop()],['duplicate-mode',r=>r.variants[1]=structuredClone(r.variants[0])],['geometry',r=>r.variants[0].measurement.main.x+=1],['font',r=>r.variants[0].fontHash='0'.repeat(64)],['mode',r=>r.variants[1].rendering.mode='Outline'],['alpha',r=>r.variants[1].rendering.alpha='Premultiplied'],['dpi',r=>r.variants[1].rendering.dpi=120],['png-hash',r=>r.captures[Object.keys(r.captures)[0]]='0'.repeat(64)],['pixel-claim',r=>{const key=Object.keys(r.rasterMatches)[0];r.rasterMatches[key]=!r.rasterMatches[key];}],['scale',(_,s)=>s.environment.displayScale=2],['architecture',(_,s)=>s.environment.osArchitecture='Wrong'],['shutdown',(_,__,e)=>e.exitCode=-1073740940],['missing-exit',(_,__,e)=>e.exited=false]];
const custom=r=>r.variants.find(row=>row.variant==='custom-g1.0-c0.0-NaturalSymmetric').rendering;
const coverage=r=>r.variants.find(row=>row.variant==='coverage-rgb565').rendering;
mutations.push(
 ['requested-gamma',r=>custom(r).requestedGamma=2.2],
 ['requested-contrast',r=>custom(r).requestedContrast=1],
 ['applied-gamma',r=>custom(r).applied.Gamma=2.2],
 ['applied-contrast',r=>custom(r).applied.EnhancedContrast=1],
 ['applied-level',r=>custom(r).applied.ClearTypeLevel=0],
 ['applied-geometry',r=>custom(r).applied.PixelGeometry=2],
 ['applied-mode',r=>custom(r).applied.RenderingMode=4],
 ['missing-readback',r=>custom(r).applied=null],
 ['coverage-order',r=>coverage(r).coverageFormat='BGR565'],
 ['unexpected-coverage',r=>custom(r).coverageFormat='RGB565']
);
const rejected=[];for(const [name,mutate]of mutations){const changed=structuredClone(report),status=structuredClone(result),processExit=structuredClone(exit);mutate(changed,status,processExit);await write(changed,status,processExit);response=run();assert.notEqual(response.status,0,'Damaged diagnostic accepted: '+name);if(name==='shutdown')assert.match(response.stderr,/Native text process did not exit cleanly/);rejected.push(name);}
const summary={passed:true,baselineFromActualNative:true,negativeCasesRejected:rejected.length,rejected,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,rejected:undefined}));
