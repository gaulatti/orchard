import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
function assertKeys(key,gesture,expectOpen=true,expectConstrained=true){
 assert.equal(key.passed,true);assert.equal(key.layer,'winui-input-keyboard-source');assert.equal(key.routedTargetRetentionClaimed,false);assert.equal(key.target,'CommandEditor');
 assert.equal(key.gesture,gesture);assert.equal(key.expectOpen,expectOpen);assert.equal(key.expectConstrained,expectConstrained);
 const keys=gesture==='Shift+F10'?['16','121']:['93'];assert.deepEqual(Object.keys(key.counts).sort(),keys.sort());
 assert.equal(key.receipts.length,keys.length*2);
 for(const value of keys){
  assert.deepEqual(key.counts[value],[1,1]);const receipts=key.receipts.filter(r=>String(r.key)===value);assert.equal(receipts.length,2);
  assert(receipts[0].kind.endsWith('down'));assert(receipts[1].kind.endsWith('up'));assert.equal(receipts[0].IsKeyReleased,false);assert.equal(receipts[1].IsKeyReleased,true);
 }
 assert(key.receipts.every(r=>r.exact&&r.exactSource&&r.RepeatCount===1));assert.deepEqual(key.errors,[]);assert.equal(key.handlersDetached,true);
}
function assertPointer(pointer,rightButton){
 assert.equal(pointer.passed,true);assert.equal(pointer.layer,'winui-input-pointer-source');assert.equal(pointer.routedXamlReleaseClaimed,false);assert.equal(pointer.rightButton,rightButton);
 assert.equal(pointer.downs,1);assert.equal(pointer.ups,1);assert.deepEqual(pointer.order,['down','up']);assert.equal(pointer.receipts.length,2);
 assert(pointer.receipts.every(r=>r.exact&&r.exactSource));assert.deepEqual(pointer.errors,[]);assert.equal(pointer.handlersDetached,true);
}
export async function auditTextareaCommands(native,width){
 const source=await read(new URL('../tests/Reference/TextareaCommands.json',import.meta.url));
 const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'textarea-commands.json'));
 assert.equal(result.passed,true);assert.equal(result.component,'textarea-commands');assert.equal(report.passed,true);assert.deepEqual(report.mismatches,[]);
 assert.equal(result.environment.processArchitecture,'X64');assert.equal(result.environment.osArchitecture,'X64');assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.highContrast,false);
 const expected=source.cases.filter(c=>c.width===width);assert.equal(expected.length,18);assert.equal(report.frames.length,18);
 for(const[index,test]of expected.entries()){
  const frame=report.frames[index],context=test.scheme+'/'+test.id,id=`${test.scheme}-${test.id}`;
  assert.equal(frame.id,test.id);assert.equal(frame.width,width);assert.equal(frame.scheme,test.scheme);assert.equal(frame.text,test.actual.text,context);assert.equal(frame.consumerText,frame.text,context);
  assert.equal(frame.IsReadOnly,test.actual.readOnly);assert.equal(frame.clipboardText,test.clipboard.normalizedText,context);assert.equal(frame.clipboardRaw.replace(/\r\n?/g,'\n'),frame.clipboardText);
  assert.equal(frame.html,false,context);assert.equal(frame.rtf,false,context);assert.equal(frame.matches,true,context);assert.equal(frame.invoked,frame.shouldInvoke,context);
  assert.equal(frame.shouldInvoke,!test.id.startsWith('readonly-')||test.id==='readonly-copy');
  assert.equal(frame.menuConstrained,true,context);assert.notEqual(frame.focus,'Unfocused',context);
  if(frame.gesture==='Right click'){
   const open=await read(path.join(native,`textarea-commands-menu-pointer-context-${id}.json`));assertPointer(open,true);assert.equal(open.target,'CommandEditor');
  }else{
   const key=await read(path.join(native,`textarea-commands-context-key-${id}.json`));assertKeys(key,frame.gesture);
  }
  const pointer=await read(path.join(native,`textarea-commands-menu-pointer-${id}.json`));assertPointer(pointer,false);assert.equal(pointer.target,'TextareaCommand-'+test.id);
  const png=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(png.width,width);assert.equal(png.height,600);
  const menuPng=readRgbaPng(await fs.readFile(path.join(native,`menu-${id}.png`)));assert.equal(menuPng.width,width);assert.equal(menuPng.height,600);
 }
 for(const scheme of['Light','Dark']){
  const ownership=await read(path.join(native,`textarea-command-ownership-${scheme}.json`));assert.equal(ownership.passed,true);
  assert.deepEqual(ownership.phases,['local-null','reset-null','rtl-copy','consumer-binding','reset-binding','template-close','unload-close']);
  for(const[id,gesture,open,constrained]of[['local-null','Apps',false,true],['rtl-copy','Shift+F10',true,true],['consumer-binding','Apps',true,false],['template','Apps',true,true],['unload','Apps',true,true]])
   assertKeys(await read(path.join(native,`textarea-commands-context-key-${scheme}-${id}.json`)),gesture,open,constrained);
  assertPointer(await read(path.join(native,`textarea-commands-menu-pointer-${scheme}-rtl-copy.json`)),false);
 }
 return{width,commandStates:18,ownershipPhases:14,sourceClipboardFormatsVerified:true,realContextGesturesVerified:true,nativeMenuInvocationsVerified:true,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
 const report=await auditTextareaCommands(path.resolve(option('--native')),Number(option('--width')));await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
