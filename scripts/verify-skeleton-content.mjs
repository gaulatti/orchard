import fs from 'node:fs/promises';import path from 'node:path';import crypto from 'node:crypto';import assert from 'node:assert/strict';import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const rgba=color=>{assert.match(color,/^#[0-9A-F]{8}$/);return [3,5,7,1].map(i=>parseInt(color.slice(i,i+2),16));};
export function verifySkeletonContent(n,reference,identity){
 assert.equal(n.format,'orchard-skeleton-content-native-v1');assert.equal(n.passed,true);assert.equal(reference.format,'orchard-skeleton-reference-v1');
 assert.equal(n.librarySha256,identity.librarySha256);assert.equal(n.hostSha256,identity.hostSha256);assert.equal(n.focus,0);assert.equal(n.activations,0);assert(n.guards>200);
 assert.equal(n.animationsEnabled,false);assert.equal(n.reducedMotionVerified,true);assert.equal(n.publicApisOnly,true);
 for(const flag of ['normalPulseVerified','osSettingsChanged','physicalInputVerified','nativeComponentAccepted','nativeARM64RuntimeVerified','compositionShadowRasterVerified','sourceRasterParityVerified'])assert.equal(n[flag],false,flag);
 assert.equal(n.rootWidth,420);assert.equal(n.rootHeight,620);assert.equal(n.states.length,36);assert.equal(n.contracts.length,16);assert.equal(n.captures.length,40);
 const close=(a,b)=>assert(Number.isFinite(a)&&Math.abs(a-b)<=reference.geometryToleranceDip,`${a} differs from ${b}`);
 const stateKeys=[],contractKeys=[],files=[];
 for(const scheme of ['Light','Dark'])for(const flow of ['LeftToRight','RightToLeft']){
  const palette=reference.themes[scheme];
  for(const spec of reference.cases){
   const key=scheme+'/'+flow+'/'+spec.id;stateKeys.push(key);const s=n.states.find(s=>s.scheme+'/'+s.flow+'/'+s.id===key);assert(s,key);assert.equal(s.kind,spec.kind);assert.equal(s.decorative,true);close(s.box.x,24);close(s.box.y,24);close(s.box.width,spec.width);close(s.box.height,spec.height);assert.equal(s.shapes.length,spec.shapes);
   if(spec.rows!==undefined)assert.equal(s.rows,spec.rows);else assert.equal(s.rows,null);if(spec.columns!==undefined)assert.equal(s.columns,spec.columns);else assert.equal(s.columns,null);
   // WinUI serializes these brush values through native single precision.
   const opacity=(actual,expected)=>assert(Number.isFinite(actual)&&Math.abs(actual-expected)<=1e-7);
   if(spec.kind==='table')assert.equal(s.background,null);else {assert.equal(s.background.color,spec.kind==='card'?palette.card:palette.shape);opacity(s.background.opacity,spec.kind==='card'?1:palette.shapeOpacity);}
   for(const shape of s.shapes){assert.equal(shape.opacity,1);assert.equal(shape.brush.color,palette.shape);opacity(shape.brush.opacity,palette.shapeOpacity);assert(shape.box.width>0&&shape.box.height>0);}
   const shapeAt=(index,x,y,w,h,radius)=>{const shape=s.shapes[index];close(shape.box.x,x);close(shape.box.y,y);close(shape.box.width,w);close(shape.box.height,h);for(const r of shape.radii)close(r,radius);};
   if(spec.kind==='shape')shapeAt(0,24,24,spec.width,spec.height,spec.radius);
   if(spec.kind==='card'){
    const text=spec.width-100,right=24+spec.width-24;
    shapeAt(0,flow==='LeftToRight'?48:right-40,48,40,40,20);
    shapeAt(1,flow==='LeftToRight'?100:right-52-text/3,50,text/3,16,7);
    shapeAt(2,flow==='LeftToRight'?100:right-52-text/2,74,text/2,12,6);
    for(let i=0;i<spec.rows;i++)shapeAt(3+i,48,100+i*24,spec.width-48,12,6);
   }
   if(spec.kind==='table'&&spec.columns>0){const width=(spec.width-8*(spec.columns-1))/spec.columns;for(let row=0;row<=spec.rows;row++)for(let col=0;col<spec.columns;col++)shapeAt(row*spec.columns+col,flow==='LeftToRight'?24+col*(width+8):24+spec.width-width-col*(width+8),row===0?24:64+(row-1)*48,width,row===0?32:40,7);}
   assert.equal(s.capture,`skeleton-${scheme}-${flow}-${spec.id}.png`);files.push(s.capture);
  }
  const get=id=>{const key=scheme+'/'+flow+'/'+id;contractKeys.push(key);const c=n.contracts.find(c=>c.scheme+'/'+c.flow+'/'+c.id===key);assert(c?.passed,key);return c;};
  const original=get('original-content');assert.equal(original.originalRetained,true);assert.equal(original.clicks,1);assert.equal(original.childInPeerTree,true);assert.equal(original.childControlElement,true);
  const template=get('template-restoration');for(const flag of ['contentRetained','editRetained','bindingRetained'])assert.equal(template[flag],true);
  const model=get('caller-templates');for(const flag of ['modelRetained','selectorModelRetained','originalRestored'])assert.equal(model[flag],true);assert(model.selectorCalls>0);
  const remount=get('opacity-and-remount');for(const flag of ['contentRetained','editRetained','opacityBindingRetained'])assert.equal(remount[flag],true);assert.equal(remount.clicks,2);assert.equal(remount.opacity,.25);assert.equal(remount.capturedOpacity,.75);close(remount.patchBox.width,60);close(remount.patchBox.height,12);assert.equal(remount.capture,`skeleton-${scheme}-${flow}-caller-content.png`);files.push(remount.capture);
 }
 assert.deepEqual(n.states.map(s=>s.scheme+'/'+s.flow+'/'+s.id).sort(),stateKeys.sort());assert.deepEqual(n.contracts.map(c=>c.scheme+'/'+c.flow+'/'+c.id).sort(),contractKeys.sort());assert.deepEqual(n.captures.map(c=>c.file).sort(),files.sort());
 return {passed:true,defaultStates:36,callerContracts:16,themes:2,directions:2,originalContentBindingsAndModelsVerified:true,childActionAndPeerTreeVerified:true,reducedMotionVerified:true,normalPulseVerified:false,compositionShadowRasterVerified:false,nativeComponentAccepted:false,librarySha256:n.librarySha256,hostSha256:n.hostSha256};
}
export async function auditSkeletonContent(options){
 const bytes=await fs.readFile(path.join(options.native,'skeleton-content.json')),n=JSON.parse(bytes),referenceBytes=await fs.readFile(options.reference),reference=JSON.parse(referenceBytes);
 assert.equal(n.referenceSha256,hash(referenceBytes));for(const pin of [reference.componentSource,reference.tokenSource])assert.equal(hash(await fs.readFile(path.join(path.dirname(options.reference),pin.path))),pin.sha256);
 const source=await fs.readFile(path.join(options.native,'source-reference.json'));assert.equal(hash(source),hash(referenceBytes));
 const host=options['host-assembly']??'Orchard.Catalog.dll',audit=verifySkeletonContent(n,reference,{librarySha256:hash(await fs.readFile(path.join(options.app,'Orchard.dll'))),hostSha256:hash(await fs.readFile(path.join(options.app,host)))});
 const exit=JSON.parse(await fs.readFile(path.join(options.native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 let pixelCount=0,colorSamples=0,emptyPixels=0,callerOpacitySamples=0;
 for(const entry of n.captures){const data=await fs.readFile(path.join(options.native,entry.file));assert.equal(hash(data),entry.sha256);const png=readRgbaPng(data);assert.equal(png.width,entry.width);assert.equal(png.height,entry.height);pixelCount+=png.width*png.height;
  const sample=(x,y,expected)=>{const col=Math.floor(x*png.width/420),row=Math.floor(y*png.height/620);assert(col>=0&&col<png.width&&row>=0&&row<png.height);for(let c=0;c<4;c++)assert(Math.abs(png.rgba[(row*png.width+col)*4+c]-expected[c])<=1,`${entry.file} pixel ${col},${row} channel${c}`);colorSamples++;};
  const blend=(front,back,opacity)=>front.map((v,c)=>c===3?255:Math.round(v*opacity+back[c]*(1-opacity)));
  const state=n.states.find(s=>s.capture===entry.file);
  if(state){const palette=reference.themes[state.scheme],background=rgba(palette.background);sample(4,4,background);const under=state.kind==='card'?rgba(palette.card):background;
   for(const shape of state.shapes)sample(shape.box.x+shape.box.width/2,shape.box.y+shape.box.height/2,blend(rgba(palette.shape),under,palette.shapeOpacity));
   if(state.kind==='card')sample(state.box.x+12,state.box.y+12,under);
   if(state.kind==='table'&&state.columns===0){for(let y=Math.ceil(state.box.y*png.height/620);y<Math.floor((state.box.y+state.box.height)*png.height/620);y++)for(let x=Math.ceil(state.box.x*png.width/420);x<Math.floor((state.box.x+state.box.width)*png.width/420);x++){for(let c=0;c<4;c++)assert.equal(png.rgba[(y*png.width+x)*4+c],background[c],'Empty table painted content');emptyPixels++;}}
  }else{
   const contract=n.contracts.find(c=>c.capture===entry.file);assert(contract);const b=contract.patchBox,palette=reference.themes[contract.scheme];
   // Native RTB captures fade the overlapping primitives individually. Keep
   // the CSS isolated-group disagreement recorded as a separate visual limit.
   const under=blend(rgba(palette.shape),rgba(palette.background),palette.shapeOpacity*.75);
   sample(b.x+b.width/2,b.y+b.height/2,blend([51,102,153,255],under,.75));callerOpacitySamples++;
  }
 }
 return {...audit,captures:40,rawPixelsValidated:pixelCount,sourceColorSamples:colorSamples-callerOpacitySamples,nativeCallerOpacitySamples:callerOpacitySamples,callerOpacitySourceGroupRasterVerified:false,emptyTablePixelsVerified:emptyPixels,reportSha256:hash(bytes)};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const args=process.argv.slice(2);assert.equal(args.length%2,0);const o=Object.fromEntries(args.reduce((p,v,i)=>i%2===0?[...p,[v.replace(/^--/,''),args[i+1]]]:p,[]));const audit=await auditSkeletonContent(o);await fs.writeFile(o.output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));}
