import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const pinned=[
 ['src/components/sonner.tsx','9abd2f6c1f907708e499170c6368e20f8400afb0c0c5dc07f60fb4635289154d'],
 ['src/components/button.tsx','119925584b3ec53c790fb0b762631894cf70eed12d488d5e9ae6743b7a67f929'],
 ['src/utils/cn.ts','74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883'],
 ['src/core/index.ts','b2544fa33ecb1693bf963e93b516a38a5794cba32a2aa5a1245e9c24a13c1ea6'],
 ['src/core/contracts.ts','893d004ad528303c7c6894db3689e644ba26058b8290d68713d012182eba5f90'],
 ['src/styles/global.css','fa7278f1107d946977c474002a09047be1e83858631f93b056435a3bbf038d96'],
 ['src/styles/tokens.generated.css','74468d6dd3516687e3f8b96aef47e10bdcce206e31edc912abe985c45b83bf3e'],
 ['package-lock.json','6ce0a460b61d1087b9d51ed49f949ba264005ba8bfbe0fc26260e7c8bf16f437'],
 ['stories/components/Sonner.stories.tsx','19fea068a682df89355c3318716552e470be42b475373e520addbe476ceabb46']
];
const positions=['top-left','top-center','top-right','bottom-left','bottom-center','bottom-right'];
const variants=['default','success','error','warning','info'];
const actions=['default','primary','secondary','outline','subtle','ghost','destructive','link'];
const states=['empty',...variants.map(v=>'variant-'+v),...positions.map(v=>'position-'+v),...actions.map(v=>'action-'+v),'empty-title','empty-description','long','unicode','rtl','narrow','font200','stack','unbounded-stack',...['action','dismiss'].flatMap(t=>['hover','focus','pressed'].map(s=>t+'-'+s))];
const historyIds=['default-timeout','button-dismiss','api-dismiss','action-retained','action-adds','action-dismisses','action-throws','persistent-values','default-change','duration-override','short-timeout-before-frame','button-before-frame','retained-action','unknown-dismiss','escape','hover-focus-no-pause','convenience-methods','unmounted-api','multiple-host-cleanup'];
const near=(actual,expected,message)=>assert(Math.abs(actual-expected)<.001,`${message}: ${actual} vs ${expected}`);
const flat=step=>step.value.hosts.flatMap(h=>h.items);
export function verifySonnerModel(r){
 assert.equal(r.format,'orchard-sonner-reference-v1');assert.equal(r.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.deepEqual(r.dependencies.map(d=>[d.path,d.sha256]),pinned);
 assert.deepEqual(r.modules.map(m=>[m.name,m.version]),[['react','19.2.4'],['react-dom','19.2.4'],['lucide-react','0.563.0']]);
 assert.equal(r.toolingModules.find(m=>m.name==='tailwindcss').version,'4.2.2');assert.equal(r.toolingModules.find(m=>m.name==='@tailwindcss/cli').version,'4.2.2');
 for(const input of [...r.dependencies,...r.fonts,...r.bundledFiles,...r.modules.map(m=>({sha256:m.packageSha256})),...r.toolingModules.map(m=>({sha256:m.packageSha256}))])assert.match(input.sha256,/^[a-f0-9]{64}$/);
 assert.deepEqual(r.fonts.map(f=>f.weight),[400,500,600]);assert.deepEqual(r.viewport,{width:1000,height:760});assert.equal(r.reducedMotion,'reduce');assert.equal(r.networkRequests,0);assert.equal(r.nativeImplemented,false);assert.equal(r.nativeComponentAccepted,false);
 assert.equal(r.cases.length,70);assert.equal(r.histories.length,38);
 for(const scheme of ['Light','Dark']){
  const cases=r.cases.filter(c=>c.scheme===scheme),histories=r.histories.filter(h=>h.scheme===scheme);assert.deepEqual(cases.map(c=>c.id),states);assert.deepEqual(histories.map(h=>h.id),historyIds);
  const primary=scheme==='Light'?'rgb(45, 45, 45)':'rgb(240, 244, 248)',secondary=scheme==='Light'?'rgb(89, 89, 89)':'rgb(184, 197, 214)',surface=scheme==='Light'?'rgb(255, 255, 255)':'rgb(24, 37, 51)';
  const colors=scheme==='Light'?['rgb(89, 89, 89)','rgb(44, 87, 132)','rgb(166, 93, 87)','rgb(255, 150, 119)','rgb(193, 129, 77)']:['rgb(184, 197, 214)','rgb(91, 163, 245)','rgb(212, 123, 117)','rgb(212, 175, 55)','rgb(212, 175, 55)'];
  for(const c of cases){
   assert.equal(path.basename(c.file),c.file);assert.match(c.pngSha256,/^[a-f0-9]{64}$/);assert.equal(c.measurement.hosts.length,1);const h=c.measurement.hosts[0],f=(c.spec.rootFontSize??16)/16,viewportWidth=c.spec.viewportWidth??1000,viewportHeight=c.spec.viewportHeight??760,position=c.spec.position??'bottom-right';
   assert.equal(h.label,'Notifications');assert.equal(h.live,'polite');assert.equal(h.paint.pointerEvents,'none');assert.deepEqual(h.paint.padding,[16*f,16*f,16*f,16*f]);assert.equal(h.items.length,c.toasts.length);
   const width=Math.min(352*f,viewportWidth),height=32*f+h.items.reduce((sum,i)=>sum+i.box.height,0)+Math.max(0,h.items.length-1)*8*f;
   near(h.box.width,width,c.id+' width');near(h.box.height,height,c.id+' height');near(h.box.x,position.endsWith('left')?16*f:position.endsWith('center')?(viewportWidth-width)/2:viewportWidth-16*f-width,c.id+' x');near(h.box.y,position.startsWith('top')?16*f:viewportHeight-16*f-height,c.id+' y');
   let y=h.box.y+16*f;
   for(let index=0;index<h.items.length;index++){
    const item=h.items[index],t=c.toasts[index];near(item.box.x,h.box.x+16*f,c.id+' item x');near(item.box.y,y,c.id+' item y');near(item.box.width,width-32*f,c.id+' item width');y+=item.box.height+8*f;
    assert.equal(item.role,'alert');assert.equal(item.visibleClass,true);assert.equal(item.paint.opacity,1);assert.equal(item.paint.background,surface);assert.equal(item.paint.radius,'12px');assert.equal(item.paint.borderWidth,'1px');assert.deepEqual(item.paint.padding,[16*f,16*f,16*f,16*f]);assert.equal(item.paint.gap,12*f+'px');assert.equal(item.paint.pointerEvents,'auto');assert.equal(item.paint.transitionDuration,'1e-05s');
    assert.equal(item.title.text,t.title??'Fictional notification');assert.equal(item.title.paint.color,primary);assert.equal(item.title.paint.fontSize,14*f+'px');assert.equal(item.title.paint.lineHeight,20*f+'px');assert.equal(item.title.paint.fontWeight,'500');
    near(item.title.box.width,item.box.width-34-60*f,c.id+' text width');assert.equal(item.description.length,t.description?1:0);
    for(const d of item.description){assert.equal(d.text,t.description);assert.equal(d.paint.color,secondary);assert.equal(d.paint.fontSize,12*f+'px');assert.equal(d.paint.lineHeight,16*f+'px');near(d.box.y,item.title.box.y+item.title.box.height+2*f,c.id+' description gap');}
    assert.equal(item.icons.length,2);near(item.icons[0].box.width,18,c.id+' icon');near(item.icons[0].box.height,18,c.id+' icon');assert.equal(item.icons[0].paint.color,colors[variants.indexOf(t.variant??'default')]);near(item.icons[1].box.width,14*(c.id==='dismiss-pressed'?.9:1),c.id+' close icon');
    assert.equal(item.buttons.length,t.hasAction?2:1);const dismiss=item.buttons.at(-1);assert.equal(dismiss.name,'Dismiss');assert.equal(dismiss.text,'');assert.equal(dismiss.paint.scale,c.id==='dismiss-pressed'?'0.9':'1');
    let textHeight=item.title.box.height+(item.description[0]?2*f+item.description[0].box.height:0);
    if(t.hasAction){const action=item.buttons[0];assert.equal(action.text,t.actionLabel??'Continue');assert.equal(action.paint.fontSize,12*f+'px');near(action.box.height,(t.actionVariant==='link'?16:36)*f,c.id+' action height');near(action.box.y,item.title.box.y+textHeight+8*f,c.id+' action gap');
     // The source's inline link shares its parent's 16px/24px line box;
     // its trailing descent is 2px below the button, unlike inline-flex Button.
     textHeight+=8*f+action.box.height+(t.actionVariant==='link'?2*f:0);}
    near(item.box.height,2+32*f+Math.max(18+2*f,14+4*f,textHeight),c.id+' intrinsic height');
   }
  }
  const c=id=>cases.find(c=>c.id===id),h=id=>histories.find(h=>h.id===id),step=(id,label)=>{const s=h(id).steps.find(s=>s.label===label);assert(s,id+' '+label);return s;},expect=(id,label,titles,visible)=>{const items=flat(step(id,label));assert.deepEqual(items.map(i=>i.title),titles,id+' '+label);assert.deepEqual(items.map(i=>i.visible),visible,id+' '+label);};
  assert.equal(c('empty').measurement.hosts[0].items.length,0);assert.equal(c('empty-title').measurement.hosts[0].items[0].title.box.height,0);assert.equal(c('long').measurement.hosts[0].items[0].box.height,124);assert.equal(c('narrow').measurement.hosts[0].box.x,-16);assert.equal(c('font200').measurement.hosts[0].items[0].box.height,230);
  assert.equal(c('rtl').measurement.hosts[0].items[0].buttons.at(-1).box.x,665);assert.equal(c('action-focus').measurement.focus,'Continue');assert.equal(c('dismiss-focus').measurement.focus,'Dismiss');assert.equal(c('unbounded-stack').measurement.hosts[0].items.length,12);assert(c('unbounded-stack').measurement.hosts[0].items[0].box.y<0);
  assert.deepEqual(c('action-default').measurement.hosts[0].items,c('action-primary').measurement.hosts[0].items);
  for(const entry of histories){assert.equal(entry.retainedReactInstance,true);assert(Array.isArray(entry.issuedIds));const ids=entry.issuedIds.filter(Boolean);assert.equal(new Set(ids).size,ids.length);for(const id of ids)assert.equal(typeof id,'string');for(const s of entry.steps)for(const event of s.value.events){assert.equal(event.kind,'action');assert.equal(event.original,true);assert(event.id);}}
  const title=['Fictional notification'];expect('default-timeout','added',title,[false]);expect('default-timeout','frame',title,[true]);expect('default-timeout','before-timeout',title,[true]);expect('default-timeout','hidden-at-4000',title,[false]);expect('default-timeout','retained-299',title,[false]);expect('default-timeout','removed-300',[],[]);
  expect('button-dismiss','before',title,[true]);expect('button-dismiss','hidden',title,[false]);expect('button-dismiss','retained-299',title,[false]);expect('button-dismiss','removed-300',[],[]);expect('api-dismiss','removed-immediately',[],[]);
  for(const id of ['action-retained','action-throws']){expect(id,'after-action',title,[true]);assert.equal(step(id,'after-action').value.events.length,1);}
  expect('action-adds','after-action',[...title,'Fictional follow-up'],[true,true]);expect('action-dismisses','after-action',[],[]);assert.equal(step('action-dismisses','after-action').value.events.length,1);
  expect('persistent-values','after-5000',Array(5).fill(title[0]),Array(5).fill(true));expect('default-change','old-hidden',['Original timer','Later timer'],[false,true]);expect('default-change','old-removed',['Later timer'],[true]);expect('default-change','new-before',['Later timer'],[true]);expect('default-change','new-hidden',['Later timer'],[false]);
  expect('duration-override','frame',['Persistent override','Explicit timer'],[true,true]);expect('duration-override','explicit-hidden',['Persistent override','Explicit timer'],[true,false]);expect('duration-override','persistent-only',['Persistent override'],[true]);
  expect('short-timeout-before-frame','timed-out',title,[false]);expect('short-timeout-before-frame','late-frame',title,[true]);expect('short-timeout-before-frame','removed',[],[]);expect('button-before-frame','hidden',title,[false]);expect('button-before-frame','late-frame',title,[true]);expect('button-before-frame','removed',[],[]);
  expect('retained-action','hidden',title,[false]);expect('retained-action','action-while-retained',title,[false]);assert.equal(step('retained-action','action-while-retained').value.events.length,1);expect('retained-action','removed',[],[]);
  for(const id of ['escape','unknown-dismiss'])expect(id,'retained',title,[true]);expect('hover-focus-no-pause','hidden-at-deadline',title,[false]);expect('convenience-methods','ordered',['success','error','warning','info'],[true,true,true,true]);
  assert.equal(h('unmounted-api').returnedId,'');assert.equal(step('unmounted-api','unmounted').value.hosts.length,0);assert.equal(h('multiple-host-cleanup').returnedId,'');const before=step('multiple-host-cleanup','last-host').value.hosts;assert.equal(before.length,2);assert.equal(before[0].items.length,0);assert.deepEqual(before[1].items.map(i=>i.title),['Last mounted']);expect('multiple-host-cleanup','earlier-unmounted',['Last mounted'],[true]);
 }
}
export function verifySonnerPaint(c,png){
 let probes=0;for(const item of c.measurement.hosts.flatMap(h=>h.items)){const x=Math.floor(item.box.x+8),y=Math.floor(item.box.y+item.box.height/2);if(x<0||x>=png.width||y<0||y>=png.height)continue;const at=(y*png.width+x)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],c.scheme==='Light'?[255,255,255,255]:[24,37,51,255],c.id+' flat surface');probes++;}return probes;
}
export async function verifySonnerSource(referenceFile,frames,recapture,{verifyBundleInputs=true}={}){
 const bytes=await fs.readFile(referenceFile),r=JSON.parse(bytes);verifySonnerModel(r);assert.equal(hash(await fs.readFile(new URL('./capture-sonner.cjs',import.meta.url))),r.scriptSha256);assert.equal(hash(await fs.readFile(new URL('./sonner-client.tsx',import.meta.url))),r.clientSha256);
 assert.equal(hash(await fs.readFile(path.join(frames,'source.css'))),r.cssSha256);assert.equal(hash(await fs.readFile(path.join(frames,'bundle.js'))),r.bundleSha256);
 if(recapture)assert.deepEqual(JSON.parse(await fs.readFile(path.join(recapture,'Sonner.json'))),r);
 for(const input of r.dependencies){assert(!input.path.includes('..')&&!path.isAbsolute(input.path));assert.equal(hash(await fs.readFile(path.join(frames,'Source',input.path))),input.sha256);if(recapture)assert.deepEqual(await fs.readFile(path.join(recapture,'Source',input.path)),await fs.readFile(path.join(frames,'Source',input.path)));}
 if(verifyBundleInputs)for(const input of r.bundledFiles)assert.equal(hash(await fs.readFile(input.path)),input.sha256,input.path);
 let pixelsValidated=0,paletteProbes=0;for(const c of r.cases){const bytes=await fs.readFile(path.join(frames,c.file));assert.equal(hash(bytes),c.pngSha256);const png=readRgbaPng(bytes);assert.equal(png.width,c.spec.viewportWidth??1000);assert.equal(png.height,c.spec.viewportHeight??760);pixelsValidated+=png.width*png.height;paletteProbes+=verifySonnerPaint(c,png);if(recapture)assert.deepEqual(await fs.readFile(path.join(recapture,c.file)),bytes,c.file);}
 return{format:'orchard-sonner-source-audit-v1',passed:true,sourceStates:r.cases.length,retainedInstanceHistories:r.histories.length,pinnedSourceFiles:r.dependencies.length,bundledFilesDeclared:r.bundledFiles.length,bundledFilesVerified:verifyBundleInputs?r.bundledFiles.length:0,frozenOnly:!verifyBundleInputs,pngs:r.cases.length,pixelsValidated,paletteProbes,independentRecaptureByteIdentical:!!recapture,sourceReferenceSha256:hash(bytes),nativeImplemented:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);return i<0?null:process.argv[i+1];};const reference=option('--reference'),frames=option('--frames'),output=option('--output');assert(reference&&frames&&output);const result=await verifySonnerSource(reference,frames,option('--recapture'),{verifyBundleInputs:!process.argv.includes('--frozen-only')});await fs.writeFile(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));}
