import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {assertNativePaint} from './reference/css-paint.mjs';

const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const referenceHash='bc824df8aef0b200cc74bc96a1aac183fe8e75e028f171f8c6abee633b2c7b8b';
const contracts=['original-native-file-metadata','original-picker-result','silent-set-and-clear','live-properties-retain-selection','cancellation-retains-selection','empty-multiple-cancellation','failure-propagation','handled-invoke-failure','explicit-extension-boundary','invoke-and-original-native-filters','automation-name-value-required','overlapping-picker-rejected','unload-retires-late-result','caller-style-and-local-value-precedence','implicit-style-ltr','implicit-style-rtl','custom-template-retains-caller-content'];
export function verifyFileInputNativeModel(r,source){
 assert.equal(r.format,'orchard-file-input-native-v1');assert.equal(r.passed,true);assert.equal(r.sourceReferenceSha256,referenceHash);assert.equal(r.cases.length,68);assert.equal(r.contracts.length,34);assert.equal(r.hardwareInputSent,false);assert.equal(r.nativeSystemPickerShown,false);assert.equal(r.actualPickerBoundaryOverridden,true);assert.equal(r.nativeComponentAccepted,false);assert.equal(r.nativeArm64RuntimeVerified,false);assert.equal(r.focus,0);assert.equal(r.activations,0);assert(r.guards>100);assert(Number.isFinite(r.rasterizationScale)&&r.rasterizationScale>0);
 let geometryValues=0,paintValues=0;const near=(a,b,label)=>{assert(Number.isFinite(a)&&Math.abs(a-b)<.0001,`${label}: ${a} != ${b}`);geometryValues++;};
 for(const theme of ['Light','Dark']){
  const cases=r.cases.filter(c=>c.theme===theme),expected=source.cases.filter(c=>c.scheme===theme);assert.deepEqual(cases.map(c=>c.id),expected.map(c=>c.id));assert.deepEqual(r.contracts.filter(c=>c.theme===theme).map(c=>c.id),contracts);
  for(const [i,c]of cases.entries()){
   const s=expected[i],original=s.measurement.input,n=c.snapshot;assert.equal(c.systemPickerCalls,0);assert.equal(path.basename(c.file),c.file);
   for(const part of [n.box,n.chrome])for(const field of ['x','y','width','height'])near(part[field],original.box[field],`${theme} ${c.id} ${field}`);
   assert.equal(n.inputSize,(s.spec.inputSize??'md')==='sm'?'Small':s.spec.inputSize==='lg'?'Large':'Medium');assert.equal(n.FontScale,c.id==='font200'?2:1);near(n.fontSize,parseFloat(original.paint.fontSize),'font');near(n.lineHeight,parseFloat(original.paint.lineHeight),'line height');
   assert.equal(n.IsEnabled,!original.disabled);assert.equal(n.IsError,!!s.spec.error);assert.equal(n.IsMultiple,original.multiple);assert.equal(n.IsRequired,original.required);assert.equal(n.IsSelectionMissing,!original.valid);assert.equal(n.IsPicking,false);assert.equal(n.IsTabStop,true);assert.equal(n.nativeFocus,'Unfocused');assert.equal(n.focusIndicatorVisible,s.interaction==='focus');assert.equal(n.flowDirection,s.spec.rtl?'RightToLeft':'LeftToRight');
   assert.deepEqual(n.padding,original.paint.padding);assert.deepEqual(n.choosePadding,original.buttonPaint.padding);assert.deepEqual(n.chooseMargin,original.buttonPaint.margin);assert.deepEqual(n.radius,Array(4).fill(parseFloat(original.paint.radius)));near(n.chooseRadius,parseFloat(original.buttonPaint.radius),'choose radius');assert.equal(n.rootOpacity,original.paint.opacity);assert.equal(n.chooseFontWeight,500);assert.equal(n.characterSpacing,-20);
   for(const [native,css]of [[n.foreground,original.paint.color],[n.background,original.paint.background],[n.border,original.paint.border],[n.chooseBackground,original.buttonPaint.background],[n.chooseForeground,original.buttonPaint.color]]){assertNativePaint(native,css,`${theme} ${c.id}`);paintValues++;}
   if(c.id==='custom-style'){assert.equal(n.fontFamily,'Consolas');assert.equal(n.chooseFontFamily,'Consolas');}else{assert(n.fontFamily.includes('EncodeSans-Regular.ttf'));assert(n.chooseFontFamily.includes('EncodeSans-Medium.ttf'));}
   assert.deepEqual(n.files.map(f=>f.name),original.files.map(f=>f.name));assert.equal(n.chooseText,original.multiple?'Choose Files':'Choose File');assert.equal(n.selectionText,n.files.length===0?'No file chosen':n.files.length===1?n.files[0].name:n.files.length+' files');
   assert.deepEqual(n.automation,{name:original.ariaLabel??'Original fictional attachment',className:'BleeckerFileInput',controlType:'Button',required:original.required,valid:original.valid,readOnly:true,value:n.files.map(f=>f.name).join(', '),invoke:true});
  }
  for(const c of r.contracts.filter(c=>c.theme===theme)){
   assert.equal(c.passed,true);const d=c.details;
   if(c.id==='original-native-file-metadata'){assert.equal(d.noteSize,24);assert.equal(d.emptySize,0);assert.equal(d.originalBytes,true);assert.equal(typeof d.nativeContentType,'string');}
   if(c.id==='original-picker-result'){assert.equal(d.changed,1);assert.equal(d.originalFileRetained,true);assert.equal(d.originalCallbackListRetained,true);assert.deepEqual(d.calls[0].filters,['*']);assert.equal(d.calls[0].multiple,false);assert.equal(d.calls[0].nativePickerType,'Windows.Storage.Pickers.FileOpenPicker');}
   if(c.id==='silent-set-and-clear')assert.deepEqual(d,{changed:1,selectionCount:2});
   if(c.id==='live-properties-retain-selection')assert.deepEqual(d,{sameList:true,originalObjects:true,count:2,disabledPickerRejected:true});
   if(c.id==='cancellation-retains-selection')assert.deepEqual(d,{canceled:1,changed:1,sameList:true});
   if(c.id==='empty-multiple-cancellation')assert.deepEqual(d,{canceled:2,changed:1,sameList:true});
   if(c.id==='failure-propagation')assert.deepEqual(d,{failed:1,originalError:true,originalSelectionRetained:true});
   if(c.id==='handled-invoke-failure')assert.deepEqual(d,{failed:2,originalError:true,originalSelectionRetained:true});
   if(c.id==='explicit-extension-boundary')assert.deepEqual(d,{mimeRejected:true,coreNotCalled:true});
   if(c.id==='invoke-and-original-native-filters'){assert.equal(d.callback,true);assert.equal(d.originalFileRetained,true);assert.equal(d.calls.at(-1).multiple,true);assert.deepEqual(d.calls.at(-1).filters,['.m3u','.m3u8']);}
   if(c.id==='automation-name-value-required')assert.deepEqual(d,{readOnly:true,valueRejected:true,missing:true,callerName:true});
   if(c.id==='overlapping-picker-rejected')assert.deepEqual(d,{concurrentRejected:true,busyCleared:true});
   if(c.id==='unload-retires-late-result')assert.deepEqual(d,{callbacksRetired:true,selectionCleared:true,awaitingCallerOriginalResult:true,remountEmpty:true});
   if(c.id==='caller-style-and-local-value-precedence')assert.deepEqual(d,{callerFont:true,callerPadding:true,sameValueAssignmentRetained:true,clearRestoredOnMeasure:true,originalListRetained:true});
   if(c.id==='implicit-style-ltr'||c.id==='implicit-style-rtl'){for(const key of ['callerFont','callerPadding','allSizes','bothFontScales','originalListRetained','originalFileRetained'])assert.equal(d[key],true);assert.equal(d.snapshot.fontSize,20);assert.deepEqual(d.snapshot.padding,[9,13,9,13]);assert.equal(d.snapshot.flowDirection,c.id==='implicit-style-rtl'?'RightToLeft':'LeftToRight');assert.equal(d.snapshot.files.length,1);assert.equal(d.snapshot.files[0].name,'fictional-note.txt');}
   if(c.id==='custom-template-retains-caller-content')assert.deepEqual(d,{sameEditor:true,originalEdit:true,originalFile:true});
  }
 }
 return{states:68,contracts:34,geometryValues,paintValues};
}
export function verifyFileInputNative(folder,hostAssembly){
 const r=read(path.join(folder,'file-input.json')),sourcePath=path.join(folder,'source-reference.json');assert.equal(hash(sourcePath),referenceHash);const source=read(sourcePath),counts=verifyFileInputNativeModel(r,source);assert.equal(hash(path.join(folder,'app-identity/Orchard.dll')),r.librarySha256);assert.equal(hash(path.join(folder,'app-identity',hostAssembly)),r.hostSha256);
 const process=read(path.join(folder,'process.json')),exit=read(path.join(folder,'process-exit.json'));assert.equal(process.hardwareInputSent,false);assert.equal(process.systemPickerAuthorized,false);assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 assert.deepEqual(r.pngs.map(p=>p.file),['Light','Dark'].flatMap(theme=>[...r.cases.filter(c=>c.theme===theme).map(c=>c.file),...r.catalogs.filter(c=>c.theme===theme).map(c=>c.file)]));let pixels=0,nativeVisibleGlyphPixels=0;const differences=[];
 for(const c of r.cases){const item=r.pngs.find(p=>p.file===c.file);assert(item);assert.equal(item.stableFrames,2);assert(item.attempts>=2&&item.attempts<=6);assert.equal(hash(path.join(folder,c.file)),item.sha256);const native=readRgbaPng(fs.readFileSync(path.join(folder,c.file)));assert.equal(native.width,1000);assert.equal(native.height,360);pixels+=native.width*native.height;
  const pixel=(x,y)=>[...native.rgba.subarray((Math.floor(y)*1000+Math.floor(x))*4,(Math.floor(y)*1000+Math.floor(x))*4+4)],canvas=pixel(500,300),button=pixel(c.snapshot.chooseBox.x+8,c.snapshot.chooseBox.y+8);assert.equal(canvas[3],255);assert.equal(button[3],255);assert.notDeepEqual(button,canvas,'Native choose chrome is absent');
  const glyph=c.snapshot.chooseTextBox;let ink=0;for(let y=Math.floor(glyph.y);y<Math.ceil(glyph.y+glyph.height);y++)for(let x=Math.floor(glyph.x);x<Math.ceil(glyph.x+glyph.width);x++)if(pixel(x,y).some((v,i)=>v!==button[i]))ink++;assert(ink>=20,'Native choose glyphs are absent');nativeVisibleGlyphPixels+=ink;
  const original=source.cases.find(s=>s.id===c.id&&s.scheme===c.theme),reference=readRgbaPng(fs.readFileSync(path.resolve('tests/Reference/FileInput',original.file))),box=original.measurement.input.box,pad=original.interaction==='focus'?2:0;let differingPixels=0,maximumChannelDifference=0;
  const canvasAt=(300*1000+500)*4;assert.deepEqual(canvas,[...reference.rgba.subarray(canvasAt,canvasAt+4)],'Native canvas theme differs from the frozen source');
  for(let y=Math.floor(box.y-pad);y<Math.ceil(box.y+box.height+pad);y++)for(let x=Math.floor(box.x-pad);x<Math.ceil(box.x+box.width+pad);x++){const at=(y*1000+x)*4;let differs=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(native.rgba[at+channel]-reference.rgba[at+channel]);if(delta){differs=true;maximumChannelDifference=Math.max(maximumChannelDifference,delta);}}if(differs)differingPixels++;}
  differences.push({id:c.id,theme:c.theme,differingPixels,maximumChannelDifference});
 }
 for(const c of r.catalogs){assert.equal(c.inputs,9);assert.equal(c.snapshots.length,9);for(const field of ['small','large','disabled','error','scaled'])assert.equal(c[field],true);assert.deepEqual(c.snapshots.slice(0,8).map(i=>i.box.height),[54,58,66,58,58,58,114,76.5625]);assert.deepEqual(c.snapshots.map(i=>i.fontSize),[14,14,16,14,14,14,28,20,20]);assert.deepEqual(c.snapshots[8].padding,[9,13,9,13]);for(const input of c.snapshots){assert(input.box.height>0);assert(input.chooseTextBox.height>0);assert.equal(input.automation.className,'BleeckerFileInput');}const item=r.pngs.find(p=>p.file===c.file);assert.equal(hash(path.join(folder,c.file)),item.sha256);readRgbaPng(fs.readFileSync(path.join(folder,c.file)));}
 const total=differences.reduce((sum,c)=>sum+c.differingPixels,0),maximum=Math.max(...differences.map(c=>c.maximumChannelDifference));return{format:'orchard-file-input-native-audit-v1',passed:true,scope:'hidden geometry, source brush/size recipe, visible native choose chrome/glyphs, supplied native file selection and callback/lifecycle contracts; no native system dialog or physical input',...counts,pngs:r.pngs.length,pngPixels:pixels,nativeVisibleGlyphPixels,catalogThemes:r.catalogs.length,librarySha256:r.librarySha256,hostSha256:r.hostSha256,sourceReferenceSha256:referenceHash,strictSourceComponentImageComparison:{passed:total===0,allowedChannelDifference:0,differingPixels:total,maximumChannelDifference:maximum,identicalFrames:differences.filter(d=>d.differingPixels===0).length,frames:differences},nativeSystemPickerShown:false,actualPickerBoundaryOverridden:true,nativeArm64RuntimeVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return process.argv[index+1];};const result=verifyFileInputNative(path.resolve(option('--native')),option('--host-assembly'));fs.writeFileSync(option('--output'),JSON.stringify(result,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({...result,strictSourceComponentImageComparison:{...result.strictSourceComponentImageComparison,frames:undefined}}));}
