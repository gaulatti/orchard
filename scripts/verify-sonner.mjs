import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const contractIds=['active-modal-isolation','original-action-command','api-dismiss-immediate','default-timeout','fractional-timeout','wrapped-timeout','nan-persistent','explicit-duration-override','live-default-capture','global-variants','multiple-host-cleanup','disabled-hidden-owner-ancestor','unload-reload-retirement','live-theme-retained'];
export function verifySonnerReport(native,reference,hostAssembly){
 assert.equal(native.format,'orchard-sonner-native-v1');assert.equal(native.passed,true);assert.equal(native.focus,0);assert.equal(native.activations,0);assert(native.guards>100);
 for(const field of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(native[field],false,field);
 assert.equal(native.publicApisOnly,true);assert.equal(native.states.length,66);assert.equal(native.contracts.length,28);assert.equal(native.presence.length,2);assert.equal(native.standalone.length,2);assert.equal(native.catalog.length,hostAssembly==='Orchard.Catalog.dll'?2:0);assert.equal(native.pngs.length,66+native.catalog.length);
 assert(['X64','Arm64'].includes(native.processArchitecture));assert.equal(native.osArchitecture,native.processArchitecture);for(const c of native.catalog){assert.equal(c.stylesApplied,true);assert.equal(c.publicStylesVerified,4);}
 let geometryValues=0,maximumGeometryError=0,paletteValues=0;
 const close=(actual,expected,label)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),label);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);assert(error<=.02,label+': '+actual+' != '+expected);geometryValues++;};
 const box=(actual,expected,label)=>{for(const field of ['x','y','width','height'])close(actual[field],expected[field],label+' '+field);};
 const rgb=css=>{const match=/^rgb\((\d+), (\d+), (\d+)\)$/.exec(css);assert(match,css);return match.slice(1).map(Number);};
 // BleeckerContentText emits explicit presentation line breaks. Source <p>
 // uses CSS normal whitespace; compare that display value while separately
 // requiring the original immutable caller object to remain attached.
 const displayText=value=>value.replace(/[\t\n\f\r ]+/g,' ').replace(/^ | $/g,'');
 const ink=(actual,expected,label)=>{assert.deepEqual([actual.r,actual.g,actual.b],rgb(expected),label);assert.equal(actual.a,255,label);assert.equal(actual.opacity,1,label);paletteValues++;};
 for(const scheme of ['Light','Dark']){
  const expected=reference.cases.filter(c=>c.scheme===scheme&&!c.id.endsWith('focus')),states=native.states.filter(c=>c.scheme===scheme);assert.deepEqual(states.map(c=>c.id),expected.map(c=>c.id));
  for(let index=0;index<states.length;index++){
   const c=states[index],source=expected[index],m=c.snapshot,h=source.measurement.hosts[0];assert.equal(path.basename(c.file),c.file);assert.equal(c.forcedTemplateState,source.state!==null);assert.equal(m.host.name,'Notifications');assert.equal(m.host.live,'Polite');box(m.host.box,h.box,c.id+' host');assert.equal(m.items.length,h.items.length);
   for(let i=0;i<m.items.length;i++){
    const actual=m.items[i],wanted=h.items[i];assert.equal(actual.originalRetained,true);assert.equal(actual.actionOriginalRetained,true);assert.equal(actual.opacity,1);assert.equal(actual.radius,12);box(actual.box,wanted.box,c.id+' card');ink(actual.background,wanted.paint.background,c.id+' background');
    assert.deepEqual([actual.border.r,actual.border.g,actual.border.b],scheme==='Light'?[230,213,184]:[255,255,255]);assert.equal(actual.border.a,255);assert.equal(actual.border.opacity,Math.fround(scheme==='Light'?.25:.1));paletteValues++;
    const text=(a,w,label)=>{assert.equal(displayText(a.text),displayText(w.text),label+' CSS-normal display text');box(a.box,w.box,label);ink(a.foreground,w.paint.color,label+' ink');assert.equal(a.fontSize,parseFloat(w.paint.fontSize));assert.equal(a.lineHeight,parseFloat(w.paint.lineHeight));assert.equal(a.fontWeight,Number(w.paint.fontWeight));};
    text(actual.title,wanted.title,c.id+' title');assert.equal(actual.description.length,wanted.description.length);for(let d=0;d<actual.description.length;d++)text(actual.description[d],wanted.description[d],c.id+' description');box(actual.icon,wanted.icons[0].box,c.id+' icon');
    assert.equal(actual.buttons.length,wanted.buttons.length);for(let b=0;b<actual.buttons.length;b++){const button=actual.buttons[b],want=wanted.buttons[b];assert.equal(button.name,want.name??want.text,c.id+' button name');assert.equal(button.enabled,true);assert.equal(button.role,'Button');box(button.box,want.box,c.id+' button');ink(button.foreground,want.paint.color,c.id+' button ink');}
   }
  }
  const contracts=native.contracts.filter(c=>c.scheme===scheme);assert.deepEqual(contracts.map(c=>c.id),contractIds);for(const c of contracts)assert.equal(c.passed,true);
  const action=contracts.find(c=>c.id==='original-action-command').details;assert.equal(action.originalIdentity,true);assert.equal(action.called,2);assert.equal(action.denied,true);
  assert.deepEqual(contracts[0].details,{variants:['Primary','Link'].map(variant=>({variant,backgroundProviderRejections:2,nestedProviderRejections:2,closingProviderRejected:true,backgroundCalls:2,ownedCalls:2,originalParameterRetained:true,originalToastAndActionRetained:true,nestedOriginalStateRestored:true,callerEnabledBindingRetained:true,retainedActionStillAllowed:true,ownedCommandsRevokedOnRemoval:true}))});
  const presence=native.presence.find(c=>c.scheme===scheme);for(const field of ['retainedAt80','retiredByDeadline','retainedActionInvoked','oldCommandRevoked'])assert.equal(presence[field],true);
  const standalone=native.standalone.find(c=>c.scheme===scheme);assert.equal(standalone.rootConstrained,true);assert.equal(standalone.unloadedPopupRetired,true);
 }
 return{geometryValues,maximumGeometryError,paletteValues};
}
export async function verifySonnerNative(folder,hostAssembly){
 const read=async file=>JSON.parse(await fs.readFile(path.join(folder,file))),native=await read('sonner.json'),referenceBytes=await fs.readFile(path.join(folder,'source-reference.json')),reference=JSON.parse(referenceBytes);
 assert.equal(hash(referenceBytes),'9a8a0c686b57d7d7db7b4db616739ff4f5add17975db071c8c09725bc7c8bba2');assert.equal(hash(await fs.readFile('tests/Reference/Sonner.json')),hash(referenceBytes));
 const counts=verifySonnerReport(native,reference,hostAssembly),exit=await read('process-exit.json');assert.equal(exit.exited,true);assert.equal(exit.timedOut,false);assert.equal(exit.exitCode,0);
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity/Orchard.dll'))),native.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',hostAssembly))),native.hostSha256);
 const executable=await fs.readFile(path.join(folder,'app-identity',hostAssembly.replace(/\.dll$/,'.exe'))),pe=executable.readUInt32LE(0x3c);assert.equal(executable.toString('binary',pe,pe+4),'PE\0\0');const machine=executable.readUInt16LE(pe+4);assert([0x8664,0xaa64].includes(machine));assert.equal(native.processArchitecture,machine===0x8664?'X64':'Arm64');
 let pngPixels=0,paletteProbes=0;const declared=new Map(native.pngs.map(p=>[p.file,p]));assert.equal(declared.size,native.pngs.length);
 for(const frame of [...native.states,...native.catalog]){
  const metadata=declared.get(frame.file);assert(metadata,frame.file);const bytes=await fs.readFile(path.join(folder,frame.file));assert.equal(hash(bytes),metadata.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,metadata.width);assert.equal(png.height,metadata.height);
  const source=frame.id?reference.cases.find(c=>c.id===frame.id&&c.scheme===frame.scheme):null;assert.equal(png.width,source?.spec.viewportWidth??1000);assert.equal(png.height,760);pngPixels+=png.width*png.height;
  if(frame.snapshot)for(const item of frame.snapshot.items){const x=Math.floor(item.box.x+8),y=Math.floor(item.box.y+item.box.height/2);if(x<0||x>=png.width||y<0||y>=png.height)continue;const at=(y*png.width+x)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],frame.scheme==='Light'?[255,255,255,255]:[24,37,51,255],frame.id+' native surface pixels');paletteProbes++;}
 }
 return{format:'orchard-sonner-native-audit-v1',passed:true,states:native.states.length,contracts:native.contracts.length,presenceHistories:native.presence.length,standaloneHistories:native.standalone.length,catalogStates:native.catalog.length,...counts,pngs:native.pngs.length,pngPixels,paletteProbes,hiddenBoundaryVerified:true,guards:native.guards,focusEvents:0,activationEvents:0,nativeExecutableArchitecture:native.processArchitecture.toLowerCase(),forcedTemplateStatesArePhysicalInput:false,sourceRasterParityVerified:false,nativeArm64RuntimeVerified:native.processArchitecture==='Arm64',nativeComponentAccepted:false,librarySha256:native.librarySha256,hostSha256:native.hostSha256};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const result=await verifySonnerNative(option('--native'),option('--host-assembly'));await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));}
