import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditTextareaLimits(native,width){
 const source=await read(new URL('../tests/Reference/TextareaLimits.json',import.meta.url));
 const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'textarea-limits.json'));
 assert.equal(result.passed,true);assert.equal(result.component,'textarea-limits');assert.equal(report.passed,true);assert.deepEqual(report.mismatches,[]);
 assert.equal(result.environment.processArchitecture,'X64');assert.equal(result.environment.osArchitecture,'X64');
 assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.highContrast,false);
 const expected=source.cases.filter(c=>c.width===width);assert.equal(expected.length,28);assert.equal(report.frames.length,28);
 for(const[index,test]of expected.entries()){
  const actual=report.frames[index],context=test.scheme+'/'+test.id;
  assert.equal(actual.id,test.id);assert.equal(actual.width,width);assert.equal(actual.scheme,test.scheme);assert.equal(actual.matches,true,context);
  assert.equal(actual.initial,test.spec.initial,context);assert.equal(actual.text,test.actual.text,context);assert.equal(actual.length,test.actual.length,context);
  assert.deepEqual(actual.selection,test.actual.selection,context);assert.equal(actual.consumerText,actual.text,context);assert.equal(actual.modelRaw.replace(/\r\n?/g,'\n'),actual.text,context);
  assert.equal(actual.MaxLength,test.actual.maxLength===-1?0:test.actual.maxLength,context);
  if(test.id==='overflow-replacement'){assert.equal(actual.nativeSelection[0],0);assert(actual.nativeSelection[1]>=actual.initial.length&&actual.nativeSelection[1]<=actual.initial.length+1);}
  assert.equal(actual.IsReadOnly,test.actual.readOnly,context);assert.equal(actual.IsError,false);assert.equal(actual.IsInvalid,null);assert.equal(actual.dataValid,true);
  if(test.spec.paste!==undefined){assert.equal(actual.undo,test.undo,context+'/undo');assert.equal(actual.undoConsumer,actual.undo,context+'/undo-binding');if(!actual.IsReadOnly)assert.equal(actual.pasteEvents,1,context+'/native-paste');}
  assert.equal(actual.file,test.file);const png=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(png.width,width);assert.equal(png.height,600);
 }
 for(const scheme of['Light','Dark']){
  const deferred=await read(path.join(native,`textarea-deferred-${scheme}.json`));assert.equal(deferred.passed,true);assert.equal(deferred.cases.length,9);
  const actions=['limit-decrease','limit-increase','content','selection','readonly','disabled','template','remount','rapid-remount'];
  for(const[index,item]of deferred.cases.entries()){
   assert.equal(item.action,actions[index]);const expected=item.action==='limit-decrease'?'abc':item.action==='limit-increase'?'abcde':item.action==='content'?'Replacement':'ab';
   assert.equal(item.text,expected);assert.equal(item.consumerText,expected);assert.equal(item.expected,expected);assert.equal(item.providerRequested,true);
   assert.equal(item.operationCompleted,true);
   assert.equal(item.MaxLength,item.action==='limit-decrease'?3:5);assert.equal(item.IsReadOnly,item.action==='readonly');assert.equal(item.IsEnabled,item.action!=='disabled');
  }
  const menu=await read(path.join(native,`textarea-menu-limits-${scheme}.json`));
  assert.equal(menu.passed,true);assert.equal(menu.clicked,true);assert.equal(menu.text,'abc');assert.equal(menu.consumerText,'abc');assert.equal(menu.undo,'ab');
  assert.equal(menu.readOnlyPasteDisabled,true);assert.equal(menu.readOnlyCopyEnabled,true);
  const pointer=await read(path.join(native,`textarea-limits-menu-pointer-${scheme}.json`));
  assert.equal(pointer.passed,true);assert.equal(pointer.layer,'winui-input-pointer-source');assert.equal(pointer.routedXamlReleaseClaimed,false);assert.equal(pointer.target,'TextareaMenuPaste');
  assert.equal(pointer.downs,1);assert.equal(pointer.ups,1);assert.deepEqual(pointer.order,['down','up']);assert.equal(pointer.receipts.length,2);assert(pointer.receipts.every(r=>r.exact&&r.exactSource));assert.deepEqual(pointer.errors,[]);assert.equal(pointer.handlersDetached,true);
 }
 const desktop=await read(path.join(native,'textarea-limits-desktop.json'));assert(desktop.keyActions.length>=94);
 for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 return {width,cases:28,deferredClipboardCases:18,menuPasteCases:2,nativeKeyActions:desktop.keyActions.length,clipboardAndBindingVerified:true,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
 const report=await auditTextareaLimits(path.resolve(option('--native')),Number(option('--width')));await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
