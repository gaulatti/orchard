import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import {pathToFileURL} from 'node:url';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export async function auditOwnership(folder,provided){
 const r=provided??JSON.parse(await fs.readFile(path.join(folder,'modal-host-ownership.json'))),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));
 assert.equal(r.format,'orchard-modal-host-ownership-v1');assert.equal(r.scope,'native-branch-ownership-transactionality-and-text-element-target-predicates');assert.equal(r.passed,true);assert.deepEqual(r.failures,[]);assert.equal(r.cases.length,18);assert.equal(r.processArchitecture,'X64');assert.equal(r.activations,0);assert.equal(r.focus,0);assert.equal(r.guards,74);
 for(const key of ['activateCalled','showCalled','focusCalled','inputSent','nativeModalityVerified','actualFocusRoutingVerified'])assert.equal(r[key],false,key);
 assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.dll'))),r.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.Catalog.dll'))),r.hostSha256);
 const expected={
  'foreign-host-position-rejected':{rejected:true,unchanged:true},
  'invalid-position-preserves-frame':{rejected:4,unchanged:true},
  'invalid-attach-leaves-root-reusable':{rejected:true,rootReusable:true,originalReferenceRetained:true,registryUnchanged:true},
  'active-textblock-and-richtext-links':{simple:true,rich:true,actualTextPointerOwnersMatched:true},
  'active-registered-branch-link':{allowed:true},
  'background-and-foreign-links-rejected':{backgroundRejected:true,foreignRejected:true},
  'inactive-and-detached-links-rejected':{inactiveRejected:true,detachedRejected:true},
  'ancestor-teardown-rewires-inline-target':{inactiveAncestorDidNotRestore:true,originalBackgroundInlineTargetRestored:true,focusCalled:false},
  'detached-branch-position-rejected':{rejected:true,unchanged:true}
 };
 for(const scheme of ['Light','Dark'])for(const[name,evidence]of Object.entries(expected)){const rows=r.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(rows.length,1);assert.equal(rows[0].passed,true);assert.deepEqual(rows[0].evidence,evidence,scheme+'/'+name);}
 return{passed:true,cases:18,guards:r.guards,librarySha256:r.librarySha256,hostSha256:r.hostSha256,foreignAndDetachedPositionRejected:true,invalidAttachRootReusable:true,inlineTextBlockRichTextAndBranchOwnershipVerified:true,inlineRestorationTargetRewiringVerified:true,nativeFocusRequested:false,actualFocusRoutingVerified:false,nativeModalityVerified:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};const result=await auditOwnership(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));}
