import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditTextareaCatalog(native){
 const result=await read(path.join(native,'result.json'));assert.equal(result.passed,true);assert([undefined,'textarea-catalog'].includes(result.component));
 assert.equal(result.environment.processArchitecture,'X64');assert.equal(result.environment.osArchitecture,'X64');
 const source=await read(new URL('../tests/Reference/Textarea.json',import.meta.url));
 const recipes=[['Small',1,'small-rows-one'],['Small',4,'small'],['Large',10,'large-rows-ten'],['Medium',10,'rows-ten'],['Medium',4,'medium']];
 for(const scheme of['Light','Dark']){
  const report=await read(path.join(native,`textarea-catalog-${scheme}.json`));assert.equal(report.passed,true);assert.equal(report.restored,true);assert.equal(report.phases.length,10);
  for(const[index,[size,rows,id]]of recipes.entries()){
   const frame=report.phases[index],expected=source.cases.find(c=>c.id===id&&c.width===640&&c.scheme===scheme&&c.state==='normal');assert(expected);
   assert.equal(frame.phase,`geometry-${size}-${rows}`);assert.equal(frame.size,size);assert.equal(frame.rows,rows);assert.equal(frame.height,expected.box.height);assert.equal(frame.lineHeight,parseFloat(expected.paint.lineHeight));assert.equal(frame.nativeLineSpacing,frame.lineHeight*.75);
  }
  const edited=report.phases[5];assert.equal(edited.phase,'native-edit');assert.equal(edited.text,'Bound notes\nSecond line\na');assert.equal(edited.consumer,edited.text);
  const readonly=report.phases[6];assert.equal(readonly.phase,'readonly-consumer');assert.equal(readonly.text,'Read-only live update\n');assert.equal(readonly.nativeReadOnly,true);
  for(const[index,theme]of['Dark','Light'].entries()){
   const frame=report.phases[7+index],expected=source.cases.find(c=>c.id==='medium'&&c.width===640&&c.scheme===theme&&c.state==='normal');
   assert.equal(frame.phase,'live-theme-'+theme);assert.equal(frame.actualTheme,theme);assertNativePaint({color:frame.border,opacity:frame.borderOpacity},expected.paint.borderColor,theme+'/live-border');assertNativePaint({color:frame.background,opacity:frame.backgroundOpacity},expected.paint.backgroundColor,theme+'/live-background');
  }
  const validity=report.phases[9];assert.equal(validity.phase,'validity-override');assert.equal(validity.explicitFalseValid,true);assert.equal(validity.clearedErrorInvalid,true);
  const error=source.cases.find(c=>c.id==='error'&&c.width===640&&c.scheme==='Light'&&c.state==='normal');assertNativePaint({color:validity.border,opacity:1},error.paint.borderColor,'live-error');
  const desktop=await read(path.join(native,`textarea-catalog-${scheme}-desktop.json`));assert.equal(desktop.keyActions.length,3);assert.deepEqual(desktop.keyActions.map(k=>k.key),[13,65,66]);
  for(const key of desktop.keyActions){assert.equal(key.target,'LiveNotes');assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
  const png=readRgbaPng(await fs.readFile(path.join(native,`textareas-${scheme}.png`)));assert(png.width>0&&png.height>0);
 }
 return{passed:true,livePhases:20,nativeKeyActions:6,sourceGeometryAndPaintVerified:true,catalogIntegrationVerified:result.component===undefined,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const report=await auditTextareaCatalog(path.resolve(option('--native')));await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
