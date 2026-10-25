import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {verifyTableLabelRaster} from './verify-table-label-raster.mjs';
const [folder,output]=process.argv.slice(2);assert(folder&&output);
const original=JSON.parse(await fs.readFile(path.join(folder,'table-label-raster.json')));assert.equal(verifyTableLabelRaster(original).passed,true,'Start from a passing native boundary report');
const cases=[
 ['wrong-format',n=>n.format='orchard-table-template-native-v1'],['foreground',n=>n.focus=1],['missing-state',n=>n.labelRaster.frames.pop()],
 ['duplicate-state',n=>n.labelRaster.frames[1]=n.labelRaster.frames[0]],['missing-png',n=>n.labelRaster.pngs.pop()],
 ['outside-frame',n=>n.labelRaster.frames[0].file='../outside.png'],['replaced-original',n=>n.labelRaster.originalFramesReplaced=true],
 ['image-mask',n=>n.labelRaster.imageMasksApplied=true],['normal-timing-claim',n=>n.labelRaster.normalTimingRun=true],
 ['wrong-text',n=>n.labelRaster.frames[0].before.text='Other'],['wrong-theme',n=>n.labelRaster.frames[0].after.theme='Other'],
 ['wrong-font',n=>n.labelRaster.frames[0].before.fontFamily='Other'],['wrong-weight',n=>n.labelRaster.frames[0].before.weight=400],
 ['wrong-palette',n=>n.labelRaster.frames[0].after.foreground.color='#ff00ff00'],['nonfinite-position',n=>n.labelRaster.frames[0].before.label.x=NaN],
 ['wrong-token',n=>{for(const key of ['foreground','buttonForeground','palette'])n.labelRaster.frames[0].before[key].color='#FF00FF00';}],
 ['missing-ancestry',n=>n.labelRaster.frames[0].after.ancestry=[]],['invalid-scale',n=>n.labelRaster.frames[0].before.rasterScale=0],
 ['changed-direction',n=>n.labelRaster.frames[0].before.heading.direction='Other']
];
if(original.format==='orchard-table-label-raster-native-v2')cases.push(['missing-margin',n=>delete n.labelRaster.frames[0].before.label.marginLeft]);
for(const [name,change]of cases){const n=structuredClone(original);change(n);assert.throws(()=>verifyTableLabelRaster(n),undefined,name);}
const rejected=cases.map(([name])=>name);
if(original.format==='orchard-table-label-raster-native-v2'){
 const n=structuredClone(original),s=n.labelRaster.frames[0].before.label;
 [s.marginLeft,s.marginRight]=[s.marginRight,s.marginLeft];
 assert.equal(verifyTableLabelRaster(n).passed,false,'A finite tracking inset on the wrong side must fail');rejected.push('reversed-tracking-margin');
}
const result={passed:true,corruptReportsRejected:rejected.length,cases:rejected,diagnosticOnly:true,nativeComponentAccepted:false};
await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
