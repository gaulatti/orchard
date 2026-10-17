import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditTextareaResize(native,width){
 const source=await read(new URL('../tests/Reference/TextareaResize.json',import.meta.url));
 const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'textarea-resize.json'));
 assert.equal(result.passed,true);assert.equal(result.component,'textarea-resize');assert.equal(report.passed,true);
 assert.equal(result.environment.processArchitecture,'X64');assert.equal(result.environment.osArchitecture,'X64');
 assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.highContrast,false);
 const expected=source.cases.filter(c=>c.width===width);assert.equal(expected.length,36);assert.equal(report.frames.length,36);
 const desktop=await read(path.join(native,'textarea-resize-desktop.json'));
 assert(desktop.pointerMoves.length>=36*20);
 for(const move of desktop.pointerMoves){
  assert.equal(move.positioned,true);assert.equal(move.hit.hit.process,move.hit.own.process);
  assert.equal(move.actual.X,move.requested.x);assert.equal(move.actual.Y,move.requested.y);
  if(!move.stationary){assert.equal(move.delivered,true);assert(move.receipts.some(r=>r.IsGenerated===false&&r.exact===true));}
 }
 for(const[index,test]of expected.entries()){
  const actual=report.frames[index],context=`${test.scheme}/${test.id}`;
  assert.equal(actual.id,test.id);assert.equal(actual.scheme,test.scheme);assert.equal(actual.width,width);assert.equal(actual.captured,true,context);
  assert.equal(actual.IsEnabled,test.mode!=='disabled',context);assert.equal(actual.IsReadOnly,test.mode==='readonly',context);
  assert.equal(actual.focus,'Unfocused',context);assert.equal(actual.Text,test.initial.value,context);
  for(const [state,browser]of[['enlarged','enlargedHeld'],['minimum','minimumHeld'],['reversed','reverseAboveMinimum']]){
   assert.equal(actual[state].height,test[browser].height,context+'/'+state);
   assert.equal(actual[state].width,test.initial.width,context+'/'+state);
   assert.equal(actual[state].text,test.initial.value,context+'/'+state);
   assert.equal(actual[state].enabled,test.mode!=='disabled',context+'/'+state);
   assert.equal(actual[state].focus,'Unfocused',context+'/'+state);
  }
  assert.equal(actual.gripBounds.X,test.dir==='rtl'?20:20+test.initial.width-12,context+'/grip');
  if(test.id==='lg-disabled-rtl')assert.equal(actual.lifecycleCancellation,true,context+'/lifecycle');
  // Layout changes can generate additional native PointerMoved messages.
  // Each injected move has its own non-generated receipt in the desktop log.
  assert(actual.receipts.filter(r=>r.IsGenerated===false).length>10,context);
  assert(actual.receipts.some(r=>r.pressed===true),context);assert(actual.receipts.some(r=>r.pressed===false),context);
  assert.equal(actual.file,test.file);const png=readRgbaPng(await fs.readFile(path.join(native,test.file)));
  assert.equal(png.width,width);assert.equal(png.height,600);
 }
 return {width,cases:36,disabledResizes:12,readonlyResizes:12,trustedNativePointer:true,sourceHeightAndAnchorVerified:true,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
 const report=await auditTextareaResize(path.resolve(option('--native')),Number(option('--width')));
 await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
