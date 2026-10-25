import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyModalContent(n,s){
 assert.equal(n.format,'orchard-modal-content-v1');assert.equal(n.passed,true);assert.deepEqual(n.failures,[]);assert.equal(n.cases.length,16);assert.equal(n.focus,0);assert.equal(n.activations,0);assert(n.guards>=500);assert.equal(n.softwareNativeInvokes,4);assert.equal(n.publicApisOnly,true);assert.equal(n.architecture,'X64');
 for(const key of ['activateCalled','showCalled','focusCalled','hardwareInputSent','physicalInputVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 assert.equal(s.format,'orchard-modal-content-source-v1');assert.equal(s.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(s.histories.length,10);
 const names=['automatic-root-child-height','automatic-root-add-remove','templated-model-text-reflow','templated-root-size-binding','plain-content-live-lines','owned-popover-suspension','owned-tooltip-lifetime','owned-select-controlled-request'],kinds=['child','collection','text','binding','plain'];let values=0;
 for(const scheme of ['Light','Dark'])for(const[index,name]of names.entries()){
  const matches=n.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(matches.length,1);const c=matches[0];assert.equal(c.passed,true);assert(c.evidence);
  if(index<5){const all=s.histories.filter(h=>h.scheme===scheme&&h.kind===kinds[index]);assert.equal(all.length,1);const h=all[0];assert.equal(c.evidence.compact,h.before.surfaceHeight);values++;for(const[prefix,key]of [['grown','grown'],['restored','shrunk']])for(const value of ['surfaceHeight','extent','viewport','offset']){assert.equal(c.evidence[prefix][value],h[key][value],scheme+':'+name+':'+prefix+'.'+value);values++;}assert(h.grown.originalRetained&&h.shrunk.originalRetained);}
  else for(const [key,value]of Object.entries(c.evidence))assert.equal(value,true,scheme+':'+name+':'+key);
 }
 return{passed:true,cases:16,sourceHistories:10,exactSourceValues:values,softwareNativeInvokes:4,guards:n.guards,librarySha256:n.librarySha256,hostSha256:n.hostSha256,publicApisOnly:true,physicalInputVerified:false,nativeComponentAccepted:false};
}
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0);return process.argv[i+1];};
if(process.argv[1]?.endsWith('verify-modal-content.mjs')){const dir=option('--native'),n=JSON.parse(await fs.readFile(path.join(dir,'modal-content.json'))),s=JSON.parse(await fs.readFile('tests/Reference/ModalContent.json')),audit=verifyModalContent(n,s);assert.equal(hash(await fs.readFile(path.join(dir,'app-identity/Orchard.dll'))),n.librarySha256);assert.equal(hash(await fs.readFile(path.join(dir,'app-identity',option('--host-assembly')))),n.hostSha256);const exit=JSON.parse(await fs.readFile(path.join(dir,'process-exit.json')));assert(exit.exited&&!exit.timedOut);assert.equal(exit.exitCode,0);await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));}
