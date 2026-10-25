import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifyAuthShellLayoutNative,verifyAuthShellLayoutNativeModel,verifyAuthShellLayoutNativePixels} from './verify-auth-shell-layout.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const[folder,host,output]=process.argv.slice(2);assert(folder&&host&&output);await verifyAuthShellLayoutNative(folder,host);const original=JSON.parse(await fs.readFile(path.join(folder,'auth-shell-layout.json'))),source=JSON.parse(await fs.readFile(path.join(folder,'source-reference.json'))),catalogExpected=host==='Orchard.Catalog.dll',rejected=[];
const row=(r,id='split-start-host-600-font-16-ltr',theme='Light')=>r.cases.find(c=>c.id===id&&c.theme===theme),contract=(r,id)=>r.contracts.find(c=>c.id===id&&c.theme==='Light');
const reject=(name,mutate)=>{const r=structuredClone(original);mutate(r);assert.throws(()=>verifyAuthShellLayoutNativeModel(r,source,{catalogExpected}),undefined,name);rejected.push(name);};
for(const[name,mutate]of[
 ['false pass',r=>r.passed=false],['missing state',r=>r.cases.pop()],['missing contract',r=>r.contracts.pop()],['native focus',r=>r.focusEvents=1],['native activation',r=>r.activations=1],['visible host',r=>r.hostVisible=true],['physical input sent',r=>r.hardwareInputSent=true],['auth service added',r=>r.authenticationImplemented=true],['external browser opened',r=>r.externalBrowserOpened=true],['clipboard changed',r=>r.clipboardModified=true],['false physical claim',r=>r.physicalInputVerified=true],['false ARM64 claim',r=>r.nativeArm64RuntimeVerified=true],['false acceptance',r=>r.nativeComponentAccepted=true],['guard missing',r=>r.guards=0],['wrong rasterization',r=>r.rasterizationScale=2],
 ['replaced access region',r=>contract(r,'retained-side-resize').sameAccess=false],['replaced brand editor',r=>contract(r,'retained-side-resize').sameBrand=false],['replaced footer editor',r=>contract(r,'retained-side-resize').sameFooter=false],['replaced aside editor',r=>contract(r,'retained-side-resize').sameAside=false],['hidden aside unmounted',r=>contract(r,'retained-side-resize').hiddenAsideMounted=false],['typed original values lost',r=>contract(r,'retained-side-resize').typedValuesRetained=false],['template clones model',r=>contract(r,'templated-aside-remount').sameOriginalModel=false],['template root not detached',r=>contract(r,'templated-aside-remount').oldDetached=false],['template root reused after remount',r=>contract(r,'templated-aside-remount').differentRoot=false],['remount retains uncontrolled input',r=>contract(r,'templated-aside-remount').resetEditor=false],['selector receives replacement',r=>contract(r,'original-selector-model').receivedOriginal=false],['selector never sees original',r=>contract(r,'original-selector-model').originalSelections=0],['selector template replaced',r=>contract(r,'original-selector-model').sameTemplateRoot=false],['binding not updated',r=>contract(r,'original-selector-model').liveBindingUpdated=false],['caller callback lost',r=>contract(r,'original-local-action').invocations=0],['disabled callback invoked',r=>contract(r,'disabled-native-action').invocations=1],['disabled native activation accepted',r=>contract(r,'disabled-native-action').rejected=false],['native style overridden',r=>contract(r,'live-native-brushes').callerStyleRetained=false]
])reject(name,mutate);
for(const[name,mutate]of[
 ['host width changed',r=>row(r).snapshot.host.box.width++],
 ['centered access incorrectly stretched',r=>row(r,'centered-end-height900').snapshot.access.box.height=900],
 ['collapsed track erases padding',r=>row(r,'split-start-host-300-font-16-ltr').snapshot.access.box.width=0],
 ['explicit root width centered',r=>row(r,'centered-end-width320').snapshot.main.box.x=290],
 ['wrong native access color',r=>row(r).snapshot.access.background.r++],
 ['wrong native dark aside',r=>row(r,undefined,'Dark').snapshot.aside.background.r=26],
 ['wrong footer typeface size',r=>row(r).snapshot.footer.fontSize++],
 ['wrong caller typeface',r=>row(r).snapshot.caller.content.fontFamily='Segoe UI'],
 ['wrong caller font size',r=>row(r).snapshot.caller.content.fontSize=14],
 ['main landmark removed',r=>row(r).snapshot.main.landmark='None'],
 ['aside label removed',r=>row(r).snapshot.aside.name=''],
 ['decoration receives input',r=>row(r).snapshot.circles[0].hitTestVisible=true]
])reject(name,mutate);
for(const key of['main','access','inner','content','contentInner','brand','footer','aside'])for(const field of['x','y','width','height'])reject(key+' geometry '+field,r=>row(r).snapshot[key].box[field]++);
const copy=path.join(path.dirname(output),path.basename(output,'.json')+'-counterexamples');assert(!await fs.stat(copy).catch(()=>null));await fs.cp(folder,copy,{recursive:true});
for(const[name,file]of[['damaged actual DLL','app-identity/Orchard.dll'],['damaged actual source','source-reference.json'],['damaged actual PNG',original.pngs[0].file]]){const p=path.join(copy,file),bytes=await fs.readFile(p);try{await fs.writeFile(p,Buffer.from('actual damaged bytes'));await assert.rejects(verifyAuthShellLayoutNative(copy,host),undefined,name);rejected.push(name);}finally{await fs.writeFile(p,bytes);}}
for(const theme of['Light','Dark']){const c=row(original,undefined,theme),png=readRgbaPng(await fs.readFile(path.join(folder,c.file))),x=Math.floor(c.snapshot.access.box.x+2),i=(2*png.width+x)*4;png.rgba[i]^=1;assert.throws(()=>verifyAuthShellLayoutNativePixels(c,png,source.cases.find(e=>e.id===c.id&&e.scheme===c.theme).spec));rejected.push(theme+' actual pixel changed by one');}
const report={passed:true,rejected:rejected.length,cases:rejected,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(report,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({passed:true,rejected:rejected.length}));
