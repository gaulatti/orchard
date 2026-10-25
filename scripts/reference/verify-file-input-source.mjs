import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './read-rgba-png.mjs';
import {cssRgba} from './css-paint.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const pinned=[['src/components/file-input.tsx','987ba3a36f5419069e7c3471e309d26501b15f45e8706bb529ee9b63020892ad'],['src/utils/cn.ts','74e8fe9d0d680c442ed6adb13e7d119d6c210c19ae6c114313b2a72552be0883'],['src/styles/global.css','fa7278f1107d946977c474002a09047be1e83858631f93b056435a3bbf038d96'],['src/styles/tokens.generated.css','74468d6dd3516687e3f8b96aef47e10bdcce206e31edc912abe985c45b83bf3e'],['package-lock.json','6ce0a460b61d1087b9d51ed49f949ba264005ba8bfbe0fc26260e7c8bf16f437'],['stories/components/FileInput.stories.tsx','3a717d512f6777416a34bdf5ac35646169d97c7d50c5a5fda94360eafdd91d5f']];
const ids=['sm-normal-enabled','sm-normal-disabled','sm-error-enabled','sm-error-disabled','md-normal-enabled','md-normal-disabled','md-error-enabled','md-error-disabled','lg-normal-enabled','lg-normal-disabled','lg-error-enabled','lg-error-disabled','focus','error-focus','hover','error-hover','default','story-default','story-images','multiple-empty','required-empty','single-selected','unicode-selected','long-selected','multiple-selected','zero-byte-selected','accept-advisory','required-selected','narrow','font200','rtl','custom-style','accessible-error','motion-default'];
const histories={
 'choose-replace-clear':[['initial',[],0],['selected',['note'],2],['replaced',['playlist'],4],['cleared-programmatically',[],4]],
 'live-props-retain-selection':[['updated',['note'],2]],
 'multiple-selection-live-single':[['multiple',['note','empty'],2],['now-single-keeps-files',['note','empty'],2]],
 'form-reset':[['selected',['note'],2],['reset',[],2]],
 'same-file-reselect':[['first',['note'],2],['second',['note'],4]],
 'required-validity':[['missing',[],0],['present',['note'],2],['missing-again',[],2]],
 'tab-order-disabled':[['enabled',[],0],['disabled-skipped',[],0]],
 'unmount-remount':[['unmounted',null,2],['remounted',[],2]],
 'invalid-nonempty-value':[['rejected',[],0]],
 'selection-cleared-with-files-api':[['selected',['note'],2],['empty',[],4]],
 'disabled-programmatic-selection':[['set-through-test-api',['note'],2]]
};
const data={note:{name:'fictional-note.txt',size:24,type:'text/plain',text:'Original fictional bytes'},playlist:{name:'fictional-playlist.m3u',size:7,type:'audio/x-mpegurl',text:'#EXTM3U'},empty:{name:'fictional-empty.txt',size:0,type:'text/plain',text:''}};
const metadata=f=>({name:f.name,size:f.size,type:f.type,lastModified:f.lastModified});
const equalFile=(actual,expected,withText=false)=>{assert.equal(actual.name,expected.name);assert.equal(actual.size,expected.size);assert.equal(actual.type,expected.type);assert(Number.isSafeInteger(actual.lastModified)&&actual.lastModified>0);if(withText)assert.equal(actual.text,expected.text);};
export function verifyFileInputModel(r){
 assert.equal(r.format,'orchard-file-input-reference-v1');assert.equal(r.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.deepEqual(r.dependencies.map(d=>[d.path,d.sha256]),pinned);
 assert.deepEqual(r.modules.map(m=>[m.name,m.version]),[['react','19.2.4'],['react-dom','19.2.4']]);assert.deepEqual(r.toolingModules.map(m=>[m.name,m.version]),[['tailwindcss','4.2.2'],['@tailwindcss/cli','4.2.2'],['esbuild','0.25.12']]);
 assert.equal(r.networkRequests,0);assert.equal(r.colorSchemeMatchesTheme,true);assert.equal(r.nativeImplemented,false);assert.equal(r.nativeComponentAccepted,false);assert.equal(r.nativeSystemDialogTested,false);assert.equal(r.browserChooserIntercepted,true);assert.deepEqual(r.viewport,{width:1000,height:360});assert.equal(r.cases.length,68);assert.equal(r.histories.length,22);assert.equal(r.bundledFiles.length,15);assert.equal(r.cssInputs.length,122);assert.equal(new Set(r.cssInputs.map(i=>i.path)).size,122);for(const i of r.cssInputs){assert.match(i.path,/^src\/.+\.(ts|tsx)$/);assert(!i.path.includes('..'));assert.match(i.sha256,/^[a-f0-9]{64}$/);}assert.deepEqual(r.fonts.map(f=>f.weight),[400,500]);
 let geometryValues=0,paintValues=0,selectionValues=0;const near=(actual,expected,label)=>{assert(Number.isFinite(actual)&&Math.abs(actual-expected)<.0001,`${label}: ${actual} != ${expected}`);geometryValues++;};
 for(const scheme of ['Light','Dark']){
  const cases=r.cases.filter(c=>c.scheme===scheme);assert.deepEqual(cases.map(c=>c.id),ids);const hs=r.histories.filter(h=>h.scheme===scheme);assert.deepEqual(hs.map(h=>h.id),Object.keys(histories));
  for(const c of cases){
   const n=c.measurement.input,p=n.paint,b=n.buttonPaint,s=c.spec,dark=scheme==='Dark',f=c.id==='font200'?2:1,size=s.inputSize??'md',custom=c.id==='custom-style',font=custom?20:(size==='lg'?16:14)*f,line=custom?28.5714:(size==='lg'?24:20)*f;
   assert.equal(c.stableFrames,2);assert.equal(path.basename(c.file),c.file);assert.match(c.pngSha256,/^[0-9a-f]{64}$/);assert.equal(n.type,'file');assert.equal(n.id,'original-file');assert.equal(n.name,'original-attachment');assert.equal(n.sameRef,true);assert.equal(n.tabIndex,0);assert.deepEqual(n.labels,['Original fictional attachment']);assert.equal(n.describedBy,'original-help');
   assert.equal(n.disabled,s.disabled??false);assert.equal(n.multiple,s.multiple??false);assert.equal(n.required,s.required??false);assert.equal(n.accept,s.accept??'');assert.equal(n.ariaLabel,s['aria-label']??null);assert.equal(n.ariaInvalid,s['aria-invalid']?'true':null);assert.equal(n.valueMissing,n.required&&!n.files.length);assert.equal(n.valid,!n.valueMissing);
   const filename=c.id==='unicode-selected'?'Straße 東京 é العربية.txt':c.id==='long-selected'?'Original fictional attachment with a deliberately long descriptive filename that should remain intact.txt':c.id==='zero-byte-selected'?'fictional-empty.txt':'fictional-note.txt',selected=['single-selected','unicode-selected','long-selected','multiple-selected','zero-byte-selected','accept-advisory','required-selected'].includes(c.id);
   assert.equal(n.files.length,selected?(c.id==='multiple-selected'?2:1):0);assert.equal(n.value,selected?'C:\\fakepath\\'+filename:'');if(selected){equalFile(n.files[0],{...data.note,name:filename,size:c.id==='zero-byte-selected'?0:24});if(n.multiple)equalFile(n.files[1],data.playlist);selectionValues+=n.files.length*4;}
   assert.equal(c.measurement.events.length,selected?2:0);for(const [i,e]of c.measurement.events.entries()){assert.equal(e.kind,i===0?'input':'change');assert.equal(e.trusted,false);assert.equal(e.sameRef,true);assert.deepEqual(e.files,n.files);}
   near(n.box.x,s.rtl?584:32,'x');near(n.box.y,c.id==='font200'?128:c.id==='narrow'?80:56,'y');near(n.box.width,c.id==='narrow'?160:384,'width');near(n.box.height,custom?76.5625:(size==='sm'?52: size==='lg'?64:56)*f+2,'height');
   assert.equal(p.color,dark?'rgb(240, 244, 248)':'rgb(45, 45, 45)');assert.equal(p.background,dark?'rgb(36, 52, 71)':'rgb(255, 255, 255)');assert.equal(p.border,s.error?(dark?'rgb(212, 123, 117)':'rgb(166, 93, 87)'):dark?'oklab(0.254879 -0.00520422 -0.0308993 / 0.5)':'oklab(0.879352 0.00668868 0.0424888 / 0.3)');assert.equal(p.borderWidth,'1px');assert.equal(p.borderStyle,'solid');assert.equal(p.radius,custom?'12px':'7px');assert.deepEqual(p.padding,custom?[9,13,9,13]:[8*f,8*f,8*f,8*f]);assert.deepEqual(p.margin,[0,0,0,0]);assert.equal(p.opacity,n.disabled?.5:1);assert.equal(p.cursor,n.disabled?'not-allowed':'default');assert.equal(p.appearance,'none');assert.equal(p.direction,s.rtl?'rtl':'ltr');
   for(const t of [p,b]){assert.equal(t.fontFamily,custom?'Consolas':'"Encode Sans", ui-sans-serif, system-ui, sans-serif');assert.equal(t.fontSize,font+'px');assert.equal(t.lineHeight,line+'px');assert.equal(t.transitionTimingFunction,'cubic-bezier(0.22, 1, 0.36, 1)');}assert.equal(p.fontWeight,'400');assert.equal(p.letterSpacing,'normal');assert.equal(p.transitionDuration,c.id==='motion-default'?'0.19s':'1e-05s');assert.equal(p.transitionProperty,'border-color, box-shadow, opacity');
   const focus=c.interaction==='focus',hover=c.interaction==='hover',color=dark?'rgb(91, 163, 245)':'rgb(44, 87, 132)';assert.equal(n.focused,focus);assert.equal(n.hovered,hover);assert.equal(n.active,false);assert.equal(p.shadow,focus?`rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, ${color} 0px 0px 0px 2px, rgba(0, 0, 0, 0) 0px 0px 0px 0px`:'none');
   assert.equal(b.color,color);assert.equal(b.background,`${dark?'oklab(0.704643 -0.0410104 -0.134962':'oklab(0.447932 -0.0282684 -0.0838237'} / ${hover?'0.2':'0.1'})`);assert.equal(b.borderWidth,'0px');assert.equal(b.borderStyle,'solid');assert.equal(b.radius,6*f+'px');assert.deepEqual(b.padding,[(size==='sm'?12:16)*f,(size==='sm'?8:size==='lg'?12:10)*f,(size==='sm'?12:16)*f,(size==='sm'?8:size==='lg'?12:10)*f]);assert.deepEqual(b.margin,[s.rtl?4:0,0,16*f,0]);assert.equal(b.fontWeight,'500');assert.equal(b.letterSpacing,(-font*.02)+'px');assert.equal(b.transitionDuration,'0.19s');assert.equal(b.shadow,'none');
   assert.equal(c.accessibility,`- text: Original fictional attachment\n- button "${s['aria-label']??'Original fictional attachment'}"${n.disabled?' [disabled]':''}\n- paragraph: Choose fictional local files.`);paintValues+=44;
  }
  for(const h of hs){
   assert.deepEqual(h.steps.map(s=>s.label),histories[h.id].map(s=>s[0]));for(const [index,step]of h.steps.entries()){
    const [,files,count]=histories[h.id][index],v=step.value,n=v.input;assert.equal(v.events.length,count,h.id+' event count');
    if(files===null){assert.equal(n,null);continue;}assert(n);assert.equal(n.sameRef,true);assert.equal(n.files.length,files.length);for(const [i,key]of files.entries()){equalFile(n.files[i],data[key],true);selectionValues+=5;}assert.equal(n.value,files.length?'C:\\fakepath\\'+data[files[0]].name:'');
    for(const [i,e]of v.events.entries()){assert.equal(e.kind,i%2?'change':'input');assert.equal(e.trusted,false);assert.equal(e.sameRef,true);if(i%2)assert.deepEqual(e.files,v.events[i-1].files);for(const file of e.files){const expected=Object.values(data).find(d=>d.name===file.name);assert(expected);equalFile(file,expected);}}
    if(files.length&&count)assert.deepEqual(n.files.map(metadata),v.events.at(-1).files);
    if(h.id==='required-validity'){assert.equal(n.required,true);assert.equal(n.valid,!!files.length);assert.equal(n.valueMissing,!files.length);}
    if(h.id==='multiple-selection-live-single')assert.equal(n.multiple,index===0);
   }
   if(h.id==='live-props-retain-selection'){assert.equal(h.sameNode,true);assert.equal(h.sameFile,true);const n=h.steps[0].value.input;assert.equal(n.disabled,true);assert.equal(n.required,true);assert.equal(n.accept,'image/png');assert.equal(n.ariaLabel,'Updated original label');assert.equal(n.box.height,66);}
   if(h.id==='tab-order-disabled'){assert.equal(h.steps[0].value.focus,'original-file');assert.equal(h.steps[1].value.focus,'after');assert.equal(h.steps[1].value.input.disabled,true);}
   if(h.id==='unmount-remount')assert.equal(h.sameNode,false);
   if(h.id==='invalid-nonempty-value')assert.deepEqual(h.assignment,{threw:true,name:'InvalidStateError'});
   if(h.id==='disabled-programmatic-selection')assert.equal(h.steps[0].value.input.disabled,true);
  }
 }
 return{states:r.cases.length,histories:r.histories.length,geometryValues,paintValues,selectionValues};
}

// Chromium's premultiplied 8-bit compositing is part of the frozen source
// raster. Keep its exact bytes; do not add a tolerance or substitute a rounded
// floating-point CSS blend for the observed native browser output.
export function verifyFileInputPaint(c,png){
 assert.equal(png.width,1000);assert.equal(png.height,360);const n=c.measurement.input,p=n.paint,b=n.buttonPaint,dark=c.scheme==='Dark',canvas=dark?[13,24,33]:[255,255,255],surface=dark?(n.disabled?[25,38,52]:[36,52,71]):[255,255,255];let probes=0;
 const pixel=(x,y,expected,label)=>{const index=(Math.floor(y)*png.width+Math.floor(x))*4;assert.deepEqual([...png.rgba.subarray(index,index+4)],[...expected,255],c.id+' '+c.scheme+' '+label);probes++;};
 const border=c.spec.error?(dark?(n.disabled?[113,74,75]:[212,123,117]):(n.disabled?[210,174,171]:[166,93,87])):(dark?(n.disabled?[22,34,47]:[31,43,60]):(n.disabled?[251,248,244]:[247,242,233]));
 pixel(500,300,canvas,'canvas');pixel(n.box.x+n.box.width-4,n.box.y+n.box.height/2,surface,'right inset');pixel(n.box.x+n.box.width/2,n.box.y,border,'top border');
 if(!c.spec.rtl){const button=dark?(n.disabled?[27,44,61]:c.interaction==='hover'?[47,75,106]:[41,63,89]):(n.disabled?[244,246,248]:c.interaction==='hover'?[213,221,230]:[233,238,242]);pixel(n.box.x+p.padding[0]+6,n.box.y+p.padding[1]+6,button,'button fill');}
 if(c.interaction==='focus')pixel(n.box.x-2,n.box.y+n.box.height/2,cssRgba(b.color).slice(0,3),'focus ring');return probes;
}

export async function verifyFileInputSource(referenceFile,frames){
 const bytes=await fs.readFile(referenceFile),r=JSON.parse(bytes),counts=verifyFileInputModel(r);for(const d of [...r.dependencies,...r.cssInputs])assert.equal(hash(await fs.readFile(path.join(frames,'Source',d.path))),d.sha256,'actual pinned source '+d.path);
 for(const [file,expected]of [['capture-file-input.cjs',r.scriptSha256],['file-input-client.tsx',r.clientSha256]])assert.equal(hash(await fs.readFile(new URL(file,import.meta.url))),expected,'capture provenance '+file);
 for(const file of r.bundledFiles)assert.equal(hash(await fs.readFile(file.path)),file.sha256,'actual bundle input '+file.path);
 for(const font of r.fonts)assert.equal(hash(await fs.readFile(new URL('../../src/Orchard/Assets/Fonts/'+font.name,import.meta.url))),font.sha256,'font provenance');
 for(const [file,field]of [['source.css','cssSha256'],['bundle.js','bundleSha256']])assert.equal(hash(await fs.readFile(path.join(frames,file))),r[field]);
 let pngPixels=0,pngPaletteProbes=0;for(const c of r.cases){const bytes=await fs.readFile(path.join(frames,c.file));assert.equal(hash(bytes),c.pngSha256);const png=readRgbaPng(bytes);pngPaletteProbes+=verifyFileInputPaint(c,png);pngPixels+=png.width*png.height;}
 return{format:'orchard-file-input-source-audit-v1',passed:true,referenceSha256:hash(bytes),...counts,pinnedDependencies:r.dependencies.length,cssInputs:r.cssInputs.length,bundleInputs:r.bundledFiles.length,pngs:r.cases.length,pngPixels,pngPaletteProbes,nativeSystemDialogTested:false,nativeImplemented:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const [reference,frames,output]=process.argv.slice(2);assert(reference&&frames&&output);const result=await verifyFileInputSource(reference,frames);await fs.writeFile(output,JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(result));}
