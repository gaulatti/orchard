import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
export async function auditFilterChip(native,width){
 const reference=await read(new URL('../tests/Reference/FilterChip.json',import.meta.url));
 assert.equal(hash((await fs.readFile(new URL('./reference/capture-filter-chip.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n')),reference.scriptSha256);
 const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'filter-chip.json'));
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'filter-chip');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
 assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.highContrast,false);
 assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);assert(['X64','Arm64'].includes(runtime.environment.processArchitecture));
 const expected=reference.cases.filter(item=>item.width===width);assert.equal(expected.length,20);assert.equal(report.reports.length,expected.length);
 let maximumGeometryError=0,maximumBaselineError=0,baselines=0,context='';
 const near=(actual,target)=>{assert(Number.isFinite(actual),context);const delta=Math.abs(actual-target);maximumGeometryError=Math.max(maximumGeometryError,delta);assert(delta<=.05,`${context}: ${actual} differs from ${target} by ${delta}`);};
 const box=(actual,target)=>{for(const key of ['x','y','width','height'])near(actual[key],target[key]);};
 const paint=(actual,target)=>{
  let channels=target.match(/^rgba?\(([^)]+)\)$/)?.[1].split(',').map(Number);
  const lab=target.match(/^oklab\(([^)]+)\)$/)?.[1].split(/[\s/]+/).map(Number);
  if(lab){
   // Same OKLab-to-sRGB matrix as the repository's BleeckerColorMix.
   const [L,a,b,alpha=1]=lab,l=(L+.3963377774*a+.2158037573*b)**3,m=(L-.1055613458*a-.0638541728*b)**3,s=(L-.0894841775*a-1.2914855480*b)**3;
   const encode=value=>Math.max(0,Math.min(255,Math.round((value<=.0031308?12.92*value:1.055*value**(1/2.4)-.055)*255)));
   channels=[encode(4.0767416621*l-3.3077115913*m+.2309699292*s),encode(-1.2684380046*l+2.6097574011*m-.3413193965*s),encode(-.0041960863*l-.7034186147*m+1.7076147010*s),alpha];
  }
  assert(channels,`${context}: unsupported source color ${target}`);
  assert.equal(actual.color.slice(3).toLowerCase(),channels.slice(0,3).map(value=>value.toString(16).padStart(2,'0')).join(''));
  assert(Math.abs(parseInt(actual.color.slice(1,3),16)/255*actual.opacity-(channels[3]??1))<.001,`${context}: source alpha differs`);
 };
 for(const [index,test]of expected.entries()){
  const actual=report.reports[index];context=`${test.id}/${test.scheme}`;assert.equal(actual.id,test.id);assert.equal(actual.scheme,test.scheme);assert.equal(actual.width,width);
  box(actual.root,test.normal.root);if(test.normal.group)box(actual.group,test.normal.group);else if(test.props.group)assert.equal(actual.group.height,0);else assert.equal(actual.group,null);
  assert.equal(actual.chips.length,test.normal.chips.length);
  for(const [chipIndex,target]of test.normal.chips.entries()){
   const chip=actual.chips[chipIndex];context=`${test.id}/${test.scheme}/chip${chipIndex}`;box(chip.box,target.box);assert.equal(chip.texts.length,target.texts.length);paint(chip.background,target.paint.background);paint(chip.border,target.paint.border);
   assert.deepEqual(Object.values(chip.Padding),[10,4,10,4]);assert(Object.values(chip.CornerRadius).every(value=>value===6));
   for(const [textIndex,text]of target.texts.entries()){
    const measured=chip.texts[textIndex];box(measured.box,text.box);paint(measured.foreground,text.paint.color);assert.equal(measured.text,text.text.replace(/[ \t\r\n\f]+/g,' ').trim());
    if(text.baseline!==null){maximumBaselineError=Math.max(maximumBaselineError,Math.abs(measured.baseline-text.baseline));near(measured.baseline,text.baseline);baselines++;}
   }
   if(target.remove){box(chip.remove.box,target.remove.box);assert.equal(chip.remove.name,target.remove.name);assert.equal(chip.remove.IsEnabled,true);}else assert.equal(chip.remove,null);
  }
  if(test.normal.clear){box(actual.clear.box,test.normal.clear.box);assert.equal(actual.clear.name,'Clear all');assert.equal(actual.clear.IsEnabled,true);}else assert.equal(actual.clear,null);
  const image=readRgbaPng(await fs.readFile(path.join(native,test.file)));assert.equal(image.width,width);assert.equal(image.height,480);
 }
 assert.equal(report.inputs.length,2);
 for(const input of report.inputs){assert.equal(input.removed,4);assert.equal(input.cleared,1);assert.equal(input.executions,1);for(const flag of ['consumerOwnership','liveUpdates','canExecute','inheritedDisabled','reorderIdentity','reorderFocus','livePalette','templateReplacement','unloadRemount','emptyGroup','synchronousRemoval'])assert.equal(input[flag],true);}
 const desktop=await read(path.join(native,'filter-chip-desktop.json'));
 for(const move of desktop.pointerMoves){assert.notEqual(move.deliveryFailed,true);assert.equal(move.positioned,true);assert(move.stationary||move.delivered);}
 for(const gate of desktop.pointerPresentations){assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);}
 for(const key of desktop.keyActions){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 for(const click of desktop.pointerClicks){assert.equal(click.delivered,true);assert(click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));}
 assert.equal(desktop.keyActions.length,4);assert.equal(desktop.pointerClicks.length,6);
 return{passed:true,width,cases:expected.length,baselines,maximumGeometryError,maximumBaselineError,geometryTolerance:.05,ownedKeyPairs:desktop.keyActions.length,physicalClicks:desktop.pointerClicks.length,sourcePixelParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1],`Pass ${name}.`);return process.argv[index+1];};
 const report=await auditFilterChip(path.resolve(option('--native')),Number(option('--width')));await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
