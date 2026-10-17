import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditTextarea(native,width){
 const source=await read(new URL('../tests/Reference/Textarea.json',import.meta.url));
 const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'textarea.json'));
 assert.equal(result.passed,true);assert.equal(result.component,'textarea');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
 assert.equal(result.environment.processArchitecture,'X64');assert.equal(result.environment.osArchitecture,'X64');
 assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.highContrast,false);
 const expected=source.cases.filter(c=>c.width===width);assert.equal(expected.length,86);assert.equal(report.frames.length,expected.length);
 for(const[index,test]of expected.entries()){
  const actual=report.frames[index],context=test.file;
  assert.equal(actual.file,test.file);assert.equal(actual.scheme,test.scheme);assert.equal(actual.state,test.state);
  assert.equal(actual.width,test.box.width,context+'/width');assert.equal(actual.height,test.box.height,context+'/height');
  assert.equal(actual.Rows,test.rows);assert.equal(actual.text,test.value);assert.equal(actual.LineHeight,parseFloat(test.paint.lineHeight));
  assert.equal(actual.nativeFont,actual.fontFamily);assert(actual.fontFamily.endsWith('/EncodeSans-Regular.ttf#Encode Sans'));assert.equal(actual.nativeFontSize,parseFloat(test.paint.fontSize)*.75);
  assert.equal(actual.lineRule,'Exactly');assert(Math.abs(actual.lineSpacing*4/3-actual.LineHeight)<.001);
  assertNativePaint({color:actual.border,opacity:actual.borderOpacity},test.paint.borderColor,context+'/border');
  assert(Math.abs(actual.editorOpacity-Number(test.paint.opacity))<.000001,context+'/opacity');
  const png=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(png.width,width);assert.equal(png.height,600);
 }
 assert.equal(report.interactions.length,2);
 for(const item of report.interactions)for(const key of['nativeEnter','selectionReplacement','undo','readOnlySelection','twoWayBinding','keyboardResize','templateReplacement','unloadRemount','trailingNewline','formattingShortcutsBlocked','plainTextCommandSurfaces','richClipboardRejected','pasteUndo'])assert.equal(item[key],true,key);
 for(const item of report.interactions){assert.equal(item.editorAfterEnter,'First\na');assert.equal(item.nativeConsumerAfterEnter.replace(/\r\n?/g,'\n'),item.editorAfterEnter);}
 for(const item of report.interactions)for(const[key,readonly]of[['editableUia',false],['readonlyUia',true]]){
  const uia=item[key];assert.equal(uia.process,report.ownerProcess);assert.notEqual(uia.clientProcess,uia.process);
  assert.equal(uia.name,'Editorial notes');assert.equal(uia.hasText,true);assert.equal(uia.readOnly,readonly);assert.equal(uia.enabled,true);
  assert.equal(uia.type,'ControlType.Edit');assert.equal(uia.text.replace(/\r\n?/g,'\n'),'First\na');assert.equal(uia.selectionCount,1);
 }
 const desktop=await read(path.join(native,'textarea-desktop.json')),keys=desktop.keyActions;assert(keys.length>=18);
 for(const key of keys){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 return {width,frames:expected.length,interactionSequences:report.interactions.length,nativeKeyActions:keys.length,geometryAndBorderPaintVerified:true,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const report=await auditTextarea(path.resolve(option('--native')),Number(option('--width')));await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
