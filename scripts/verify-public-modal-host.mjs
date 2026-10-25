import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import{pathToFileURL}from'node:url';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export async function auditPublicHost(folder,provided){
 const r=provided??JSON.parse(await fs.readFile(path.join(folder,'public-modal-host.json'))),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));
 assert.equal(r.format,'orchard-public-modal-host-v1');assert.equal(r.scope,'native-root-content-ownership-lifecycle-and-key-planning');assert.equal(r.passed,true);assert.deepEqual(r.failures,[]);assert.equal(r.processArchitecture,'X64');assert.equal(r.activations,0);assert.equal(r.focus,0);assert.equal(r.cases.length,30);assert.equal(r.guards,286);assert.equal(r.closedRootRefreshVerified,true);
 for(const key of ['activateCalled','showCalled','focusCalled','inputSent','actualKeyRoutingVerified','actualFocusCancellationVerified','nativeModalityVerified'])assert.equal(r[key],false,key);
 assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.dll'))),r.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.Catalog.dll'))),r.hostSha256);
 const expected={
  'root-content-and-caller-bindings':{originalContent:true,bindingsRetained:3,valuesContinueUpdating:true,privatePointerShield:true,sameRootBackgroundRejected:true,activeAndDetachedTargetsPreserved:true,activePeerScopeRetained:true},
  'sequential-native-buttons-and-inline':{targets:['Priority','Ordinary','Inline'],disabledAndHiddenExcluded:true,surfaceExcluded:true},
  'tab-and-shift-tab-plans':{plans:8,allHandled:true,nativeKeysSent:false},
  'escape-is-a-controlled-request':{escapeHandled:true,requestOnly:true,unrelatedKeyPreserved:true,activeUnchanged:true},
  'branch-participates-in-ownership-and-order':{targets:4,branchFirst:true,branchAllowed:true,backgroundRejected:true},
  'logical-ancestry-crosses-ported-branches':{childDepth:1,grandchildDepth:2,nearestBranchOwner:true},
  'child-first-activation-keeps-descendants-active':{lateAncestorPreservesChild:true,grandchildActive:true,inactiveBranchRejected:true,emptySurfaceFallback:true,parentResumed:true},
  'template-replacement-retains-owned-plane':{activeSurfaceRetained:true,originalContentRetained:true,contentBindingRetained:true,registrationRetained:true},
  'rtl-root-retains-physical-overlay-viewport':{fullViewportRetained:true,activeSurfaceRetained:true},
  'outside-declaration-rejected':{rejected:true},
  'closing-and-detaching-release-background':{backgroundReleased:true,closingFrameInert:true,callerValuesRetained:true,disposalIdempotent:true},
  'constrained-root-rejected-before-taking-content':{rejected:true,overlayReusable:true},
  'transformed-root-rejected-before-taking-content':{rejected:true,overlayReusable:true},
  'content-template-and-selector-retain-caller-data':{explicitTemplateRendered:true,selectorRendered:true,originalDataRetained:true,contentBindingRetained:true},
  'unload-releases-registry-and-registrations':{registrationReleased:true,backgroundReleased:true,reloadReconnected:true}
 };
 for(const scheme of ['Light','Dark'])for(const[name,evidence]of Object.entries(expected)){const rows=r.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(rows.length,1,scheme+'/'+name);assert.equal(rows[0].passed,true,scheme+'/'+name);assert.deepEqual(rows[0].evidence,evidence,scheme+'/'+name);}
 assert.equal(r.catalogRootCases.length,16);const captures=[];for(const scheme of ['Light','Dark']){const rows=r.catalogRootCases.filter(c=>c.scheme===scheme);assert.deepEqual(rows.map(c=>c.fixture),['Foundation','ScrollAreas','Popovers','HoverCards','MonitoringWorkspace','EditorialWorkspace','Foundation','diagnostic-modal-layer']);for(const[index,row]of rows.entries()){assert.deepEqual(row,{scheme,fixture:row.fixture,width:1100,height:800,originalFixtureRetained:true,themeMatched:true,modalActive:false,capture:row.capture});if([0,4,5].includes(index)){const c=row.capture;assert(c);assert.equal(c.file,`catalog-${scheme.toLowerCase()}-${row.fixture}.png`);assert.equal(c.width,1100);assert.equal(c.height,800);const bytes=await fs.readFile(path.join(folder,c.file));assert.equal(hash(bytes),c.sha256);assert.equal(bytes.subarray(1,4).toString(),'PNG');assert.equal(bytes.readUInt32BE(16),1100);assert.equal(bytes.readUInt32BE(20),800);captures.push(c);}else assert.equal(row.capture,null);}}
 return{passed:true,cases:r.cases.length,catalogRootStates:16,diagnosticPngs:captures.length,guards:r.guards,librarySha256:r.librarySha256,hostSha256:r.hostSha256,originalContentAndBindingsRetained:true,logicalAncestryAcrossBranchesVerified:true,keyboardPlansInspected:true,templateAndUnloadLifecycleVerified:true,closedRootRefreshVerified:true,bothFictionalDemosLoaded:true,nativeInputSent:false,actualKeyRoutingVerified:false,actualFocusCancellationVerified:false,nativeModalityVerified:false,sourceRasterParityVerified:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return path.resolve(process.argv[i+1]);};const result=await auditPublicHost(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));}
