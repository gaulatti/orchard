import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export const tableMinimumWidths={
 'plain-rich':240,'original-action':240,'fixed-checkbox':240,
 'scroll-auto':240,'scroll-fixed':240,'scroll-restored-auto':240,'scroll-minimum':392,'scroll-restored-minimum':240,
 'caller-override-initial':372,'caller-override-grown':452,'caller-override-shrunk':240,'caller-template':346
};
export function verifyTableMinimumReport(r){
 assert.equal(r.format,'orchard-table-minimum-native-v1');assert.equal(r.passed,true);assert.equal(r.publicApisOnly,true);
 for(const k of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(r[k],false,k);
 assert.equal(r.focus,0);assert.equal(r.activations,0);assert(Number.isInteger(r.guards)&&r.guards>=144);
 for(const k of ['librarySha256','hostSha256'])assert.match(r[k],/^[a-f0-9]{64}$/);
 assert.equal(r.states.length,24);assert.equal(r.pngs.length,24);const names=new Set();
 for(const s of r.states){
  assert(['Light','Dark'].includes(s.scheme));assert(Object.hasOwn(tableMinimumWidths,s.name));const key=s.scheme+'/'+s.name;assert(!names.has(key));names.add(key);
  const expected=tableMinimumWidths[s.name];assert.equal(s.expectedWidth,expected);assert(Number.isFinite(s.cellWidth)&&Math.abs(s.cellWidth-expected)<=.05);
  assert.equal(s.expectedOverflow,expected>240);assert(Number.isFinite(s.scrollableWidth));assert(expected>240?s.scrollableWidth>expected-241:s.scrollableWidth>=0&&s.scrollableWidth<=.05);
  for(const k of ['originalContentRetained','originalContentLoaded','callerTemplateRetained'])assert.equal(s[k],true,k);
  if(s.name.startsWith('scroll-'))assert(Number.isFinite(s.viewportScrollableWidth)&&s.viewportScrollableWidth>0);else assert.equal(s.viewportScrollableWidth,null);
  const minimum={'caller-override-initial':340,'caller-override-grown':420,'caller-override-shrunk':160,'caller-template':340}[s.name];
  if(minimum!==undefined){assert.equal(s.callerMinimum,minimum);assert(Number.isInteger(s.callerZeroMeasures)&&s.callerZeroMeasures>0);}else{assert.equal(s.callerMinimum,null);assert.equal(s.callerZeroMeasures,null);}
  assert(Array.isArray(s.types)&&s.types.length>0);for(const t of s.types){assert.equal(typeof t.type,'string');assert(Number.isInteger(t.count)&&t.count>0);}
  assert.equal(s.file,`table-minimum-${s.scheme}-${s.name}.png`);
 }
 for(const scheme of ['Light','Dark'])for(const name of Object.keys(tableMinimumWidths))assert(names.has(scheme+'/'+name));
 assert.deepEqual(r.pngs.map(p=>p.file),r.states.map(s=>s.file));
 return{passed:true,states:24,themes:2,scrollViewportStates:10,callerOverrideStates:6,callerTemplateStates:2,originalContentAndTemplatesRetained:true,liveMinimumGrowthAndShrinkVerified:true,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false,librarySha256:r.librarySha256,hostSha256:r.hostSha256};
}
export async function auditTableMinimum(folder,host){
 const bytes=await fs.readFile(path.join(folder,'table-minimum.json')),r=JSON.parse(bytes),result=verifyTableMinimumReport(r);
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.dll'))),r.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',host+'.dll'))),r.hostSha256);
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 let pixels=0;for(const p of r.pngs){assert.match(p.sha256,/^[a-f0-9]{64}$/);const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const image=readRgbaPng(bytes);assert.equal(image.width,1000);assert.equal(image.height,760);assert.equal(p.width,image.width);assert.equal(p.height,image.height);pixels+=image.width*image.height;}
 return{...result,rawPixelsValidated:pixels,reportSha256:hash(bytes)};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const[folder,host,output]=process.argv.slice(2);assert(folder&&['Orchard.Catalog','PackageConsumer'].includes(host)&&output);
 const result=await auditTableMinimum(folder,host);await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
