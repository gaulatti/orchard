import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import {spawnSync} from 'node:child_process';import {fileURLToPath} from 'node:url';
import crypto from 'node:crypto';import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return path.resolve(process.argv[i+1]);};
const native=option('--native'),source=option('--source'),app=option('--app'),output=option('--output'),audit=fileURLToPath(new URL('./verify-admin-shell.mjs',import.meta.url));
const report=JSON.parse(await fs.readFile(path.join(native,'admin-shell.json'))),result=JSON.parse(await fs.readFile(path.join(native,'result.json')));
await fs.mkdir(output,{recursive:true});try{await fs.access(path.join(output,'summary.json'));throw new Error('Choose a fresh audit regression folder.');}catch(error){if(error.code!=='ENOENT')throw error;}
const fixture=path.join(output,'fixture');await fs.mkdir(fixture);for(const file of Object.keys(report.captures))await fs.link(path.join(native,file),path.join(fixture,file));
const run=()=>spawnSync(process.execPath,[audit,'--native',fixture,'--source',source,'--app',app,'--output',path.join(fixture,'audit.json')],{encoding:'utf8'});
const write=async(r,s)=>{await fs.writeFile(path.join(fixture,'admin-shell.json'),JSON.stringify(r));await fs.writeFile(path.join(fixture,'result.json'),JSON.stringify(s));};
await write(report,result);const baseline=run();assert.equal(baseline.status,0,baseline.stdout+baseline.stderr);
const first=r=>r.frames[0].measurement,scrolled=r=>r.frames.find(f=>f.scrolled).scrolled;
const mutations=[['failed',r=>r.passed=false],['scope',r=>r.scope='wrong'],['source-hash',r=>r.referenceSha256='0'.repeat(64)],['library-hash',r=>r.librarySha256='0'.repeat(64)],['catalog-hash',r=>r.catalogSha256='0'.repeat(64)],['foreground',r=>r.foregroundCapture=false],['raw-client',r=>r.rawClientFrames=false],['painted-source',r=>r.sourcePixelsPainted=true],['os-settings',r=>r.osSettingsChanged=true],['narrator',r=>r.narratorLaunched=true],['false-acceptance',r=>r.nativeComponentAccepted=true],['raster-claim',r=>r.sourceRasterParityVerified=!r.sourceRasterParityVerified],['resting-phase',r=>r.scrolledRestMilliseconds=0],['missing-state',r=>r.frames.pop()],['duplicate-state',r=>r.frames[0]=structuredClone(r.frames[1])],['shell-box',r=>first(r).shell.height+=1],['row-box',r=>first(r).row.y+=1],['main-box',r=>first(r).main.width+=1],['main-clip',r=>first(r).mainClip.width+=1],['content-box',r=>first(r).content.height+=1],['minimum',r=>first(r).minimum+=1],['landmark',r=>first(r).mainLandmark='None'],['unloaded',r=>first(r).IsLoaded=false],['width-extent',r=>first(r).scroll.extentWidth+=1],['missing-scrolled',r=>r.frames.find(f=>f.scrolled).scrolled=null],['scrolled-position',r=>scrolled(r).content.x+=1],['scrolled-offset',r=>scrolled(r).scroll.horizontalOffset=0],['scrolled-sidebar',r=>scrolled(r).sidebar.y+=1],['missing-capture-hash',r=>delete r.captures[Object.keys(r.captures)[0]]],['wrong-capture-hash',r=>r.captures[Object.keys(r.captures)[0]]='0'.repeat(64)],['missing-raster-receipt',r=>delete r.rasterMatches[Object.keys(r.rasterMatches)[0]]],['wrong-raster-receipt',r=>{const name=Object.keys(r.rasterMatches)[0];r.rasterMatches[name]=!r.rasterMatches[name];}],['component',(_,s)=>s.component='wrong'],['architecture',(_,s)=>s.environment.osArchitecture='wrong'],['scale',(_,s)=>s.environment.displayScale=2],['text-scale',(_,s)=>s.environment.textScale=2]];
mutations.push(['owned-footer-decoration-rounding',r=>r.frames[0].measurement.footerDecorationRoundingEnabled=false]);
const rejected=[];for(const [name,mutate]of mutations){const changed=structuredClone(report),status=structuredClone(result);mutate(changed,status);await write(changed,status);const response=run();assert.notEqual(response.status,0,'Corrupted foreground receipt accepted: '+name);rejected.push(name);}
const reference=JSON.parse(await fs.readFile(new URL('../tests/Reference/AdminShell.json',import.meta.url))),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
for(const target of reference.cases.filter(row=>row.id==='fractional')){
 const original=await fs.readFile(path.join(native,target.file)),wanted=readRgbaPng(original);let replacement;
 for(const row of reference.cases.filter(row=>row.file&&row.file!==target.file&&row.spec.viewport.width===target.spec.viewport.width&&row.spec.viewport.height===target.spec.viewport.height)){
  const bytes=await fs.readFile(path.join(native,row.file)),pixels=readRgbaPng(bytes);if(!pixels.rgba.equals(wanted.rgba)){replacement=bytes;break;}
 }
 assert(replacement,'A different complete raw client image is required.');
 // Break only the test fixture's hard link before replacing its pixels.
 const fixtureFile=path.join(fixture,target.file);await fs.unlink(fixtureFile);await fs.writeFile(fixtureFile,replacement);
 const changed=structuredClone(report);changed.captures[target.file]=hash(replacement);changed.rasterMatches[target.file]=false;changed.sourceRasterParityVerified=false;
 await write(changed,result);const response=run();assert.notEqual(response.status,0,'Forged fractional pixels accepted.');assert.match(response.stderr,/AdminShell fractional surface pixels differ/);
 rejected.push('fractional-surface-pixels-'+target.scheme);await fs.writeFile(fixtureFile,original);assert.equal(hash(await fs.readFile(path.join(native,target.file))),hash(original),'Native evidence was modified.');
}
const summary={passed:true,baselineFromActualForeground:true,negativeCasesRejected:rejected.length,rejected,sourceRasterParityVerified:false,nativeComponentAccepted:false};await fs.writeFile(path.join(output,'summary.json'),JSON.stringify(summary,null,2)+'\n');console.log(JSON.stringify({...summary,rejected:undefined}));


