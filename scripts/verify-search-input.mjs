import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditSearchInput(native,width){
 const reference=await read(new URL('../tests/Reference/SearchInput.json',import.meta.url));
 const script=(await fs.readFile(new URL('./reference/capture-search-input.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n');
 assert.equal(crypto.createHash('sha256').update(script).digest('hex'),reference.scriptSha256);
 const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'search-input.json'));
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'search-input');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
 assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.highContrast,false);
 assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);assert(['X64','Arm64'].includes(runtime.environment.processArchitecture));
 const expected=reference.cases.filter(item=>item.width===width);assert.equal(expected.length,20);assert.equal(report.reports.length,expected.length);
 let maximumGeometryError=0,context='';const near=(actual,target)=>{assert(Number.isFinite(actual),context);const delta=Math.abs(actual-target);maximumGeometryError=Math.max(maximumGeometryError,delta);assert(delta<=.05,`${context}: ${actual} differs from ${target} by ${delta}`);};
 const box=(actual,target)=>{for(const key of ['x','y','width','height'])near(actual[key],target[key]);};
 for(const [index,test]of expected.entries()){
  const actual=report.reports[index];context=`${test.id}/${test.scheme}`;assert.equal(actual.id,test.id);assert.equal(actual.scheme,test.scheme);assert.equal(actual.width,width);
  box(actual.editor.box,test.normal.editor.box);box(actual.search,test.normal.search);assert.equal(actual.editor.value,test.normal.editor.value);assert.equal(actual.editor.IsEnabled,!test.normal.editor.disabled);assert.equal(actual.editor.IsReadOnly,test.normal.editor.readOnly);
  assert.equal(actual.editor.role,'Edit');assert.equal(actual.editor.name,'Search catalogue');assert.deepEqual(Object.values(actual.editor.Padding),[40,0,32,0]);assert.equal(actual.editor.FontSize,test.props.inputSize==='sm'?13:test.props.inputSize==='lg'?15:14);
  if(test.normal.clear){box(actual.clear.box,test.normal.clear.box);assert.equal(actual.clear.role,'Button');assert.equal(actual.clear.name,test.normal.clear.name);assert.equal(actual.clear.IsEnabled,true);}else assert.equal(actual.clear,null);
  const image=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(image.width,width);assert.equal(image.height,480);
 }
 assert.equal(report.inputs.length,2);
 for(const input of report.inputs){assert.equal(input.clears,6);assert.equal(input.executions,1);for(const flag of ['consumerOwnership','disabledEditorClear','readOnlyClear','readOnlyEscape','escapeChange','nativeTyping','selectionReplacement','backspace','twoWayBinding','liveNames','commandGuard','inheritedDisabled','sizePadding','templateReplacement','unloadRemount','optionalClear','consumerClear'])assert.equal(input[flag],true);}
 const desktop=await read(path.join(native,'search-input-desktop.json'));
 for(const move of desktop.pointerMoves){assert.notEqual(move.deliveryFailed,true);assert.equal(move.positioned,true);assert(move.stationary||move.delivered);}
 for(const gate of desktop.pointerPresentations){assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);}
 for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 for(const click of desktop.pointerClicks){assert.equal(click.delivered,true);assert(click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));}
 assert.equal(desktop.keyActions.length,28);assert.equal(desktop.pointerClicks.length,10);
 const held=await read(path.join(native,'search-input-held-keys.json'));assert.equal(held.length,2);for(const key of held){assert.equal(key.key,9);assert.equal(key.downs,1);assert.equal(key.ups,1);assert(key.downOwned&&key.upOwned&&key.focusTransferred);assert.equal(key.downTarget,'SearchEditor');assert.equal(key.upTarget,'ClearSearch');}
 for(const input of report.inputs)for(const flag of ['clipboard','maxLength','tabOrder'])assert.equal(input[flag],true);
 assert.equal(desktop.keyActions.filter(key=>key.modifier===17&&key.modifierDowns===1&&key.modifierUps===1).length,4);
 assert.equal(report.accessibility.length,12);
 for(const theme of ['Light','Dark'])for(const id of ['default','disabled','readonly','empty','updated','remounted']){
  const checks=report.accessibility.filter(item=>item.scheme===theme&&item.id===id);assert.equal(checks.length,1);const tree=checks[0].tree;assert.notEqual(tree.process,tree.clientProcess);assert.equal(tree.editors.length,1);const edit=tree.editors[0];assert.equal(edit.type,'ControlType.Edit');assert(edit.hasText&&edit.hasValue);assert.equal(edit.name,['updated','remounted'].includes(id)?'Updated search name':'Search catalogue');assert.equal(edit.enabled,id!=='disabled');assert.equal(edit.readOnly,id==='readonly');assert.equal(edit.value,id==='empty'?'':id==='updated'?'External update':id==='remounted'?'Remounted text':'Culture');
  assert.equal(tree.buttons.length,id==='empty'?0:1);if(tree.buttons.length){const button=tree.buttons[0];assert.equal(button.type,'ControlType.Button');assert(button.hasInvoke&&button.enabled&&button.focusable);assert.equal(button.name,['updated','remounted'].includes(id)?'Limpar pesquisa':'Clear search');}
 }
 return{passed:true,width,cases:expected.length,maximumGeometryError,geometryTolerance:.05,ownedKeyPairs:desktop.keyActions.length,ownedTabTransfers:held.length,physicalClicks:desktop.pointerClicks.length,externalUiaSnapshots:report.accessibility.length,sourcePixelParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1],`Pass ${name}.`);return process.argv[index+1];};
 const report=await auditSearchInput(path.resolve(option('--native')),Number(option('--width')));await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
