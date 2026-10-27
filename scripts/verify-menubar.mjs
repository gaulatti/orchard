import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {cssRgba} from './reference/css-paint.mjs';

const rowReferenceBytes=await fs.readFile(new URL('../tests/Reference/MenubarRowMotion.json',import.meta.url));
const rowReference=JSON.parse(rowReferenceBytes);

const digest=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const stateIds=['closed','empty-bar','basic','rich-action','checked-beta','empty-menu','edit-menu','submenu','deep-submenu','disabled-submenu','unicode','long-label','narrow','rtl','bottom-right'];
export function verifyMenubarPeerMetadata(native) {
  assert.equal(native.peerTrees.length,8);assert.equal(native.groupContracts.length,12);
  const names=['File','More fictional choices','Deeper fictional choices'];
  let menus=0,containers=0;
  const leaf=node=>{assert.equal(node.role,'MenuItem');assert.equal(node.control,true);assert.equal(node.content,true);assert.equal(node.truncated,false);assert.deepEqual(node.children,[],'menu label/icon text must not become separate controls');};
  for(const scheme of ['Light','Dark'])for(const kind of ['closed','basic','submenu','deep-submenu']){
    const matches=native.peerTrees.filter(t=>t.scheme===scheme&&t.kind===kind);assert.equal(matches.length,1);const t=matches[0];
    assert.equal(t.bar.role,'MenuBar');assert.deepEqual(t.bar.children.map(p=>p.name),['File','Edit']);t.bar.children.forEach(leaf);
    const levels={closed:0,basic:1,submenu:2,'deep-submenu':3}[kind];assert.equal(t.surfaces.length,levels);
    assert.deepEqual(t.bar.children[0].controlled,levels?[{role:'Menu',name:'File'}]:[]);assert.deepEqual(t.bar.children[1].controlled,[]);
    for(const [i,s]of t.surfaces.entries()){
      const menu=s.menu;assert.equal(menu.role,'Menu');assert.equal(menu.name,names[i]);assert.equal(menu.control,true);assert.equal(menu.content,true);assert.equal(menu.truncated,false);menus++;
      s.rows.forEach(row=>{leaf(row);const child=names.indexOf(row.name);assert.deepEqual(row.controlled,child>i&&child<levels?[{role:'Menu',name:row.name}]:[]);if(row.selection){assert.equal(row.selection.containerAvailable,true);assert.equal(row.selection.container.resolved,true);assert.equal(row.selection.container.role,'Group');containers++;}});
      const groups=menu.children.filter(p=>p.role==='Group');assert.equal(groups.length,i===0?1:0);
      if(groups.length){const group=groups[0];assert.equal(group.control,true);assert.equal(group.content,true);assert.equal(group.group.multiple,false);assert.equal(group.group.required,false);assert.deepEqual(group.children.map(p=>p.name),['Fictional Alpha mode','Fictional Beta mode','Unavailable mode']);group.children.forEach(leaf);assert.deepEqual(group.group.selected,[{resolved:true,role:'MenuItem',name:'Fictional Alpha mode'}]);}
      assert.deepEqual(menu.children.filter(p=>p.role==='MenuItem').map(p=>p.name),i===0?['New fictional draft','Unavailable action','Show fictional guides','More fictional choices','Unavailable submenu']:i===1?['Nested original action','Deeper fictional choices']:['Deep original action']);
    }
  }
  for(const scheme of ['Light','Dark']){
    const records=native.groupContracts.filter(c=>c.scheme===scheme);assert.deepEqual(records.map(c=>c.id).sort(),['separate','select-first','reorder','removed','closing','reopened'].sort());
    for(const c of records){assert.equal(c.passed,true);assert.equal(c.callbacks,c.id==='separate'?0:1);assert.equal(c.groups.length,c.id==='reopened'?1:2);for(const [i,g]of c.groups.entries()){assert.equal(g.role,'Group');const active=c.id!=='closing'&&!(c.id==='removed'&&i===1);assert.equal(g.enabled,active);assert.deepEqual(g.names,!active?[]:i===0?['First A','First B']:c.id==='reorder'?['Second Y','Second X']:['Second X','Second Y']);assert.deepEqual(g.selected,!active?[]:i===0?[c.id==='separate'?'First A':'First B']:['Second X']);}}
  }
  return{peerTreeObservations:8,menuPeersVerified:menus,radioContainerAssociationsVerified:containers,multipleGroupLifecycleCases:12,automationEventDeliveryVerified:false};
}
export function verifyMenubar(native,reference,catalogExpected) {
  let geometryValues=0,maximumGeometryError=0;
  const box=(actual,expected,label)=>{for(const key of ['x','y','width','height']){assert(Number.isFinite(actual[key]));const error=Math.abs(actual[key]-expected[key]);assert(error<=.02,label+'.'+key+': '+error);maximumGeometryError=Math.max(maximumGeometryError,error);geometryValues++;}};
  assert.equal(native.format,'orchard-menubar-native-v1');assert.equal(native.passed,true);assert.equal(native.publicApisOnly,false);
  assert.equal(native.interactionScenariosUsePublicApis,false);assert.equal(native.selectionScenariosUsePublicApis,true);assert.equal(native.controlledClockUsesPrivateRenderer,true);
  const metadata=verifyMenubarPeerMetadata(native);
  assert.equal(native.keyboardControllerUsesPrivateMethod,true);assert.equal(native.keyboardRoutedEventsVerified,false);assert.equal(native.keyboardFocusMovementVerified,false);
  const keyboardTargets={
    'closed-next-root':'Edit','closed-wrap-root':'File','root-home':'File','root-end':'Edit','closed-escape':null,'skip-disabled-root':'File',
    'open-first':'New fictional draft','open-next-root':'Edit','open-previous-root':'File','unhandled-letter':null,'skip-disabled-and-separator':'Show fictional guides','enter-radio-group':'Fictional Alpha mode','radio-group-next':'Fictional Beta mode','leave-radio-group':'More fictional choices','item-home':'New fictional draft',
    'wrap-up':'More fictional choices','wrap-down':'New fictional draft','item-end':'More fictional choices','disabled-action':null,'disabled-submenu':null,
    'enter-submenu':'Nested original action','submenu-next':'Deeper fictional choices','enter-deep':'Deep original action','escape-deep':'Deeper fictional choices',
    'back-submenu':'More fictional choices','escape-root':'File','open-last':'More fictional choices','space-submenu':'Nested original action',
    'leaf-next-root':'Edit original action','leaf-previous-root':'New fictional draft','tab-exit':'File','enter-root':'New fictional draft',
    'activate-action':'File','retired-row':null,'checkbox-False':'File','checkbox-True':'File','radio-False':'File','radio-True':'File',
    'empty-menu':'File','empty-menu-close':'File','unloaded-owner':null
  };
  const unhandled=new Set(['closed-escape','unhandled-letter','disabled-action','disabled-submenu','tab-exit','retired-row','unloaded-owner']);
  for(const scheme of ['Light','Dark'])for(const flow of ['LeftToRight','RightToLeft']){
    const cases=native.keyboard.filter(c=>c.scheme===scheme&&c.flow===flow);
    assert.deepEqual(cases.map(c=>c.id).sort(),Object.keys(keyboardTargets).sort());
    for(const c of cases){assert.equal(c.passed,true);assert.equal(c.target,keyboardTargets[c.id],c.id);assert.equal(c.handled,!unhandled.has(c.id),c.id);}
    for(const id of ['closed-next-root','closed-wrap-root','skip-disabled-root','open-next-root','enter-submenu','leaf-next-root'])assert.equal(cases.find(c=>c.id===id).key,flow==='LeftToRight'?'Right':'Left');
    for(const id of ['open-previous-root','back-submenu','leaf-previous-root'])assert.equal(cases.find(c=>c.id===id).key,flow==='LeftToRight'?'Left':'Right');
  }
  assert.equal(native.keyboard.length,Object.keys(keyboardTargets).length*4);
  const radioIds=['selected-select','selected-add','unselected-remove','conflicting-add','selected-remove',
    ...['disabled-unselected','disabled-selected','unloaded'].flatMap(kind=>['Select','Add','Remove'].map(method=>kind+'-'+method)),
    'select-False','select-True','add-empty-False','add-empty-True','invoke-selected','throwing-select'];
  for(const scheme of ['Light','Dark']){
    const cases=native.radioSelection.filter(c=>c.scheme===scheme);
    assert.deepEqual(cases.map(c=>c.id).sort(),[...radioIds].sort());
    for(const c of cases){
      const requested=/^(select-|add-empty-)/.test(c.id)||['invoke-selected','throwing-select'].includes(c.id);
      const value=['select-True','add-empty-True'].includes(c.id)?'b':c.id==='add-empty-False'?'unknown':'a';
      const error=/^(disabled-|unloaded-)/.test(c.id)?'ElementNotEnabledException':['conflicting-add','selected-remove','throwing-select'].includes(c.id)?'InvalidOperationException':null;
      assert.equal(c.passed,true);assert.equal(c.error,error,c.id);assert.equal(c.callbacks,requested?1:0,c.id);
      assert.equal(c.value,value,c.id);assert.deepEqual(c.selected,value==='unknown'?[]:[value],c.id);
      assert.equal(c.surfaces,c.id.startsWith('unloaded-')||(requested&&c.id!=='throwing-select')?0:1,c.id);
      assert.equal(c.callerRetained,!['select-True','add-empty-True'].includes(c.id),c.id);
    }
  }
  assert.equal(native.radioSelection.length,radioIds.length*2);
  assert.equal(native.surfaceMotion.length,2);
  assert.equal(rowReference.format,'orchard-menubar-row-motion-reference-v1');
  assert.equal(rowReference.originalCaptureBrowser,reference.browser);assert.equal(rowReference.cssSha256,reference.cssSha256);assert.equal(rowReference.bundleSha256,reference.bundleSha256);
  assert.equal(native.rowMotion.length,48);assert.equal(native.rowMotionCaptures.length,2);assert.equal(native.rowPaintOwnership.length,2);
  const rowKey=t=>[t.scheme,t.reduced,t.role,t.kind].join('/');
  assert.deepEqual(native.rowMotion.map(rowKey).sort(),rowReference.transitions.map(rowKey).sort());
  let rowPresentations=0;
  const paintEqual=(actual,css,label)=>{const expected=cssRgba(css);assert.match(actual.color,/^#[0-9A-F]{8}$/);const rgba=[...actual.color.slice(3).matchAll(/../g)].map(v=>parseInt(v[0],16));assert.deepEqual(rgba,expected.slice(0,3),label+'/RGB');const alpha=parseInt(actual.color.slice(1,3),16)/255*actual.opacity;if(css.startsWith('rgb'))assert.equal(Math.round(alpha*255),Math.round(expected[3]*255),label+'/alpha-byte');else assert(Math.abs(alpha-expected[3])<.000001,label+'/alpha');};
  for(const t of native.rowMotion){const source=rowReference.transitions.find(r=>rowKey(r)===rowKey(t));assert.equal(t.controlledClock,true);assert.equal(t.inputRoutingVerified,false);for(const field of ['background','foreground'])paintEqual(t.before[field],source.before[field],rowKey(t)+'/before/'+field);const samples=source.samples.filter(s=>!t.reduced||s.milliseconds!==0);assert.deepEqual(t.samples.map(s=>s.milliseconds),samples.map(s=>s.milliseconds));for(const [i,s]of t.samples.entries()){assert.equal(s.paint.rendering,false);for(const field of ['background','foreground'])paintEqual(s.paint[field],samples[i][field],rowKey(t)+'/'+s.milliseconds+'/'+field);rowPresentations++;}}
  assert.equal(rowPresentations,264);
  for(const scheme of ['Light','Dark']){const o=native.rowPaintOwnership.find(t=>t.scheme===scheme);assert(o);for(const key of ['originalBindingsRetained','liveBrushMutationVerified','nonColorBrushIdentityRetained','callerTemplateRetained','reloadVerified','unloadedObserversReleased','passed'])assert.equal(o[key],true);assert(native.rowMotionCaptures.some(c=>c.scheme===scheme&&native.pngs.some(p=>p.file===c.file)));}
  for(const scheme of ['Light','Dark']) {
    const trace=native.surfaceMotion.find(t=>t.scheme===scheme);assert(trace);
    for(const key of ['originalSurfaceRetained','callerOpacityBindingRetained','callerTransformRetained','controlledClock'])assert.equal(trace[key],true);
    assert.equal(trace.callerOpacity,trace.callerSourceOpacity);assert(Math.abs(trace.callerSourceOpacity-.4)<.0000001);assert.equal(trace.normalRealtimeMotionVerified,false);
    assert.deepEqual(trace.histories.map(h=>[h.visible,h.time,h.animate]),[[false,0,false],[true,20,true],[false,300,true],[true,360,true],[false,600,true],[false,800,false],[true,820,false]]);
    const curve=(v,entry)=>{if(v<=0)return 0;if(v>=1)return 1;let lo=0,hi=1;for(let i=0;i<40;i++){const t=(lo+hi)/2,x=3*(1-t)**2*t*(entry?.22:.42)+3*(1-t)*t*t*(entry?.36:1)+t**3;if(x<v)lo=t;else hi=t;}const t=(lo+hi)/2;return entry?1-(1-t)**3:3*(1-t)*t*t+t**3;};
    let from=0,to=0,start=0,duration=0,reversingStart=0,factor=1,entry=false;
    const running=t=>duration>0&&t-start<duration,value=t=>running(t)?from+(to-from)*curve((t-start)/duration,entry):to;
    const expectedTimes=[[0],[20,75,130,240],[300,350],[360,380,420,580],[600,650,749],[800],[820]];
    for(const [i,h]of trace.histories.entries()) {
      const target=h.visible?1:0;
      if(!h.animate){from=to=reversingStart=target;start=h.time;duration=0;factor=1;entry=h.visible;}
      else if(target!==to){const current=value(h.time),reverse=running(h.time)&&target===reversingStart;
        factor=reverse?Math.min(1,Math.abs(curve((h.time-start)/duration,entry)*factor+1-factor)):1;
        reversingStart=reverse?to:current;from=current;to=target;start=h.time;duration=(h.visible?220:150)*factor;entry=h.visible;}
      assert.deepEqual(h.samples.map(s=>s.time),expectedTimes[i]);
      for(const s of h.samples){const p=value(s.time);for(const [key,expected]of Object.entries({opacity:p,scaleX:h.visible?1:.992,scaleY:h.visible?1:.992,translateY:h.visible?0:-4}))assert(Math.abs(s[key]-expected)<.000002,scheme+':'+s.time+':'+key);}
    }
  }
  for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(native[key],false,key);
  assert.equal(native.focus,0);assert.equal(native.activations,0);assert(native.guards>100);
  assert.equal(native.states.length,30);assert.equal(native.contracts.length,28);
  assert.equal(native.presence.length,2);assert.equal(native.standalone.length,2);
  assert.equal(native.catalog.length,catalogExpected?2:0);
  assert.equal(native.pngs.length,32+native.catalog.length);
  assert.equal(new Set(native.pngs.map(p=>p.file)).size,native.pngs.length);
  for(const scheme of ['Light','Dark']) {
    const states=native.states.filter(s=>s.scheme===scheme),contracts=native.contracts.filter(c=>c.scheme===scheme);
    assert.deepEqual(states.map(s=>s.kind),stateIds);
    for(const s of states) {
      assert(reference.cases.some(c=>c.id===s.kind&&c.scheme===scheme));
      assert.equal(s.sourceRasterParityVerified,false);
      assert.equal(s.disabledInvocationsRejected,s.kind==='disabled-submenu'?3:0);
      const m=s.measurement;
      const original=reference.cases.find(c=>c.id===s.kind&&c.scheme===scheme).measurement;
      box(m.bar,original.bar.box,s.kind+'.bar');
      assert.equal(m.bar.width,s.kind==='narrow'?140:s.kind==='bottom-right'?180:360);
      assert(m.bar.height>0);
      assert.equal(m.menusRetained,true);
      assert.equal(m.triggers.length,s.kind==='empty-bar'?0:2);
      for(const [i,trigger] of m.triggers.entries()){assert.equal(trigger.role,'MenuItem');assert.equal(trigger.activeBackgroundOpacity,trigger.expanded==='Expanded'?1:0);assert(trigger.box.width>0&&trigger.box.height>0);assert.equal(trigger.label,original.triggers[i].text);box(trigger.box,original.triggers[i].box,s.kind+'.trigger'+i);}
      const count=s.kind==='deep-submenu'?3:s.kind==='submenu'?2:['closed','empty-bar'].includes(s.kind)?0:1;
      assert.equal(m.surfaces.length,count,s.kind);
      for(const [i,surface] of m.surfaces.entries()) {
        const sourceSurface=original.surfaces[i];
        const expectedBox=s.kind==='bottom-right'&&i===0?{...sourceSurface.box,
          x:reference.viewport.width-sourceSurface.box.width,
          y:original.triggers[0].box.y-sourceSurface.box.height-4}:sourceSurface.box;
        box(surface.box,expectedBox,s.kind+'.surface'+i);
        assert.equal(surface.Opacity,1);
        assert.equal(surface.box.width,surface.name==='MenubarRootSurface'?224:192);
        assert(surface.box.height>0);
        assert.deepEqual(surface.background,scheme==='Light'?{a:255,r:255,g:255,b:255,opacity:1}:{a:255,r:24,g:37,b:51,opacity:1});
        const sourceRows=sourceSurface.rows.filter(r=>r.role!=='separator');assert.equal(surface.rows.length,sourceRows.length);
        for(const [j,row] of surface.rows.entries()){assert.equal(row.role,'MenuItem');assert.equal(row.activeBackgroundOpacity,0);assert.equal(row.name,row.label);assert(row.box.width>0&&row.box.height>0);if(row.label.startsWith('Unavailable'))assert.equal(row.enabled,false);const expectedRow={...sourceRows[j].box,x:sourceRows[j].box.x+expectedBox.x-sourceSurface.box.x,y:sourceRows[j].box.y+expectedBox.y-sourceSurface.box.y};box(row.box,expectedRow,s.kind+'.surface'+i+'.row'+j);}
      }
      assert.deepEqual(m.events,[]);
      const checked=m.surfaces.flatMap(s=>s.rows).filter(r=>r.label==='Show fictional guides');
      if(checked.length)assert.equal(checked[0].isChecked,s.kind==='checked-beta'?'On':'Off');
      const radio=m.surfaces.flatMap(s=>s.rows).find(r=>r.label==='Fictional Beta mode');
      if(radio)assert.equal(radio.isChecked,s.kind==='checked-beta'?'True':'False');
    }
    for(const accepted of [false,true])for(const action of ['checkbox','radio','action','nested','deep']) {
      const match=contracts.filter(c=>c.action===action&&c.accepted===accepted);assert.equal(match.length,1);
      const c=match[0];assert.equal(c.passed,true);assert.equal(c.retiredInvokeRejected,true);assert.equal(c.events.length,1);
      assert.deepEqual(c.events,action==='checkbox'?[{key:'checked',value:true}]:action==='radio'?[{key:'radio',value:'b'}]:[action==='action'?'new':action]);
      assert.equal(c.originalActionObjectRetained,!(accepted&&['checkbox','radio'].includes(action)));
      const rows=c.reopened.surfaces[0].rows;
      assert.equal(rows.find(r=>r.label==='Show fictional guides').isChecked,accepted&&action==='checkbox'?'On':'Off');
      assert.equal(rows.find(r=>r.label==='Fictional Beta mode').isChecked,accepted&&action==='radio'?'True':'False');
    }
    assert.deepEqual(contracts.find(c=>c.action==='active-modal-isolation'),{scheme,action:'active-modal-isolation',passed:true,backgroundProviderRejections:3,nestedProviderRejections:2,closingProviderRejections:2,retiredOwnerProviderRejected:true,backgroundCalls:1,ownedCalls:2,originalBackgroundRowsAndBindingRetained:true,nestedOriginalRowRetained:true,unopenedTriggerRestored:true,originalModalContentRetained:true});
    const collection=contracts.find(c=>c.action==='live-collections');assert(collection.passed&&collection.originalCollectionRetained&&collection.originalTriggerRetained);
    const throwing=contracts.find(c=>c.action==='throwing-callback');assert.equal(throwing.passed,true);assert.equal(throwing.after.surfaces.length,1);assert.deepEqual(throwing.after.events,['new']);
    const unload=contracts.find(c=>c.action==='disable-unload-reload');assert(unload.passed&&unload.originalMenusRetained&&unload.templateRetirementVerified);
    assert.deepEqual(native.presence.find(p=>p.scheme===scheme),{scheme,closingOriginalSurfaceRetainedAt50:true,retiredBy190:true,reopenCancelledOriginalExit:true,normalMotionVerified:false,passed:true});
    const standalone=native.standalone.find(p=>p.scheme===scheme);
    const {viewportPlacement,...standaloneContract}=standalone;
    assert.deepEqual(standaloneContract,{scheme,originalCallbackCount:1,rootConstrained:true,noNativeLightDismiss:true,unloadedPopupRetired:true,sourceRasterParityVerified:false,passed:true});
    assert.equal(viewportPlacement.width,900);assert.equal(viewportPlacement.height,760);
    const within=box=>Number.isFinite(box.x)&&Number.isFinite(box.y)&&Number.isFinite(box.width)&&Number.isFinite(box.height)&&box.x>=0&&box.y>=0&&box.x+box.width<=viewportPlacement.width+.5&&box.y+box.height<=viewportPlacement.height+.5;
    assert(within(viewportPlacement.root)&&within(viewportPlacement.submenu),'Menubar surface escaped the native viewport');
    assert(viewportPlacement.root.y+viewportPlacement.root.height<=viewportPlacement.triggerTop,'bottom root did not open above its trigger');
    assert(viewportPlacement.submenu.x+viewportPlacement.submenu.width<=viewportPlacement.parentRowLeft,'right-edge submenu did not flip left');
    if(catalogExpected){const c=native.catalog.find(c=>c.scheme===scheme);assert.equal(c.bars,4);assert.equal(c.originalStylesApplied,true);assert.equal(c.registeredFixture,'Orchard.Catalog.MenubarsFixture');}
  }
  return{...metadata,geometryValues,maximumGeometryError,geometryTolerance:.02,radioSelectionProviderCases:native.radioSelection.length,keyboardControllerCases:native.keyboard.length,keyboardRoutedEventsVerified:false,keyboardFocusMovementVerified:false,controlledSurfaceMotionSamples:32,controlledSurfaceMotionVerified:true,controlledRowMotionPresentations:rowPresentations,rowMotionColorProperties:rowPresentations*2,rowPaintOwnershipCases:2,rowMotionReferenceSha256:crypto.createHash('sha256').update(rowReferenceBytes).digest('hex'),rowMotionSourceBrowser:rowReference.browser,originalStaticSourceBrowser:reference.browser,normalRealtimeMotionVerified:false,sourceStatesNotExercised:['font200'],documentedSourceAdaptations:['disabled submenu stays closed; UI Automation expand rejects disabled entries','bottom-edge root opens above trigger and right-edge submenu flips left within native viewport'],sourceRasterParityVerified:false,nativeComponentAccepted:false};
}

export function verifyMenubarPaint(state,png) {
  let probes=0;
  const trigger=state.measurement.triggers.find(t=>t.expanded==='Expanded');
  if(!trigger)return 0;
  const x=Math.floor(trigger.box.x+10),y=Math.floor(trigger.box.y+3),at=(y*png.width+x)*4;
  assert(x>=0&&x<png.width&&y>=0&&y<png.height);
  const expected=state.scheme==='Light'?[44,87,132,255]:[91,163,245,255];
  // The flipped root casts its real overlay shadow across the nearby trigger.
  // All other states retain the unobscured exact-palette probe.
  if(state.kind!=='bottom-right'){assert.deepEqual([...png.rgba.subarray(at,at+4)],expected,state.kind+' active trigger');probes++;}
  for(const row of state.measurement.surfaces.flatMap(s=>s.rows).filter(row=>row.expanded==='Expanded')) {
    const x=Math.floor(row.box.x+10),y=Math.floor(row.box.y+3),at=(y*png.width+x)*4;
    assert(x>=0&&x<png.width&&y>=0&&y<png.height);
    assert.deepEqual([...png.rgba.subarray(at,at+4)],state.scheme==='Light'?[255,255,255,255]:[24,37,51,255],state.kind+' expanded submenu row');probes++;
  }
  return probes;
}

export function verifyMenubarPng(frame,bytes) {
  assert.equal(digest(bytes),frame.sha256);
  const png=readRgbaPng(bytes);assert.equal(png.width,900);assert.equal(png.height,760);assert.equal(frame.width,900);assert.equal(frame.height,760);return png;
}

export async function verifyMenubarFiles(folder,hostAssembly) {
  const nativeBytes=await fs.readFile(path.join(folder,'menubar.json')),native=JSON.parse(nativeBytes),referenceBytes=await fs.readFile('tests/Reference/Menubar.json'),reference=JSON.parse(referenceBytes);
  const geometry=verifyMenubar(native,reference,hostAssembly==='Orchard.Catalog.dll');
  assert.equal(digest(await fs.readFile(path.join(folder,'source-reference.json'))),digest(referenceBytes));
  const process=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert(process.exited&&process.exitCode===0&&!process.timedOut);
  assert.equal(digest(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),native.librarySha256);
  assert.equal(digest(await fs.readFile(path.join(folder,'app-identity',hostAssembly))),native.hostSha256);
  let paletteProbes=0,pixelsValidated=0;
  for(const frame of native.pngs) {
    assert.equal(path.basename(frame.file),frame.file);
    const bytes=await fs.readFile(path.join(folder,frame.file));const png=verifyMenubarPng(frame,bytes);pixelsValidated+=png.width*png.height;
    const state=native.states.find(s=>s.file===frame.file);if(state)paletteProbes+=verifyMenubarPaint(state,png);
  }
  return{format:'orchard-menubar-native-audit-v1',passed:true,states:native.states.length,contracts:native.contracts.length,presenceHistories:native.presence.length,standalonePopupHistories:native.standalone.length,catalogStates:native.catalog.length,pngs:native.pngs.length,pixelsValidated,paletteProbes,...geometry,publicCallerOwnershipVerified:true,hiddenBoundaryVerified:true,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false,librarySha256:native.librarySha256,hostSha256:native.hostSha256,reportSha256:digest(nativeBytes),referenceSha256:digest(referenceBytes)};
}

if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href) {
  const arg=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],name);return process.argv[i+1];};
  const result=await verifyMenubarFiles(arg('--native'),arg('--host-assembly'));await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
