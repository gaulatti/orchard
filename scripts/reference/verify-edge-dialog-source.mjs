import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const pin = 'a86059013cd59684ce452849278ea81e734f2066';
const sides = ['left', 'right', 'top', 'bottom'];
const schemes = ['Light', 'Dark'];
const premium = 'cubic-bezier(0.22, 1, 0.36, 1)';
const exit = 'cubic-bezier(0.4, 0, 1, 1)';
const dependencies = {
 'src/components/drawer.tsx': '9f86c9ea5106c4a34bc64e7d28876afb6133d7e5aa59ab74e6f954965fe9683b',
 'src/components/sheet.tsx': '2ee3b6f1d2f315dab3055e7a1332cdc771accad32827763617b4c06669975bca',
 'src/components/modal.tsx': 'c15e1fbc28838ce7a665a4b6cdbcf5fc9190160dad791c178f02eacce92763f8',
 'src/components/dialog-close-button.tsx': 'a1614f004198702d1325af4dac2e2ab18fe6cf227574df2b1caf22a2e341c7e8',
 'src/utils/cn.ts': '74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883',
 'src/utils/hooks.ts': 'ca3d38ac220ddadaa1ff1a22eb6171992b17b50bae755b1186253fbcade92aed',
 'src/styles/global.css': 'fa7278f1107d946977c474002a09047be1e83858631f93b056435a3bbf038d96',
 'src/styles/tokens.generated.css': '74468d6dd3516687e3f8b96aef47e10bdcce206e31edc912abe985c45b83bf3e',
 'package-lock.json': '6ce0a460b61d1087b9d51ed49f949ba264005ba8bfbe0fc26260e7c8bf16f437'
};
const key = r => JSON.stringify([r.scheme, r.viewport, r.spec]);
const unique = (rows, getKey) => assert.equal(new Set(rows.map(getKey)).size, rows.length);
const single = rows => { assert.equal(rows.length, 1); return rows[0]; };
const close = (a, b, tolerance = .0002) => assert(Math.abs(a-b) <= tolerance, `${a} differs from ${b}`);

// Checks capture coverage and the source's observable contracts. Native rendering
// and input delivery are separate acceptance boundaries.
export function verifyEdgeDialogSource(reference, motion, presence) {
 for (const [r, format] of [[reference, 'orchard-edge-dialog-reference-v1'], [motion, 'orchard-edge-dialog-motion-v1'], [presence, 'orchard-edge-dialog-presence-v1']]) {
  assert.equal(r.format, format); assert.equal(r.commit, pin);
  assert.equal(r.dependencies.length, 9);
  unique(r.dependencies, d => d.path);
  assert.deepEqual(Object.fromEntries(r.dependencies.map(d => [d.path, d.sha256])), dependencies);
  assert.deepEqual(r.modules.map(m => [m.name, m.version]), [['react','19.2.4'], ['react-dom','19.2.4'], ['lucide-react','0.563.0']]);
  assert.deepEqual(r.fonts.map(f => [f.family, f.weight]), [['Encode Sans',400], ['Encode Sans',500], ['Encode Sans',600], ['Libre Franklin',400], ['Libre Franklin',600]]);
  assert.equal(r.bundledFiles.length, 1696); unique(r.bundledFiles, f => f.path);
  for (const sha of [r.scriptSha256, r.clientSha256, r.bundleSha256, r.cssSha256, ...r.fonts.map(f=>f.sha256), ...r.bundledFiles.map(f=>f.sha256)]) assert.match(sha, /^[a-f0-9]{64}$/);
 }
 assert.deepEqual(reference.fonts, motion.fonts); assert.deepEqual(reference.fonts, presence.fonts);
 assert.deepEqual(reference.bundledFiles, motion.bundledFiles); assert.deepEqual(reference.bundledFiles, presence.bundledFiles);
 assert.deepEqual(reference.modules, motion.modules); assert.deepEqual(reference.modules, presence.modules);
 assert.equal(reference.clientSha256, motion.clientSha256); assert.equal(reference.clientSha256, presence.clientSha256);
 assert.equal(reference.bundleSha256, motion.bundleSha256); assert.equal(reference.bundleSha256, presence.bundleSha256);
 assert.equal(reference.cases.length, 194); unique(reference.cases, key); unique(reference.cases, r=>r.file);
 const expected = [];
 for (const scheme of schemes) for (const viewport of [{width:420,height:500},{width:767,height:600},{width:768,height:600},{width:900,height:760}]) {
  const add = spec => { expected.push(key({scheme,viewport,spec})); if(spec.shape==='custom')expected.push(key({scheme,viewport,spec:{...spec,height:1000}})); };
  for(const shape of ['fixed','long','empty','no-heading','description-only','title-wrap','custom']) add({kind:'drawer',shape});
  if(viewport.width===900)add({kind:'drawer',shape:'snap'});
  for(const side of sides) {
   for(const shape of ['fixed','long'])add({kind:'sheet',side,shape});
   if(viewport.width===900)for(const shape of ['empty','no-heading','description-only','title-wrap','no-scroll','fill','custom'])add({kind:'sheet',side,shape});
  }
 }
 assert.deepEqual(reference.cases.map(key).sort(), expected.sort());
 for(const row of reference.cases) {
  const {spec:s,measurement:m,viewport:v}=row, drawer=s.kind==='drawer', vertical=['left','right'].includes(s.side), b=m.dialog.box;
  assert.match(row.file,/^edge-dialog-\d+-(Light|Dark)\.png$/); assert.match(row.pngSha256,/^[a-f0-9]{64}$/);
  assert.deepEqual(m.events,[]); assert.equal(m.editor,['empty','fill'].includes(s.shape)?null:'Fictional original value');
  assert.equal(m.labelled,!['no-heading','description-only'].includes(s.shape)); assert.equal(m.described,s.shape!=='no-heading');
  assert.equal(m.name,m.labelled?(s.shape==='title-wrap'?'Fictional component heading with deliberately long wrapping words across several lines':'Fictional component panel'):'Dialog');
  assert.equal(Boolean(m.title),m.labelled); assert.equal(Boolean(m.description),m.described);
  assert.equal(m.dialog.paint.background,row.scheme==='Light'?'rgb(255, 255, 255)':'rgb(24, 37, 51)');
  assert.deepEqual(m.dialog.paint.border,drawer?[1,0,0,0]:[1,1,1,1]);
  assert.deepEqual(m.dialog.paint.radii,s.shape==='custom'?[0,0,0,0]:drawer||s.side==='bottom'?[14,14,0,0]:s.side==='top'?[0,0,14,14]:[0,0,0,0]);
  if(m.title){assert(m.title.paint.fontFamily.startsWith('"Encode Sans"'));assert.equal(m.title.paint.fontSize,drawer?'18px':'24px');assert.equal(m.title.paint.lineHeight,drawer?'28px':'32px');assert.equal(m.title.paint.fontWeight,'500');}
  if(m.description){assert(m.description.paint.fontFamily.startsWith('"Libre Franklin"'));assert.equal(m.description.paint.fontSize,'14px');assert.equal(m.description.paint.lineHeight,'22.75px');assert.equal(m.description.paint.fontWeight,'400');}
  assert.equal(Boolean(m.handle),drawer);assert.equal(Boolean(m.close),!drawer);assert.equal(Boolean(m.header),!drawer||s.shape!=='no-heading');
  if(drawer){assert.equal(b.x,0);assert.equal(b.width,v.width);assert.equal(b.y+b.height,v.height);assert(b.height<=v.height*(s.shape==='custom'?.7:.9));assert.equal(m.handle.box.height,40);assert.deepEqual(m.body.paint.padding,[24,0,24,24]);}
  else {assert.equal(b.width,Math.min(v.width,s.shape==='custom'?512:vertical?384:Infinity));assert.equal(b.x,s.side==='right'?v.width-b.width:0);assert.equal(b.y,s.side==='bottom'?v.height-b.height:0);if(vertical)assert.equal(b.height,v.height);else if(['long','no-scroll','fill'].includes(s.shape)||s.height===1000)assert(b.height>v.height);assert.equal(m.close.box.height,36);assert.deepEqual(m.inner.paint.padding,Array(4).fill(v.width>=768?32:24));}
  assert.equal(m.body.paint.overflow,['no-scroll','fill'].includes(s.shape)?'hidden':'auto');
 }
 for(const scheme of schemes) {
  const rows=reference.cases.filter(r=>r.scheme===scheme&&r.viewport.width===900&&r.spec.kind==='drawer');
  assert.deepEqual(single(rows.filter(r=>r.spec.shape==='snap')).measurement,single(rows.filter(r=>r.spec.shape==='fixed')).measurement);
 }
 assert.equal(reference.histories.length,20);unique(reference.histories,r=>JSON.stringify([r.scheme,r.kind,r.accept,r.action]));
 for(const scheme of schemes)for(const kind of ['drawer','sheet'])for(const accept of [false,true])for(const action of kind==='drawer'?['escape','backdrop']:['close','escape','backdrop']) {
  const h=single(reference.histories.filter(r=>r.scheme===scheme&&r.kind===kind&&r.accept===accept&&r.action===action));
  assert.deepEqual(h.before.events,[]);assert.equal(h.before.editor,'Retained fictional edge edit');assert.deepEqual(h.after.events,['close']);assert.equal(Boolean(h.after.dialog),!accept);if(!accept){assert.equal(h.after.editor,h.before.editor);assert.deepEqual(h.after.dialog,h.before.dialog);}
 }
 assert.equal(reference.gestures.length,10);unique(reference.gestures,r=>JSON.stringify([r.scheme,r.delta]));
 for(const scheme of schemes)for(const delta of [-80,0,60,60.5,100]){const g=single(reference.gestures.filter(r=>r.scheme===scheme&&r.delta===delta));assert.deepEqual(g.after.events,delta>60?['close']:[]);assert(g.after.dialog);assert.equal(g.after.editor,'Fictional original value');}
 assert.equal(motion.timelines.length,10);unique(motion.timelines,r=>JSON.stringify([r.scheme,r.spec]));
 let samples=0;
 for(const scheme of schemes)for(const side of ['drawer',...sides]) {
  const t=single(motion.timelines.filter(r=>r.scheme===scheme&&(side==='drawer'?r.spec.kind==='drawer':r.spec.kind==='sheet'&&r.spec.side===side)));
  const edge=side==='drawer'?'bottom':side, vertical=['left','right'].includes(edge), sign=['left','top'].includes(edge)?-1:1;
  for(const [direction,duration,times]of [['opening',240,[0,60,120,180,240]],['closing',180,[0,45,90,135,180]]]) {
   const d=t[direction],opening=direction==='opening';assert.equal(d.panel.property,'translate');assert.equal(d.backdrop.property,'opacity');assert.equal(d.panel.duration,duration);assert.equal(d.backdrop.duration,duration);assert.equal(d.panel.easing,opening?premium:exit);assert.equal(d.backdrop.easing,opening||side==='drawer'?premium:exit);assert.deepEqual(d.points.map(p=>p.ms),times);
   for(const [index,p]of d.points.entries()) {
    const distance=p.box[vertical?'width':'height'], panel=bezier(index/4,opening? .22:.4,opening?.36:1,opening?1:0),backdrop=bezier(index/4,opening||side==='drawer'?.22:.4,opening||side==='drawer'?.36:1,opening||side==='drawer'?1:0);
    const offset=(opening?1-panel:panel)*distance*sign;
    close(p.translateX,vertical?offset:0);close(p.translateY,vertical?0:offset);close(p.backdropOpacity,opening?backdrop:1-backdrop,.000002);samples++;
   }
  }
 }
 assert.equal(samples,100);assert.equal(presence.traces.length,8);unique(presence.traces,r=>JSON.stringify([r.scheme,r.kind,r.reduced]));
 let presenceSamples=0;
 const expectedSteps=[['enter',0,0,true,false,false,false],['enter',19,19,true,false,false,false],['enter',20,20,true,true,false,false],['exit',0,20,true,false,true,true],['exit',179,199,true,false,true,true],['exit',180,200,false,false,false,false],['reopen',0,295,true,false,false,false],['reopen',19,314,true,false,false,false],['reopen',20,315,true,true,false,false],['reopen',200,495,true,true,false,false],['early-exit',0,680,true,false,true,true],['early-exit',179,859,true,false,true,true],['early-exit',180,860,false,false,false,false]];
 for(const scheme of schemes)for(const kind of ['drawer','sheet'])for(const reduced of [false,true]) {
  const t=single(presence.traces.filter(r=>r.scheme===scheme&&r.kind===kind&&r.reduced===reduced));assert.equal(t.driver,'isolated-browser-realm-clock');
  assert.deepEqual(t.steps.filter(s=>s.action==='set').map(s=>[s.time,s.open]),[[0,true],[20,false],[200,true],[220,false],[295,true],[495,false],[675,true],[680,false]]);
  const rows=t.steps.filter(s=>s.action==='sample');assert.deepEqual(rows.map(s=>[s.phase,s.age,s.time,s.present,s.visible,s.hidden,s.inert]),expectedSteps);presenceSamples+=rows.length;
 }
 return {sourceStates:194,drawerStates:66,sheetStates:128,controlledHistories:20,touchHistories:10,motionTimelines:10,motionSamples:samples,presenceTraces:8,presenceSamples,nativeControlsImplemented:false,nativeComponentAccepted:false};
}

function bezier(progress,x1,x2,y1) {
 if(progress<=0)return 0;if(progress>=1)return 1;
 let low=0,high=1;for(let i=0;i<40;i++){const t=(low+high)/2,x=3*(1-t)**2*t*x1+3*(1-t)*t*t*x2+t**3;if(x<progress)low=t;else high=t;}
 const t=(low+high)/2;return 3*(1-t)**2*t*y1+3*(1-t)*t*t+t**3;
}

async function main() {
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1],`Missing ${name}`);return process.argv[i+1];};
 const files=[option('--source'),option('--motion'),option('--presence')],reports=await Promise.all(files.map(async f=>JSON.parse(await fs.readFile(f))));
 const counts=verifyEdgeDialogSource(...reports);let pixels=0,identical=0;const r=reports[0];
 for(const row of r.cases){const bytes=await fs.readFile(path.join(path.dirname(files[0]),'EdgeDialog',row.file));assert.equal(hash(bytes),row.pngSha256);const p=readRgbaPng(bytes);assert.equal(p.width,row.viewport.width);assert.equal(p.height,row.viewport.height);pixels+=p.width*p.height;}
 for(const [index,script]of ['capture-edge-dialog.cjs','capture-edge-dialog-motion.cjs','capture-edge-dialog-presence.cjs'].entries()){assert.equal(hash(await fs.readFile('scripts/reference/'+script)),reports[index].scriptSha256);}
 assert.equal(hash(await fs.readFile('scripts/reference/edge-dialog-client.tsx')),r.clientSha256);
 for(const font of r.fonts)assert.equal(hash(await fs.readFile('src/Orchard/Assets/Fonts/'+font.name)),font.sha256);
 const frozen=process.argv.includes('--frozen-only'),packages=new Map();
 if(!frozen) {
  const entry=r.bundledFiles.find(f=>f.path.includes('/node_modules/')).path,root=entry.slice(0,entry.indexOf('/node_modules/')),lock=JSON.parse(await fs.readFile(path.join(root,'package-lock.json')));
  for(const d of r.dependencies)assert.equal(hash(await fs.readFile(path.join(root,d.path))),d.sha256);
  for(const record of r.bundledFiles){assert.equal(hash(await fs.readFile(record.path)),record.sha256);if(!record.path.startsWith(root+'/node_modules/'))continue;let dir=path.dirname(record.path),found=false;while(dir.startsWith(root)){const key=path.relative(root,dir).replaceAll('\\','/');if(packages.has(key)){found=true;break;}try{const bytes=await fs.readFile(path.join(dir,'package.json')),pkg=JSON.parse(bytes);assert.equal(pkg.version,lock.packages[key]?.version,key);packages.set(key,{name:pkg.name,version:pkg.version,sha256:hash(bytes)});found=true;break;}catch(e){if(e.code!=='ENOENT')throw e;}dir=path.dirname(dir);}assert(found,record.path);}
  for(const [i,argument]of ['--independent','--motion-independent','--presence-independent'].entries()){const independent=option(argument);assert.deepEqual(await fs.readFile(files[i]),await fs.readFile(path.join(independent,path.basename(files[i]))));identical++;if(i===0)for(const row of r.cases){assert.deepEqual(await fs.readFile(path.join(path.dirname(files[0]),'EdgeDialog',row.file)),await fs.readFile(path.join(independent,row.file)));identical++;}}
 }
 const audit={passed:true,...counts,pngs:194,pixelsValidated:pixels,independentFilesByteIdentical:identical,originalInputsRehashed:!frozen,bundledInputs:r.bundledFiles.length,actualPackages:packages.size,packages:Object.fromEntries(packages),sources:await Promise.all(files.map(async p=>({path:p,sha256:hash(await fs.readFile(p))})))};
 await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,packages:undefined,sources:undefined}));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
