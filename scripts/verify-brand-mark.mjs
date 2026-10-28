import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import crypto from 'node:crypto';import{fileURLToPath}from'node:url';import{readRgbaPng}from'./reference/read-rgba-png.mjs';import{readGrayAlphaPng}from'./reference/read-gray-alpha-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex'),near=(a,b,label,t=.05)=>assert(Number.isFinite(a)&&Math.abs(a-b)<=t,label+`: ${a} != ${b}`);
export function verifyBrandMark(n,r,catalogExpected){
 assert.equal(n.format,'orchard-brand-mark-native-v1');assert.equal(n.passed,true);assert.equal(n.publicApisOnly,true);assert.equal(n.states.length,37);assert.equal(n.contracts.length,2);assert.equal(n.catalog.length,catalogExpected?2:0);assert.equal(n.pngs.length,37+(catalogExpected?2:0));assert(n.guards>=300);assert.equal(n.focus,0);assert.equal(n.activations,0);for(const k of['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRuntimeVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[k],false,k);assert.equal(n.assetSha256,r.asset.sha256);assert.equal(n.invalidSizesRejected,4);
 const staticKinds=['size-16','size-32','size-64','size-96','size-0','disabled','rtl','font-large','constrained'],live=['bound','bound-size','brush-color','brush-opacity','brush-replaced','caller-template','template-restored','reload'];
 const expected=[];for(const scheme of['Light','Dark'])for(const kind of[...staticKinds,...live])expected.push(scheme+'/'+kind);expected.push('Light/theme-initial','Dark/theme-dark','Light/theme-restored');assert.deepEqual(n.states.map(s=>s.scheme+'/'+s.kind).sort(),expected.sort());
 for(const s of n.states){assert.equal(s.controlType,'Image');assert.equal(s.accessibleName,'Fictional studio logo');assert.equal(s.tabStop,false);assert.equal(s.invokeAvailable,false);assert.equal(s.selectionAvailable,false);assert.equal(s.enabled,s.kind!=='disabled');assert.equal(s.sizeBindingRetained,true);assert.equal(s.brushBindingRetained,true);const size=s.kind.startsWith('size-')?Number(s.kind.slice(5)):s.kind==='bound'?48:s.kind==='reload'?32:live.includes(s.kind)?96:64;assert.equal(s.size,size);near(s.box.width,s.kind==='constrained'?32:s.kind==='caller-template'?43:size,s.kind+'/width');near(s.box.height,s.kind==='caller-template'?29:size,s.kind+'/height');
  assert.equal(s.opacity,1);
  if(live.includes(s.kind)){const first=['bound','bound-size'].includes(s.kind),changed=['brush-color','brush-opacity'].includes(s.kind),reload=s.kind==='reload';const wanted=first?{a:255,r:44,g:87,b:132}:changed?{a:255,r:150,g:60,b:180}:reload?{a:255,r:180,g:90,b:40}:{a:128,r:90,g:200,b:10};assert.deepEqual(s.foreground,wanted,'original caller color');near(s.brushOpacity,s.kind==='brush-opacity'?.5:first||changed||reload?1:.7,'original caller brush opacity',.000001);}
  if(s.kind==='caller-template'){assert.equal(s.iconBox,null);assert.equal(s.uri,null);continue;}
  assert.equal(s.imageLoaded,true);assert.equal(s.monochrome,true);assert.equal(s.uri,'ms-appx:///Orchard/Assets/Images/BleeckerLogo.png');assert.equal(s.iconWidth,1024);assert.equal(s.iconHeight,1024);near(s.iconOpacity,.9,'image opacity',.000001);near(s.iconBox.width,size,'image width');near(s.iconBox.height,size,'image height');if(!live.includes(s.kind)){const c=r.themes[s.scheme.toLowerCase()];assert.deepEqual(s.foreground,{a:255,r:parseInt(c.slice(1,3),16),g:parseInt(c.slice(3,5),16),b:parseInt(c.slice(5,7),16)});assert.equal(s.brushOpacity,1);}
 }
 for(const c of n.contracts){assert(['Light','Dark'].includes(c.scheme));for(const k of['callerBrushRetained','bindingsRetained','reloadRetainsOriginalControl','unloaded'])assert.equal(c[k],true,k);}assert.equal(new Set(n.contracts.map(c=>c.scheme)).size,2);
 for(const c of n.catalog){assert.equal(c.marks,6);assert.equal(c.defaultSize,64);assert.equal(c.registeredFixture,'Orchard.Catalog.BrandMarksFixture');assert(['Light','Dark'].includes(c.scheme));}
 assert.equal(new Set(n.pngs.map(p=>p.file)).size,n.pngs.length);assert.deepEqual(n.pngs.map(p=>p.file).sort(),[...n.states.map(s=>s.file),...n.catalog.map(c=>c.file)].sort());
 return{passed:true,nativeStates:37,actualCatalogStates:n.catalog.length,callerBindingsAndBrushesRetained:true,liveTintAndFrameMetadataVerified:true,decorativeImageSemanticsVerified:true,physicalInputVerified:false,sourceRuntimeVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export function verifyBrandMarkPaint(s,png,source){
 const b=s.kind==='caller-template'?s.box:s.iconBox,background=s.background,foreground=s.foreground,opacity=(s.kind==='caller-template'?1:.9)*s.opacity*s.brushOpacity*foreground.a/255;let ink=0,probes=0;const bounds={minX:png.width,minY:png.height,maxX:-1,maxY:-1};const expected=['r','g','b'].map(c=>Math.round(foreground[c]*opacity+background[c]*(1-opacity)));
 for(const[c,i]of[['r',0],['g',1],['b',2]])assert.equal(png.rgba[i],background[c],'reported background must match the actual frame');assert.equal(png.rgba[3],255);
 const left=Math.max(0,Math.floor(b.x)),top=Math.max(0,Math.floor(b.y)),right=Math.min(png.width,Math.ceil(b.x+b.width)),bottom=Math.min(png.height,Math.ceil(b.y+b.height));
 function sourceOpaque(x,y){if(s.kind==='caller-template')return true;const sx=(x+.5-b.x)/b.width*source.width-.5,sy=(y+.5-b.y)/b.height*source.height-.5,radius=Math.max(1,Math.ceil(source.width/b.width/2));for(let yy=Math.floor(sy-radius);yy<=Math.ceil(sy+radius);yy++)for(let xx=Math.floor(sx-radius);xx<=Math.ceil(sx+radius);xx++)if(xx<0||yy<0||xx>=source.width||yy>=source.height||source.alpha[yy*source.width+xx]!==255)return false;return true;}
 for(let y=top;y<bottom;y++)for(let x=left;x<right;x++){const at=(y*png.width+x)*4;const changed=['r','g','b'].some((c,i)=>Math.abs(png.rgba[at+i]-background[c])>2);if(changed){ink++;bounds.minX=Math.min(bounds.minX,x);bounds.minY=Math.min(bounds.minY,y);bounds.maxX=Math.max(bounds.maxX,x);bounds.maxY=Math.max(bounds.maxY,y);}const inside=x+.5>=s.box.x+1&&x+.5<s.box.x+s.box.width-1&&y+.5>=s.box.y+1&&y+.5<s.box.y+s.box.height-1;if(inside&&sourceOpaque(x,y)){for(let i=0;i<3;i++)assert(Math.abs(png.rgba[at+i]-expected[i])<=2,s.scheme+'/'+s.kind+'/actual tint pixel');assert.equal(png.rgba[at+3],255);probes++;}}
 if(s.size===0){assert.equal(ink,0);return{inkPixels:0,opaqueTintProbes:0};}assert(ink>10,s.kind+'/image has no ink');if(s.kind!=='constrained'&&s.size>=32)assert(probes>=5,s.kind+'/too few original-alpha interior probes');return{inkPixels:ink,opaqueTintProbes:probes,bounds};
}
export function verifyBrandMarkSilhouette(s,png,source){
 const box=s.iconBox,x0=Math.floor(box.x),y0=Math.floor(box.y),width=Math.ceil(box.x+box.width)-x0,height=Math.ceil(box.y+box.height)-y0;
 const expected=new Uint8Array(width*height),actual=new Uint8Array(width*height);
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){
  let covered=0;
  // Sample the original asset across each output pixel. This checks the
  // source shape without asserting a browser/WinUI antialiasing algorithm.
  for(let yy=0;yy<4;yy++)for(let xx=0;xx<4;xx++){
   const sx=Math.max(0,Math.min(source.width-1,Math.floor((x0+x+(xx+.5)/4-box.x)*source.width/box.width)));
   const sy=Math.max(0,Math.min(source.height-1,Math.floor((y0+y+(yy+.5)/4-box.y)*source.height/box.height)));
   if(source.alpha[sy*source.width+sx]>127)covered++;
  }
  const at=((y0+y)*png.width+x0+x)*4,index=y*width+x;
  expected[index]=covered>=8?1:0;
  actual[index]=Math.max(Math.abs(png.rgba[at]-s.background.r),Math.abs(png.rgba[at+1]-s.background.g),Math.abs(png.rgba[at+2]-s.background.b))>2?1:0;
 }
 const near=(mask,x,y)=>{for(let yy=Math.max(0,y-1);yy<=Math.min(height-1,y+1);yy++)for(let xx=Math.max(0,x-1);xx<=Math.min(width-1,x+1);xx++)if(mask[yy*width+xx])return true;return false;};
 let sourceInk=0,nativeInk=0,overlap=0;
 for(let y=0;y<height;y++)for(let x=0;x<width;x++){
  const at=y*width+x,e=expected[at],a=actual[at];sourceInk+=e;nativeInk+=a;overlap+=e&&a?1:0;
  if(e)assert(near(actual,x,y),s.scheme+'/'+s.kind+'/missing source ink');
  if(a)assert(near(expected,x,y),s.scheme+'/'+s.kind+'/stray native ink');
 }
 const intersectionOverUnion=overlap/(sourceInk+nativeInk-overlap);
 assert(sourceInk>0&&nativeInk>0&&intersectionOverUnion>=.95,s.scheme+'/'+s.kind+'/source silhouette overlap');
 return{scheme:s.scheme,kind:s.kind,sourceInk,nativeInk,overlap,intersectionOverUnion,maximumEdgeDistancePixels:1};
}
export async function auditBrandMark(o){const bytes=await fs.readFile(path.join(o.native,'brand-mark.json')),n=JSON.parse(bytes),refBytes=await fs.readFile('tests/Reference/BrandMark.json'),r=JSON.parse(refBytes);assert.deepEqual(await fs.readFile(path.join(o.native,'source-reference.json')),refBytes);const sourceBytes=await fs.readFile('tests/Reference/BrandMark/BleeckerLogo.png');assert.equal(hash(sourceBytes),r.asset.sha256);const source=readGrayAlphaPng(sourceBytes),catalogExpected=(o['host-assembly']??'Orchard.Catalog.dll')==='Orchard.Catalog.dll',a=verifyBrandMark(n,r,catalogExpected),exit=JSON.parse(await fs.readFile(path.join(o.native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);for(const[file,sha]of[['Orchard.dll',n.librarySha256],[o['host-assembly']??'Orchard.Catalog.dll',n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(o.native,'app-identity',file))),sha);
 let pixels=0,probes=0;const images=new Map(),paint=[],silhouettes=[];for(const p of n.pngs){const b=await fs.readFile(path.join(o.native,p.file));assert.equal(hash(b),p.sha256);const png=readRgbaPng(b);assert.equal(png.width,640);assert.equal(png.height,480);images.set(p.file,{bytes:b,png});pixels+=png.width*png.height;}for(const s of n.states){const png=images.get(s.file).png,p=verifyBrandMarkPaint(s,png,source);probes+=p.opaqueTintProbes;paint.push({scheme:s.scheme,kind:s.kind,...p});if(['size-16','size-32','size-64','size-96'].includes(s.kind))silhouettes.push(verifyBrandMarkSilhouette(s,png,source));}
 for(const scheme of['Light','Dark']){const find=kind=>n.states.find(s=>s.scheme===scheme&&s.kind===kind).file;assert.deepEqual(images.get(find('size-64')).png.rgba,images.get(find('rtl')).png.rgba,'RTL must preserve the original logo orientation');assert.deepEqual(images.get(find('size-64')).png.rgba,images.get(find('font-large')).png.rgba,'Image frame must not depend on font size');}
 return{...a,originalAssetAlphaTintProbes:probes,rawPixelsValidated:pixels,originalAssetSilhouetteVerified:silhouettes.length===8,silhouettes,paint,librarySha256:n.librarySha256,hostSha256:n.hostSha256,reportSha256:hash(bytes),referenceSha256:hash(refBytes)};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const o=Object.fromEntries(process.argv.slice(2).reduce((r,v,i,a)=>i%2===0?[...r,[v.replace(/^--/,''),a[i+1]]]:r,[])),a=await auditBrandMark(o);await fs.writeFile(o.output,JSON.stringify(a,null,2)+'\n');console.log(JSON.stringify({...a,paint:a.paint.length}));}
