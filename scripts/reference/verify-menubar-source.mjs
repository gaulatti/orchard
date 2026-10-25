import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const pinnedInputs=[
  ['src/components/menubar.tsx','372c40d54f5cf711426d2042b3a1f00d71221f157ab16643d8f42f8883cc2122'],
  ['src/utils/cn.ts','74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883'],
  ['src/utils/hooks.ts','ca3d38ac220ddadaa1ff1a22eb6171992b17b50bae755b1186253fbcade92aed'],
  ['src/styles/global.css','fa7278f1107d946977c474002a09047be1e83858631f93b056435a3bbf038d96'],
  ['src/styles/tokens.generated.css','74468d6dd3516687e3f8b96aef47e10bdcce206e31edc912abe985c45b83bf3e'],
  ['package-lock.json','6ce0a460b61d1087b9d51ed49f949ba264005ba8bfbe0fc26260e7c8bf16f437']
];
const caseIds=['closed','empty-bar','basic','rich-action','checked-beta','empty-menu','edit-menu','submenu','deep-submenu','disabled-submenu','unicode','long-label','narrow','rtl','bottom-right','font200'];
const historyIds=['checkbox-refused','radio-refused','checkbox-accepted','radio-accepted','action','disabled-action','disabled-radio','toggle-trigger','switch-trigger','hover-other-trigger','outside-dismiss','inside-bar-pointer','escape','arrow-down','shortcut-display-only','submenu-click','submenu-action','live-selection','replace-items','replace-trigger','remove-active-index','active-index-now-edit','prepend-index','callback-throws','tab-and-enter'];
const active=measurement=>measurement.triggers.filter(t=>t.expanded==='true').map(t=>t.text);
const checked=measurement=>measurement.surfaces[0].rows.filter(row=>row.checked!==null).map(row=>[row.text,row.checked]);
const initialChecked=[['Show fictional guidesCtrl+G','false'],['Fictional Alpha mode','true'],['Fictional Beta modeAlt+B','false'],['Unavailable mode','false']];

export function verifyMenubarModel(reference) {
  assert.equal(reference.format,'orchard-menubar-reference-v1');
  assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');
  assert.deepEqual(reference.dependencies.map(input=>[input.path,input.sha256]),pinnedInputs);
  assert.equal(reference.modules.find(m=>m.name==='react').version,'19.2.4');
  assert.deepEqual(reference.fonts.map(f=>f.weight),[400,500,600]);
  for(const input of [...reference.dependencies,...reference.fonts,...reference.bundledFiles,...reference.modules.map(m=>({sha256:m.packageSha256}))]) assert.match(input.sha256,/^[a-f0-9]{64}$/);
  assert.deepEqual(reference.viewport,{width:900,height:760});
  assert.equal(reference.reducedMotion,'reduce');
  assert.equal(reference.networkRequests,0);
  assert.equal(reference.nativeImplemented,false);
  assert.equal(reference.nativeComponentAccepted,false);
  assert.equal(reference.cases.length,caseIds.length*2);
  assert.equal(reference.histories.length,historyIds.length*2);
  assert.equal(reference.presence.length,2);
  for(const scheme of ['Light','Dark']) {
    const cases=reference.cases.filter(c=>c.scheme===scheme),histories=reference.histories.filter(h=>h.scheme===scheme);
    assert.deepEqual(cases.map(c=>c.id),caseIds);
    assert.deepEqual(histories.map(h=>h.id),historyIds);
    const c=id=>cases.find(c=>c.id===id).measurement;
    const h=id=>histories.find(h=>h.id===id);
    for(const row of cases) {
      assert.equal(path.basename(row.file),row.file);
      assert.match(row.pngSha256,/^[a-f0-9]{64}$/);
      assert(row.file.endsWith('.png'));
      for(const surface of row.measurement.surfaces) {
        for(const value of Object.values(surface.box)) assert(Number.isFinite(value));
        assert(surface.box.width>=192 && surface.box.height>0);
        assert.equal(surface.visibleClass,true);
        assert.equal(surface.paint.opacity,1);
        assert.equal(surface.paint.radius,'7px');
        assert.deepEqual(surface.paint.padding,row.id==='font200'?[12,12,12,12]:[6,6,6,6]);
        assert.equal(surface.paint.background,scheme==='Light'?'rgb(255, 255, 255)':'rgb(24, 37, 51)');
        for(const item of surface.rows.filter(i=>i.role!=='separator')) {
          assert.equal(item.paint.fontSize,row.id==='font200'?'28px':'14px');
          assert.equal(item.paint.lineHeight,row.id==='font200'?'40px':'20px');
          assert.equal(item.paint.opacity,item.disabled ? 0.5 : 1);
          assert.equal(item.tabIndex,0);
        }
      }
    }
    assert.deepEqual(c('closed').bar.box,{x:20,y:20,width:360,height:42});
    assert.deepEqual(c('closed').bar.paint.padding,[4,4,4,4]);
    assert.equal(c('closed').bar.paint.gap,'2px');
    assert.equal(c('closed').bar.paint.radius,'7px');
    assert.deepEqual(active(c('closed')),[]);
    assert.equal(c('closed').surfaces.length,0);
    assert.equal(c('empty-bar').bar.box.height,10);
    assert.equal(c('empty-bar').triggers.length,0);
    const basic=c('basic').surfaces[0];
    assert.deepEqual(basic.box,{x:25,y:61,width:224,height:316});
    assert.equal(basic.role,'menu');
    assert.deepEqual(basic.rows.map(row=>row.box.height),[32,32,1,52,32,32,32,1,32,32]);
    assert.deepEqual(basic.rows.map(row=>row.role),['menuitem','menuitem','separator','menuitemcheckbox','menuitemradio','menuitemradio','menuitemradio','separator','menuitem','menuitem']);
    assert.deepEqual(checked(c('basic')),initialChecked);
    assert.equal(c('checked-beta').surfaces[0].rows[3].checked,'true');
    assert.equal(c('checked-beta').surfaces[0].rows[5].checked,'true');
    assert.equal(c('basic').triggers[0].paint.background,scheme==='Light'?'rgb(44, 87, 132)':'rgb(91, 163, 245)');
    assert.equal(c('basic').triggers[0].paint.color,'rgb(255, 255, 255)');
    assert.equal(c('basic').triggers[0].paint.fontWeight,'500');
    assert.equal(c('rich-action').surfaces[0].rows[0].icons.length,1);
    assert.equal(c('rich-action').surfaces[0].rows[0].icons[0].box.width,16);
    assert.equal(c('empty-menu').surfaces[0].box.height,22);
    assert.equal(c('empty-menu').surfaces[0].rows.length,0);
    assert.equal(c('edit-menu').surfaces[0].box.height,54);
    assert.deepEqual(active(c('edit-menu')),['Edit']);
    assert.equal(c('submenu').surfaces.length,2);
    assert.deepEqual(c('submenu').surfaces[1].box,{x:246,y:302,width:192,height:126});
    assert.equal(c('submenu').surfaces[1].role,null);
    assert.equal(c('deep-submenu').surfaces.length,3);
    assert.deepEqual(c('deep-submenu').surfaces[2].box,{x:435,y:365,width:192,height:54});
    assert.equal(c('disabled-submenu').surfaces.length,2);
    assert.equal(c('disabled-submenu').surfaces[0].rows[9].disabled,true);
    assert.equal(c('disabled-submenu').surfaces[0].rows[9].expanded,'true');
    assert.equal(c('unicode').surfaces[0].box.height,336);
    assert.equal(c('long-label').surfaces[0].box.height,376);
    assert.equal(c('narrow').surfaces[0].box.width,224);
    assert.equal(c('narrow').bar.box.width,140);
    assert.equal(c('rtl').surfaces[0].box.x,328.671875);
    assert(c('bottom-right').surfaces[0].box.y+c('bottom-right').surfaces[0].box.height>760);
    assert.equal(c('font200').bar.box.height,82);
    assert.equal(c('font200').surfaces[0].box.width,448);
    assert.equal(c('font200').surfaces[0].box.height,628);
    for(const entry of histories) assert.equal(entry.retainedReactInstance,true);
    for(const accept of [false,true]) for(const action of ['checkbox','radio']) {
      const entry=h(`${action}-${accept?'accepted':'refused'}`);
      assert.deepEqual(entry.after.events,[action==='checkbox'?{key:'checked',value:true}:{key:'radio',value:'b'}]);
      assert.equal(entry.after.surfaces.length,0);
      assert.deepEqual(active(entry.after),[]);
      assert.equal(entry.after.focus,'BODY');
      const expected=structuredClone(initialChecked);
      if(accept&&action==='checkbox') expected[0][1]='true';
      if(accept&&action==='radio'){expected[1][1]='false';expected[2][1]='true';}
      assert.deepEqual(checked(entry.reopened),expected);
      assert.deepEqual(entry.reopened.events,entry.after.events);
    }
    for(const id of ['action','submenu-action']) {
      assert.deepEqual(h(id).after.events,[id==='action'?'new':'nested']);
      assert.equal(h(id).after.surfaces.length,0);
      assert.equal(h(id).after.focus,'BODY');
    }
    for(const id of ['disabled-action','disabled-radio','hover-other-trigger','inside-bar-pointer','escape','arrow-down','shortcut-display-only','submenu-click']) {
      assert.deepEqual(active(h(id).after),['File']);
      assert.equal(h(id).after.surfaces.length,1);
      assert.deepEqual(h(id).after.events,[]);
      assert.equal(h(id).after.focus,'File');
    }
    assert.equal(h('toggle-trigger').after.surfaces.length,0);
    assert.equal(h('toggle-trigger').after.focus,'File');
    assert.deepEqual(active(h('switch-trigger').after),['Edit']);
    assert.equal(h('switch-trigger').after.focus,'Edit');
    assert.equal(h('outside-dismiss').after.surfaces.length,0);
    assert.equal(h('outside-dismiss').after.focus,'outside-after');
    assert.deepEqual(checked(h('live-selection').after),[['Show fictional guidesCtrl+G','true'],['Fictional Alpha mode','false'],['Fictional Beta modeAlt+B','true'],['Unavailable mode','false']]);
    assert.equal(h('replace-items').after.surfaces[0].rows[0].text,'Updated original labelCtrl+N');
    assert.deepEqual(active(h('replace-trigger').after),['Renamed']);
    assert.equal(h('remove-active-index').after.surfaces.length,0);
    assert.deepEqual(active(h('active-index-now-edit').after),['Edit']);
    assert.deepEqual(active(h('prepend-index').after),['Before']);
    assert.deepEqual(active(h('callback-throws').after),['File']);
    assert.deepEqual(h('callback-throws').after.events,['new']);
    assert.equal(h('tab-and-enter').after.focus,'New fictional draftCtrl+N');
    assert.deepEqual(h('tab-and-enter').selected.events,['new']);
    assert.equal(h('tab-and-enter').selected.surfaces.length,0);
    assert.deepEqual(reference.presence.find(p=>p.scheme===scheme),{scheme,at50:{present:true,sameNode:true,visibleClass:false,expanded:'false'},at180:{present:false,sameNode:false,visibleClass:false,expanded:'false'}});
  }
}

export function verifyMenubarPaint(row,png) {
  let probes=0;
  const pixel=(x,y,expected,label)=>{
    assert(x>=0&&x<png.width&&y>=0&&y<png.height,label+' outside capture');
    const offset=(Math.floor(y)*png.width+Math.floor(x))*4;
    assert.deepEqual([...png.rgba.subarray(offset,offset+4)],expected,row.file+' '+label);
    probes++;
  };
  // Narrow/RTL/edge menus cast their overlay shadow across this bar sample.
  // Keep the direct surface-color assertion to unobscured fixture positions.
  if(!['empty-bar','narrow','rtl','bottom-right'].includes(row.id)) {
    const bar=row.measurement.bar.box;
    pixel(bar.x+bar.width-3,bar.y+bar.height/2,row.scheme==='Light'?[255,255,255,255]:[24,37,51,255],'bar surface');
  }
  for(const trigger of row.measurement.triggers.filter(t=>t.expanded==='true'))
    pixel(trigger.box.x+10,trigger.box.y+3,row.scheme==='Light'?[44,87,132,255]:[91,163,245,255],'active trigger');
  return probes;
}

export async function verifyMenubarSource(referenceFile,frames,recapture) {
  const bytes=await fs.readFile(referenceFile),reference=JSON.parse(bytes);
  verifyMenubarModel(reference);
  assert.equal(hash(await fs.readFile(new URL('./capture-menubar.cjs',import.meta.url))),reference.scriptSha256);
  assert.equal(hash(await fs.readFile(new URL('./menubar-client.tsx',import.meta.url))),reference.clientSha256);
  if(recapture) assert.deepEqual(JSON.parse(await fs.readFile(path.join(recapture,'Menubar.json'))),reference);
  for(const input of reference.dependencies) {
    assert(!input.path.includes('..')&&!path.isAbsolute(input.path));
    const bytes=await fs.readFile(path.join(frames,'Source',input.path));
    assert.equal(hash(bytes),input.sha256,input.path);
    if(recapture) assert.deepEqual(await fs.readFile(path.join(recapture,'Source',input.path)),bytes,input.path);
  }
  let pixelsValidated=0,paletteProbes=0;
  for(const row of reference.cases) {
    const bytes=await fs.readFile(path.join(frames,row.file));
    assert.equal(hash(bytes),row.pngSha256,row.file);
    const png=readRgbaPng(bytes);
    assert.equal(png.width,900);assert.equal(png.height,760);
    paletteProbes+=verifyMenubarPaint(row,png);
    pixelsValidated+=png.width*png.height;
    if(recapture) assert.deepEqual(await fs.readFile(path.join(recapture,row.file)),bytes,row.file);
  }
  return{format:'orchard-menubar-source-audit-v1',passed:true,sourceStates:reference.cases.length,retainedInstanceHistories:reference.histories.length,presenceHistories:reference.presence.length,pngs:reference.cases.length,pinnedSourceFiles:reference.dependencies.length,pixelsValidated,paletteProbes,independentRecaptureByteIdentical:!!recapture,sourceReferenceSha256:hash(bytes),nativeImplemented:false,nativeComponentAccepted:false};
}

if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href) {
  const option=name=>{const i=process.argv.indexOf(name);return i<0?null:process.argv[i+1];};
  const reference=option('--reference'),frames=option('--frames'),output=option('--output');
  assert(reference&&frames&&output,'Use --reference, --frames and --output, optionally --recapture.');
  const result=await verifyMenubarSource(reference,frames,option('--recapture'));
  await fs.mkdir(path.dirname(path.resolve(output)),{recursive:true});
  await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result));
}
