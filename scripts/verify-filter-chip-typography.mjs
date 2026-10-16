import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditFilterChipTypography(native){
 const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'filter-chip-typography.json'));
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'filter-chip-typography');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
 assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.highContrast,false);
 const steps=['normal','large','tracking','system-family','italic-expanded','secondary-family','rtl','constrained','unicode','restored'];
 assert.deepEqual(report.reports.map(item=>item.phase),['Light','Dark'].flatMap(theme=>steps.map(step=>theme+'/'+step)));
 let textMeasurements=0,maximumWidthError=0;
 for(const item of report.reports){
  const [theme,step]=item.phase.split('/'),owner=item.owner;assert.equal(item.texts.length,5);assert.equal(item.chips.length,2);
  assert.equal(owner.FontSize,step==='normal'||step==='restored'?12:20);assert.equal(owner.CharacterSpacing,['normal','large','restored'].includes(step)?0:125);
  assert.equal(owner.style,['italic-expanded','secondary-family','rtl','constrained','unicode'].includes(step)?'Italic':'Normal');
  assert.equal(owner.stretch,['italic-expanded','secondary-family','rtl','constrained','unicode'].includes(step)?'Expanded':'Normal');
  assert.equal(owner.direction,['rtl','constrained','unicode'].includes(step)?'RightToLeft':'LeftToRight');
  for(const text of item.texts){
   assert.equal(text.FontSize,owner.FontSize);assert.equal(text.CharacterSpacing,owner.CharacterSpacing);assert.equal(text.style,owner.style);assert.equal(text.stretch,owner.stretch);assert.equal(text.direction,owner.direction);assert.equal(text.IsTextScaleFactorEnabled,true);
   assert.equal(text.lineHeight,Math.fround((text.role==='ClearFilterText'?16:18)*owner.FontSize/12));
   assert(Number.isFinite(text.intrinsic)&&text.intrinsic>0);const delta=Math.abs(text.box.Item3-text.intrinsic);maximumWidthError=Math.max(maximumWidthError,delta);assert(delta<.02,`${item.phase}/${text.role}: rendered paragraph differs from independent native measurement`);
   assert(Number.isFinite(text.baseline)&&text.baseline>=text.box.Item2&&text.baseline<=text.box.Item2+text.box.Item4);textMeasurements++;
  }
  if(step==='constrained')assert(new Set(item.chips.map(chip=>chip.box.Item2)).size>1);
  if(step==='rtl'||step==='unicode')assert(item.chips[0].box.Item1>item.chips[1].box.Item1);
  const image=readRgbaPng(await fs.readFile(path.join(native,`filter-chip-typography-${step}-${theme}.png`)));assert.equal(image.width,640);assert.equal(image.height,480);
 }
 const desktop=await read(path.join(native,'filter-chip-typography-desktop.json'));assert.equal(desktop.keyActions.length,2);
 for(const key of desktop.keyActions){assert.equal(key.key,32);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 for(const move of desktop.pointerMoves){assert.notEqual(move.deliveryFailed,true);assert.equal(move.positioned,true);assert(move.stationary||move.delivered);}
 for(const gate of desktop.pointerPresentations)assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);
 return{passed:true,phases:report.reports.length,textMeasurements,maximumWidthError,widthTolerance:.02,ownedSpacePairs:desktop.keyActions.length,operatingSystemSettingsChanged:false,actualWindowsTextScalingVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
 const report=await auditFilterChipTypography(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
