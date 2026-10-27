import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const digest=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
export const navigationStateIds=['closed','empty','links-only','basic','empty-group','no-description','long-description','unicode','narrow','rtl','bottom-right','group-href-unused','layout-open','custom','custom-input','font200'];
export const navigationContractIds=['route-Buttons','route-Forms','route-External library','route-Home','command-denial-disable','live-indices-originals','custom-original-retention-close','hidden-declaration-ancestor','active-modal-isolation','template-unload-reload','surface-motion'];
export function verifyNavigationMenu(native,reference,catalogExpected) {
  let geometryValues=0,maximumGeometryError=0;
  const box=(actual,expected,label)=>{for(const key of ['x','y','width','height']){assert(Number.isFinite(actual[key]),label+'.'+key);const error=Math.abs(actual[key]-expected[key]);assert(error<=.02,label+'.'+key+': '+error);maximumGeometryError=Math.max(maximumGeometryError,error);geometryValues++;}};
  const inputs=(actual,original,label)=>{assert.equal(actual.length,original.length,label);for(const [i,input] of actual.entries()){assert.equal(input.role,'Edit');assert.equal(input.value,original[i].value);assert.equal(input.selectionStart,original[i].selectionStart);assert.equal(input.selectionEnd,original[i].selectionEnd);assert.equal(input.box.width,90);assert.equal(input.box.height,18);}};
  assert.equal(native.format,'orchard-navigation-menu-native-v1');assert.equal(native.passed,true);assert.equal(native.publicApisOnly,true);
  for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(native[key],false,key);
  assert.equal(native.focus,0);assert.equal(native.activations,0);assert(native.guards>100);
  assert.equal(native.states.length,navigationStateIds.length*2);assert.equal(native.contracts.length,navigationContractIds.length*2);
  assert.equal(native.presence.length,2);assert.equal(native.standalone.length,2);assert.equal(native.catalog.length,catalogExpected?2:0);
  assert.equal(native.pngs.length,native.states.length+native.catalog.length);assert.equal(new Set(native.pngs.map(p=>p.file)).size,native.pngs.length);
  for(const scheme of ['Light','Dark']) {
    const states=native.states.filter(s=>s.scheme===scheme),contracts=native.contracts.filter(c=>c.scheme===scheme);
    assert.deepEqual(states.map(s=>s.kind),navigationStateIds);assert.deepEqual(contracts.map(c=>c.action),navigationContractIds);assert(contracts.every(c=>c.passed===true));
    for(const state of states) {
      assert.equal(state.sourceRasterParityVerified,false);
      const source=reference.cases.find(c=>c.id===state.kind&&c.scheme===scheme);assert(source);
      const m=state.measurement,s=source.measurement;box(m.menu,s.container.box,state.kind+'.menu');
      const expectedEntries=[...s.links.map(l=>({label:l.text,box:l.box,role:'Hyperlink'})),...s.triggers.map(t=>({label:t.text,box:t.box,role:'Button'}))];
      assert.equal(m.entries.length,expectedEntries.length);
      for(const entry of m.entries) {const original=expectedEntries.find(e=>e.label===entry.label);assert(original);assert.equal(entry.role,original.role);box(entry.box,original.box,state.kind+'.'+entry.label);if(state.kind.startsWith('custom'))inputs(entry.inputs,s.links.find(l=>l.text===entry.label)?.input??[],state.kind+'.'+entry.label);}
      assert.equal(m.surfaces.length,s.surfaces.length);
      for(const [i,surface] of m.surfaces.entries()) {
        const original=s.surfaces[i];box(surface.box,original.box,state.kind+'.surface');assert.equal(surface.Opacity,1);assert.equal(surface.rows.length,original.links.length);
        assert.deepEqual(surface.background,scheme==='Light'?{a:255,r:255,g:255,b:255,opacity:1}:{a:255,r:24,g:37,b:51,opacity:1});
        for(const [j,row] of surface.rows.entries()) {const link=original.links[j];assert.equal(row.href,link.href);assert.equal(row.external,state.kind.startsWith('custom')?row.label==='External library':link.target==='_blank');assert.equal(row.label,link.parts[0].text);assert.equal(row.role,'Hyperlink');assert.equal(row.navigateUriUnset,true);box(row.box,link.box,state.kind+'.row'+j);if(state.kind.startsWith('custom'))inputs(row.inputs,link.input,state.kind+'.row'+j);}
      }
    }
    for(const label of ['Buttons','Forms','External library','Home']) {
      const d=contracts.find(c=>c.action==='route-'+label).details;assert(d.originalReceived);assert.deepEqual(d.order,['command','event']);assert.equal(d.retiredRejected,label!=='Home');assert.equal(d.groupRemainsOpen,label==='Home');assert.equal(d.external,label==='External library');
      assert.equal(d.href,label==='Buttons'?'#fictional-buttons':label==='Forms'?'/fictional/forms?kind=one%2Ftwo#fields':label==='Home'?'#fictional-home':'https://example.invalid/library');
    }
    assert.deepEqual(contracts.find(c=>c.action==='custom-original-retention-close').details,{customRequests:1,originalItemRetained:true,sameInputDuringLiveUpdate:true,resetAfterUnmount:true,directCloseIsNull:true});
    assert.deepEqual(contracts.find(c=>c.action==='hidden-declaration-ancestor').details,{immediateEligibilityRevoked:true,portedSurfaceRetired:true,revealStaysClosed:true});
    assert.deepEqual(contracts.find(c=>c.action==='active-modal-isolation').details,{backgroundRejectedProviders:4,nestedRejectedProviders:2,closingOwnerProviderRejected:true,ownerRetiredProviderRejected:true,backgroundInvocations:1,ownedInvocations:2,unopenedInvocations:1,originalBackgroundDelivered:true,originalOwnedDelivered:true,unopenedOriginalDelivered:true,blockedCommandsIgnored:true,callerEnabledBindingRetained:true,nestedOriginalStateRestored:true,originalModalContentRetained:true});
    const motion=contracts.find(c=>c.action==='surface-motion').details;
    assert(motion.entryMid>0&&motion.entryMid<1&&motion.entryEnd===1);
    assert(motion.exitMid>0&&motion.exitMid<1&&motion.reopenStart>0&&motion.reopenStart<1&&motion.reopenEnd===1);
    assert.equal(motion.reducedClosed,0);assert.equal(motion.reducedOpen,1);
    assert.equal(motion.callerRetained,true);assert.equal(motion.originalSurfaceRetained,true);assert.equal(motion.controlledClock,true);assert.equal(motion.normalRealtimeMotionVerified,false);
    assert.deepEqual(native.presence.find(p=>p.scheme===scheme),{scheme,originalRetainedAt50:true,retiredBy190:true,reopenCancelledExit:true,normalMotionVerified:false,passed:true});
    assert.deepEqual(native.standalone.find(p=>p.scheme===scheme),{scheme,rootConstrained:true,noNativeLightDismiss:true,unloadedPopupRetired:true,passed:true});
    if(catalogExpected) {const c=native.catalog.find(c=>c.scheme===scheme);assert.equal(c.menus,6);assert.equal(c.originalStylesApplied,true);assert.equal(c.registeredFixture,'Orchard.Catalog.NavigationMenusFixture');}
  }
  return{geometryValues,maximumGeometryError};
}
export function verifyNavigationMenuPng(frame,bytes) {
  assert.equal(digest(bytes),frame.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,1000);assert.equal(png.height,760);assert.equal(frame.width,1000);assert.equal(frame.height,760);return png;
}
export function verifyNavigationMenuPaint(state,png,original,sourcePng) {
  let probes=0;
  const pixel=(image,x,y)=>[...image.rgba.subarray((y*image.width+x)*4,(y*image.width+x)*4+4)];
  const probe=(x,y,label)=>{x=Math.floor(x);y=Math.floor(y);assert(x>=0&&y>=0&&x<png.width&&y<png.height);const actual=pixel(png,x,y),expected=pixel(sourcePng,x,y);for(let c=0;c<4;c++)assert(Math.abs(actual[c]-expected[c])<=1,state.kind+'.'+label+': '+actual+' vs '+expected);probes++;};
  const expanded=original.measurement.triggers.find(t=>t.expanded==='true');
  if(expanded)probe(expanded.box.x+10,expanded.box.y+3,'active trigger');
  // Sample flat interior padding. The 14px empty surface has no straight
  // vertical edge; its rounded corner is an antialiasing comparison, not a
  // flat palette probe. Complete edge/raster parity remains unverified.
  for(const surface of original.measurement.surfaces)probe(surface.box.x+surface.box.width/2,surface.box.y+2,'surface');
  return probes;
}
export async function verifyNavigationMenuFiles(folder,hostAssembly) {
  const nativeBytes=await fs.readFile(path.join(folder,'navigation-menu.json')),native=JSON.parse(nativeBytes),referenceBytes=await fs.readFile('tests/Reference/NavigationMenu.json'),reference=JSON.parse(referenceBytes);
  assert.equal(digest(referenceBytes),'91203639eac8fe7bd904a34e46a82cedc3bfeff124adb06c254011906be70f1c');
  const geometry=verifyNavigationMenu(native,reference,hostAssembly==='Orchard.Catalog.dll');assert.equal(digest(await fs.readFile(path.join(folder,'source-reference.json'))),digest(referenceBytes));
  const process=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert(process.exited&&process.exitCode===0&&!process.timedOut);
  assert.equal(digest(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),native.librarySha256);assert.equal(digest(await fs.readFile(path.join(folder,'app-identity',hostAssembly))),native.hostSha256);
  let paletteProbes=0,pixelsValidated=0;
  for(const frame of native.pngs) {
    assert.equal(path.basename(frame.file),frame.file);const png=verifyNavigationMenuPng(frame,await fs.readFile(path.join(folder,frame.file)));pixelsValidated+=png.width*png.height;
    const state=native.states.find(s=>s.file===frame.file);if(state){const original=reference.cases.find(c=>c.id===state.kind&&c.scheme===state.scheme),sourceBytes=await fs.readFile(path.join('tests/Reference/NavigationMenu',original.file));assert.equal(digest(sourceBytes),original.pngSha256);paletteProbes+=verifyNavigationMenuPaint(state,png,original,readRgbaPng(sourceBytes));}
  }
  return{format:'orchard-navigation-menu-native-audit-v1',passed:true,states:native.states.length,contracts:native.contracts.length,presenceHistories:native.presence.length,standalonePopupHistories:native.standalone.length,catalogStates:native.catalog.length,pngs:native.pngs.length,pixelsValidated,paletteProbes,...geometry,hiddenBoundaryVerified:true,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false,librarySha256:native.librarySha256,hostSha256:native.hostSha256,reportSha256:digest(nativeBytes),referenceSha256:digest(referenceBytes)};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href) {
  const arg=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return process.argv[i+1];};const result=await verifyNavigationMenuFiles(arg('--native'),arg('--host-assembly'));await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
