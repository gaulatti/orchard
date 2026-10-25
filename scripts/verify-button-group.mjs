import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyButtonGroupModel(r){
 assert(['orchard-button-group-native-v1','orchard-button-group-native-v2'].includes(r.format));const twoWay=r.format==='orchard-button-group-native-v2';assert.equal(r.passed,true);assert.deepEqual(r.failures,[]);
 for(const key of ['hostVisible','hardwareInputSent','activateCalled','focusCalled','enabledButtonInvoked','nativeArm64RuntimeVerified','actualDisplayOrTextScalingVerified','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(r[key],false,key);
 assert.equal(r.activations,0);assert.equal(r.focus,0);assert(r.guards>=200);assert.equal(r.processArchitecture,r.osArchitecture);assert(['X64','Arm64'].includes(r.processArchitecture));
 assert.equal(r.sourceSha256,'f4a21c77bf545ddc0096dc7e7dc5d6fcdb892b381d651668501cbbab412d5306');
 for(const key of ['librarySha256','hostSha256'])assert.match(r[key],/^[a-f0-9]{64}$/);
 assert.equal(typeof r.catalogFixtureVerified,'boolean');
 const themes=['Light','Dark'],checks=['empty','order-visibility-orientation','local-removal','binding-removal-and-unload','style-removal','alignment-restoration','long-labels','invalid-child','invalid-orientation'];
 if(twoWay)checks.push('two-way-binding-restoration');
 if(r.catalogFixtureVerified)checks.push('catalog-fixture');
 assert.deepEqual(r.checks.map(c=>c.id).sort(),themes.flatMap(t=>checks.map(c=>t+'/'+c)).sort());for(const c of r.checks)assert.equal(c.passed,true,c.id);
 const phases=['empty','horizontal','reorder','collapsed-middle','all-collapsed','vertical','horizontal-restored','local-removed','binding-cleared','long-LeftToRight','long-RightToLeft','long-vertical'];if(r.catalogFixtureVerified)phases.push('catalog-horizontal','catalog-vertical');
 assert.deepEqual(r.frames.map(f=>f.id).sort(),themes.flatMap(t=>phases.map(p=>t+'/'+p)).sort());
 for(const f of r.frames){
  for(const k of ['groupWidth','groupHeight'])assert(Number.isFinite(f[k])&&f[k]>=0,f.id+'/'+k);
  assert(['Horizontal','Vertical'].includes(f.orientation));assert(['LeftToRight','RightToLeft'].includes(f.direction));
  const visible=f.children.filter(c=>c.visible);for(const c of f.children){if(f.id.includes('/catalog-'))assert.equal(c.id,null);else assert.equal(typeof c.id,'string');assert.equal(typeof c.enabled,'boolean');assert.equal(c.loaded,true);for(const k of ['x','y','width','height'])assert(Number.isFinite(c[k]));}
  if(/\/(empty|all-collapsed|binding-cleared)$/.test(f.id)){assert.equal(visible.length,0);assert.equal(f.groupWidth,0);assert.equal(f.groupHeight,0);}
  for(let i=0;i<visible.length;i++){const c=visible[i],first=i===0,last=i===visible.length-1;assert(c.width>0&&c.height>0);assert.deepEqual(c.margin,[0,0,0,0]);assert.deepEqual(c.radius,f.orientation==='Horizontal'?[first?7:0,last?7:0,last?7:0,first?7:0]:[first?7:0,first?7:0,last?7:0,last?7:0]);assert.deepEqual(c.border,f.orientation==='Horizontal'?[1,1,last?1:0,1]:[1,1,1,last?1:0]);}
  for(const c of visible){assert.equal(c.horizontalAlignment,'Stretch');assert.equal(c.verticalAlignment,'Stretch');for(const k of ['actionX','actionY','actionWidth','actionHeight'])assert(Number.isFinite(c[k]));for(const [a,b]of [['x','actionX'],['y','actionY'],['width','actionWidth'],['height','actionHeight']])assert(Math.abs(c[a]-c[b])<.001,f.id+'/native-action-'+a);}
  const ordered=[...visible].sort((a,b)=>f.orientation==='Horizontal'?a.x-b.x:a.y-b.y);
  for(let i=0;i<ordered.length;i++){const c=ordered[i];if(f.orientation==='Horizontal'){assert(Math.abs(c.height-f.groupHeight)<.001,f.id+'/shared-height');if(i)assert(Math.abs(c.x-ordered[i-1].x-ordered[i-1].width)<.001,f.id+'/horizontal-gap');}else{assert(Math.abs(c.width-f.groupWidth)<.001,f.id+'/shared-width');if(i)assert(Math.abs(c.y-ordered[i-1].y-ordered[i-1].height)<.001,f.id+'/vertical-gap');}}
  if(f.id.endsWith('/horizontal'))assert.deepEqual(f.children.map(c=>c.id),['first','middle','last']);
  if(f.id.endsWith('/reorder'))assert.deepEqual(f.children.map(c=>c.id),['last','first','middle']);
  if(/\/long-(LeftToRight|RightToLeft)$/.test(f.id)){assert.equal(f.groupWidth,360);assert.deepEqual(f.children.map(c=>c.id),['long-0','long-1','long-2']);}
  if(f.id.endsWith('/long-vertical')){assert.equal(f.groupWidth,260);assert.equal(f.orientation,'Vertical');}
 }
 const restore=['local-removed','bound-removed','removed-producer-update','group-unloaded','unloaded-producer-update','children-cleared','style-removed'];
 assert.deepEqual(r.restorations.map(v=>v.id).sort(),themes.flatMap(t=>restore.map(v=>t+'/'+v)).sort());
 for(const v of r.restorations){assert.equal(v.loaded,false);const stage=v.id.split('/')[1];
  if(stage==='local-removed'){assert.deepEqual(v.radius,[2,3,4,5]);assert.deepEqual(v.border,[2,3,4,5]);assert.deepEqual(v.margin,[6,7,8,9]);}
  else if(stage==='style-removed'){assert.deepEqual(v.localValuesUnset,[true,true,true]);assert.deepEqual(v.radius,[21,21,21,21]);assert.deepEqual(v.border,[4,4,4,4]);assert.deepEqual(v.margin,[5,5,5,5]);}
  else{assert.deepEqual(v.originalBindingsRestored,[true,true,true]);assert.deepEqual(v.radius,['unloaded-producer-update','children-cleared'].includes(stage)?[19,19,19,19]:[12,13,14,15]);assert.deepEqual(v.border,[4,5,6,7]);assert.deepEqual(v.margin,stage==='bound-removed'?[8,9,10,11]:[17,17,17,17]);}
 }
 const pngNames=themes.flatMap(t=>['long-LeftToRight','long-RightToLeft','long-vertical',...(r.catalogFixtureVerified?['catalog-horizontal']:[])].map(p=>t+'-'+p+'.png'));
 assert.deepEqual(r.alignmentRestorations.map(v=>v.id).sort(),themes.flatMap(t=>['local-removed','style-removed','binding-removed','binding-unloaded'].map(p=>t+'/'+p)).sort());for(const v of r.alignmentRestorations){assert.equal(v.horizontal,'Center');assert.equal(v.vertical,'Bottom');assert.equal(v.originalBindingsRestored,true);assert.equal(v.styleLocalValuesUnset,true);assert.equal(v.loaded,false);}
 assert.deepEqual(r.pngs.map(p=>p.file).sort(),pngNames.sort());for(const p of r.pngs){assert(p.width>0&&p.height>0);assert.match(p.sha256,/^[a-f0-9]{64}$/);}
 if(twoWay){assert.deepEqual(r.twoWayRestorations.map(v=>v.id).sort(),themes.flatMap(t=>['mounted','released'].map(p=>t+'/'+p)).sort());for(const v of r.twoWayRestorations){assert.deepEqual(v.border,[2,2,2,2]);assert.equal(v.horizontal,'Right');assert.equal(v.vertical,'Top');if(v.id.endsWith('/mounted')){assert.equal(v.sourcePreserved,true);assert.deepEqual(v.radius,[11,11,11,11]);assert.deepEqual(v.margin,[3,3,3,3]);}else{assert.deepEqual(v.originalBindingsRestored,[true,true,true,true,true]);assert.deepEqual(v.radius,[13,13,13,13]);assert.deepEqual(v.margin,[17,17,17,17]);}}}
 return{passed:true,checks:r.checks.length,frames:r.frames.length,appearanceRestorations:r.restorations.length,alignmentRestorations:r.alignmentRestorations.length,twoWayCallerSourceVerified:twoWay,connectedNativeActionBoundsVerified:true,pngs:r.pngs.length,catalogFixtureVerified:r.catalogFixtureVerified,guards:r.guards,librarySha256:r.librarySha256,hostSha256:r.hostSha256,actualDisplayOrTextScalingVerified:false,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export async function auditButtonGroup(folder,host){
 const bytes=await fs.readFile(path.join(folder,'button-group.json')),r=JSON.parse(bytes),audit=verifyButtonGroupModel(r);
 for(const[file,sha]of [['Orchard.dll',r.librarySha256],[host+'.dll',r.hostSha256]])assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',file))),sha);
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.observationExpired,false);
 const process=JSON.parse(await fs.readFile(path.join(folder,'process.json')));assert.equal(process.foregroundAuthorized,false);assert.equal(process.hardwareInputSent,false);
 let pixels=0,paintColumns=0;for(const p of r.pngs){const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,p.width);assert.equal(png.height,p.height);pixels+=png.width*png.height;if(p.file.endsWith('-long-LeftToRight.png')||p.file.endsWith('-catalog-horizontal.png')){const id=p.file.replace(/\.png$/,'').replace(/^(Light|Dark)-/,'$1/'),f=r.frames.find(f=>f.id===id);assert(f);const x=Math.floor(f.children[0].width/f.groupWidth*png.width/2);for(let y=0;y<png.height;y++)assert(png.rgba[(y*png.width+x)*4+3]>0,p.file+'/unpainted-action-row-'+y);paintColumns++;}}
 return{...audit,reportSha256:hash(bytes),capturePixelsVerified:pixels,nativeActionPaintColumnsVerified:paintColumns};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const[folder,host,output]=process.argv.slice(2);assert(folder&&host&&output);const r=await auditButtonGroup(folder,host);await fs.writeFile(output,JSON.stringify(r,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(r));}
