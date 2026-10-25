import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const pinned=[
 ['src/components/navigation-menu.tsx','937a1ff45c8a4058247968189c39a22bc444f1168c06b0b4b64200325e5a6d71'],
 ['src/components/nav-menu.tsx','1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5'],
 ['src/utils/cn.ts','74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883'],
 ['src/utils/hooks.ts','ca3d38ac220ddadaa1ff1a22eb6171992b17b50bae755b1186253fbcade92aed'],
 ['src/styles/global.css','fa7278f1107d946977c474002a09047be1e83858631f93b056435a3bbf038d96'],
 ['src/styles/tokens.generated.css','74468d6dd3516687e3f8b96aef47e10bdcce206e31edc912abe985c45b83bf3e'],
 ['package-lock.json','6ce0a460b61d1087b9d51ed49f949ba264005ba8bfbe0fc26260e7c8bf16f437'],
 ['stories/components/NavigationMenu.stories.tsx','39dec883bc52c533e1bc95e24f9b10e5ef37add5335a080cdfc156b0a4efa266']
];
const stateIds=['closed','empty','links-only','basic','empty-group','no-description','long-description','unicode','narrow','rtl','bottom-right','custom','custom-input','group-href-unused','layout-open','trigger-hover','direct-hover','link-hover','trigger-focus','child-focus','font200','trigger-pressed'];
const historyIds=['default-child','default-external','direct-while-open','custom-child','custom-direct-while-open','custom-ignored-close','custom-throw','outside-dismiss','inside-pointer','hover-other-group','toggle-root','switch-root','escape','arrow-down','live-label','live-description','replace-trigger','prepend-index','remove-active-index','tab-enter-child','custom-node-retained'];
const active=m=>m.triggers.filter(t=>t.expanded==='true').map(t=>t.text);
const anchor=(href,target=null,rel=null)=>({kind:'anchor',href,target,rel});

export function verifyNavigationMenuModel(r){
 assert.equal(r.format,'orchard-navigation-menu-reference-v1');assert.equal(r.commit,'a86059013cd59684ce452849278ea81e734f2066');
 assert.deepEqual(r.dependencies.map(d=>[d.path,d.sha256]),pinned);
 assert.equal(r.modules.find(m=>m.name==='react').version,'19.2.4');assert.deepEqual(r.fonts.map(f=>f.weight),[400,500,600]);
 for(const input of [...r.dependencies,...r.fonts,...r.bundledFiles,...r.modules.map(m=>({sha256:m.packageSha256}))])assert.match(input.sha256,/^[a-f0-9]{64}$/);
 assert.deepEqual(r.viewport,{width:1000,height:760});assert.equal(r.reducedMotion,'reduce');assert.equal(r.networkRequests,0);assert.equal(r.nativeImplemented,false);assert.equal(r.nativeComponentAccepted,false);
 assert.equal(r.cases.length,44);assert.equal(r.histories.length,42);assert.equal(r.presence.length,2);
 for(const scheme of ['Light','Dark']){
  const states=r.cases.filter(c=>c.scheme===scheme),histories=r.histories.filter(h=>h.scheme===scheme);assert.deepEqual(states.map(c=>c.id),stateIds);assert.deepEqual(histories.map(h=>h.id),historyIds);
  const c=id=>states.find(c=>c.id===id).measurement,h=id=>histories.find(h=>h.id===id),primary=scheme==='Light'?'rgb(45, 45, 45)':'rgb(240, 244, 248)';
  for(const s of states){assert.equal(path.basename(s.file),s.file);assert.match(s.pngSha256,/^[a-f0-9]{64}$/);const m=s.measurement;assert.equal(m.container.role,null);assert.equal(m.intrinsic.natural.length,m.links.length+m.triggers.length);assert.equal(m.intrinsic.minimum.length,m.intrinsic.natural.length);for(let i=0;i<m.intrinsic.natural.length;i++)assert(m.intrinsic.natural[i]>=m.intrinsic.minimum[i]&&m.intrinsic.minimum[i]>0);
   for(const t of m.triggers){assert.equal(t.hasPopup,null);assert.equal(t.paint.fontSize,'13px');assert.equal(t.paint.lineHeight,'19.5px');assert.equal(t.paint.fontWeight,'500');assert.equal(t.paint.radius,'7px');assert.equal(t.paint.letterSpacing,'normal');assert.equal(t.chevron.length,1);assert.equal(t.chevron[0].paint.rotate,t.expanded==='true'?'180deg':'0deg');assert.equal(t.chevron[0].box.width,14);}
   for(const surface of m.surfaces){assert.equal(surface.box.width,s.id==='font200'?512:256);assert(surface.box.height>0);assert.equal(surface.paint.background,scheme==='Light'?'rgb(255, 255, 255)':'rgb(24, 37, 51)');assert.equal(surface.visibleClass,true);assert.equal(surface.paint.opacity,1);assert.equal(surface.paint.scale,'1');assert.equal(surface.paint.radius,'7px');assert.deepEqual(surface.paint.padding,s.id==='font200'?[12,12,12,12]:[6,6,6,6]);for(const link of surface.links){assert.equal(link.parts[0].paint.color,primary);assert.equal(link.parts[0].paint.fontWeight,'500');if(link.parts[1])assert.equal(link.parts[1].paint.lineHeight,s.id==='font200'?'39px':'19.5px');}}
   for(const call of m.renderCalls)assert.equal(call.original,true);
  }
  assert.deepEqual(c('closed').container.box,{x:20,y:20,width:680,height:36});assert.equal(c('closed').container.paint.gap,'4px');assert.equal(c('empty').container.box.height,0);assert.equal(c('empty').triggers.length,0);assert.equal(c('empty').links.length,0);assert.equal(c('links-only').triggers.length,0);
  assert.deepEqual(c('basic').surfaces[0].box,{x:85.765625,y:59.75,width:256,height:198.5});assert.deepEqual(c('basic').surfaces[0].links.map(l=>l.box.height),[61.5,61.5,61.5]);
  assert.equal(c('basic').surfaces[0].links[1].href,'/fictional/forms?kind=one%2Ftwo#fields');assert.equal(c('basic').surfaces[0].links[2].target,'_blank');assert.equal(c('basic').surfaces[0].links[2].rel,'noreferrer noopener');
  assert.deepEqual(c('basic').intrinsic.natural,[61.765625,115.921875,83.09375,121.96875]);assert.deepEqual(c('basic').intrinsic.minimum,c('basic').intrinsic.natural);
  assert.equal(c('empty-group').surfaces[0].box.height,14);assert.equal(c('empty-group').surfaces[0].links.length,0);assert.equal(c('no-description').surfaces[0].box.height,134);assert(c('no-description').surfaces[0].links.every(l=>l.parts.length===1&&l.box.height===40));
  assert.equal(c('long-description').surfaces[0].links[0].box.height,100.5);assert.equal(c('long-description').surfaces[0].box.height,237.5);assert(c('unicode').surfaces[0].links[0].text.includes('Straße 東京 é العربية'));
  assert.equal(c('narrow').container.box.width,200);assert.deepEqual(c('narrow').triggers.map(t=>t.box),c('basic').triggers.map(t=>t.box));assert.equal(c('rtl').surfaces[0].box.x,518.3125);
  assert(c('bottom-right').surfaces[0].box.x+c('bottom-right').surfaces[0].box.width>1000);assert(c('bottom-right').surfaces[0].box.y+c('bottom-right').surfaces[0].box.height>760);
  assert(c('custom').surfaces[0].links.every(l=>l.custom&&l.marker));assert.equal(c('custom-input').surfaces[0].box.height,252.5);assert.equal(c('custom-input').surfaces[0].links[0].input[0].value,'retained original text');
  assert.deepEqual(c('group-href-unused').triggers,c('basic').triggers);assert.deepEqual(active(c('layout-open')),['Layout']);assert.equal(c('layout-open').surfaces[0].box.height,75.5);
  assert.equal(c('trigger-hover').triggers[0].paint.color,primary);assert.equal(c('direct-hover').links[0].paint.color,primary);assert.equal(c('link-hover').surfaces[0].links[0].parts[0].paint.color,primary);
  assert.equal(c('trigger-focus').focus,'Components');assert.equal(c('child-focus').focus,'ButtonsReusable fictional buttons.');assert.equal(c('font200').container.box.height,72);assert.equal(c('font200').triggers[0].box.height,51.5);assert.equal(c('font200').surfaces[0].box.height,395);
  assert.equal(c('trigger-pressed').triggers[0].paint.translate,'0px 1px');assert.equal(c('trigger-pressed').triggers[0].box.y,c('closed').triggers[0].box.y+1);assert.equal(c('trigger-pressed').surfaces.length,0);
  for(const entry of histories)assert.equal(entry.retainedReactInstance,true);
  for(const id of ['default-child','default-external','custom-child','outside-dismiss','toggle-root','tab-enter-child'])assert.equal(h(id).after.surfaces.length,0,id);
  assert.deepEqual(h('default-child').after.events,[anchor('#fictional-buttons')]);assert.deepEqual(h('default-external').after.events,[anchor('https://example.invalid/library','_blank','noreferrer noopener')]);
  assert.equal(h('default-child').after.focus,'BODY');assert.equal(h('default-external').after.focus,'BODY');assert.equal(h('custom-child').after.focus,'BODY');
  assert.deepEqual(active(h('direct-while-open').after),['Components']);assert.deepEqual(h('direct-while-open').after.events,[anchor('#fictional-home')]);
  for(const id of ['custom-child','custom-direct-while-open','custom-ignored-close','custom-throw']){const direct=id==='custom-direct-while-open';assert.deepEqual(h(id).after.events,[{kind:'custom',marker:direct?'original-home':'original-buttons',original:true,hasOnClick:!direct},anchor(direct?'#fictional-home':'#fictional-buttons')]);assert(h(id).after.renderCalls.every(call=>call.original));}
  for(const id of ['custom-direct-while-open','custom-ignored-close','custom-throw','inside-pointer','hover-other-group','escape','arrow-down'])assert.deepEqual(active(h(id).after),['Components'],id);
  assert(h('custom-child').before.renderCalls.some(call=>call.marker==='original-header'&&call.hasOnClick&&call.original));
  assert(h('custom-direct-while-open').after.renderCalls.some(call=>call.marker==='original-home'&&!call.hasOnClick&&call.original));
  assert.equal(h('outside-dismiss').after.focus,'outside-after');assert.equal(h('inside-pointer').after.focus,'Components');assert.equal(h('escape').after.focus,'Components');assert.equal(h('arrow-down').after.focus,'Components');
  assert.deepEqual(active(h('switch-root').after),['Layout']);assert.equal(h('switch-root').after.focus,'Layout');assert.equal(h('toggle-root').after.focus,'Components');
  assert(h('live-label').after.surfaces[0].links[0].text.startsWith('Updated original label'));assert.equal(h('live-description').after.surfaces[0].links[0].parts[1].text,'Updated original description.');assert.deepEqual(active(h('replace-trigger').after),['Renamed']);assert.equal(h('replace-trigger').after.focus,'BODY');
  assert.equal(h('prepend-index').after.surfaces.length,0);assert.equal(h('remove-active-index').after.surfaces.length,0);assert.equal(h('tab-enter-child').tab.focus,'ButtonsReusable fictional buttons.');assert.deepEqual(h('tab-enter-child').after.events,[anchor('#fictional-buttons')]);
  assert.equal(h('custom-node-retained').sameInputNode,true);assert.equal(h('custom-node-retained').after.surfaces[0].links[0].input[0].value,'caller retained custom text');
  assert.deepEqual(r.presence.find(p=>p.scheme===scheme),{scheme,at50:{present:true,sameNode:true,visibleClass:false,expanded:'false'},at180:{present:false,sameNode:false,visibleClass:false,expanded:'false'}});
 }
}

export function verifyNavigationMenuPaint(state,png){
 let probes=0;
 for(const surface of state.measurement.surfaces){const x=Math.floor(surface.box.x+8),y=Math.floor(surface.box.y+8);assert(x>=0&&x<png.width&&y>=0&&y<png.height);const at=(y*png.width+x)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],state.scheme==='Light'?[255,255,255,255]:[24,37,51,255],state.id+' surface');probes++;}
 return probes;
}
export async function verifyNavigationMenuSource(referenceFile,frames,recapture){
 const bytes=await fs.readFile(referenceFile),r=JSON.parse(bytes);verifyNavigationMenuModel(r);assert.equal(hash(await fs.readFile(new URL('./capture-navigation-menu.cjs',import.meta.url))),r.scriptSha256);assert.equal(hash(await fs.readFile(new URL('./navigation-menu-client.tsx',import.meta.url))),r.clientSha256);
 if(recapture)assert.deepEqual(JSON.parse(await fs.readFile(path.join(recapture,'NavigationMenu.json'))),r);
 for(const input of r.dependencies){assert(!input.path.includes('..')&&!path.isAbsolute(input.path));const bytes=await fs.readFile(path.join(frames,'Source',input.path));assert.equal(hash(bytes),input.sha256,input.path);if(recapture)assert.deepEqual(await fs.readFile(path.join(recapture,'Source',input.path)),bytes);}
 let pixelsValidated=0,paletteProbes=0;
 for(const state of r.cases){const bytes=await fs.readFile(path.join(frames,state.file));assert.equal(hash(bytes),state.pngSha256);const png=readRgbaPng(bytes);assert.equal(png.width,1000);assert.equal(png.height,760);pixelsValidated+=png.width*png.height;paletteProbes+=verifyNavigationMenuPaint(state,png);if(recapture)assert.deepEqual(await fs.readFile(path.join(recapture,state.file)),bytes,state.file);}
 return{format:'orchard-navigation-menu-source-audit-v1',passed:true,sourceStates:r.cases.length,retainedInstanceHistories:r.histories.length,presenceHistories:r.presence.length,pinnedSourceFiles:r.dependencies.length,pngs:r.cases.length,pixelsValidated,paletteProbes,independentRecaptureByteIdentical:!!recapture,sourceReferenceSha256:hash(bytes),nativeImplemented:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const i=process.argv.indexOf(name);return i<0?null:process.argv[i+1];};const reference=option('--reference'),frames=option('--frames'),output=option('--output');assert(reference&&frames&&output);const result=await verifyNavigationMenuSource(reference,frames,option('--recapture'));await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));}
