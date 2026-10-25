import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {verifyDataTablePaint,verifyDataTablePaintPixels} from './verify-data-table-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const native=JSON.parse(await fs.readFile(path.join(option('--native'),'data-table-reference.json')));
const reference=JSON.parse(await fs.readFile('tests/Reference/DataTable.json'));
verifyDataTablePaint(native,reference);
const scene=(n,scheme='Light')=>n.cases.find(c=>c.scheme===scheme&&c.name==='default').snapshot;
const paint=(n,scheme)=>scene(n,scheme).paint;
const layer=n=>paint(n).shadowLayers[1];
const checkbox=n=>scene(n).checkboxes[0].paint;
const mutations=[
 ['unsettled',n=>n.settledMotionEndpoints=false],['wrong-duration',n=>n.controlDurationMilliseconds=60],
 ['unclipped',n=>n.captureViewportClipped=false],['false-shadow-raster',n=>n.compositionShadowRasterVerified=true],
 ['missing-unload',n=>n.shadowUnloadedContexts--],['missing-lifecycle-theme',n=>n.shadowLifecycleContracts.pop()],
 ...['callerSurfaceBindingsRetained','unloadDetached','reloadAttached','templateDetached','callerTemplateWithoutDecorationsVerified','callerOriginalRowsRetained','defaultRestored'].map(k=>[k,n=>n.shadowLifecycleContracts[0][k]=false]),
 ['stale-corner-mask',n=>n.shadowLifecycleContracts[0].updatedCornerMaskRadii[0]=12],
 ['missing-paint',n=>delete scene(n).paint],['surface-fill',n=>paint(n).surface.fill.r=0],
 ['generic-light-border',n=>Object.assign(paint(n).surface.stroke,{r:0,g:0,b:0,a:.08})],
 ['generic-light-header',n=>paint(n).headerFill.a=.4],['dark-white-surface',n=>paint(n,'Dark').surface.fill.r=255],
 ['dark-border',n=>paint(n,'Dark').surface.stroke.a=.25],['dark-header',n=>paint(n,'Dark').headerFill.a=.5],
 ['border-thickness',n=>paint(n).surface.border[0]=2],['corner',n=>paint(n).surface.radii[0]=0],
 ['surface-geometry',n=>paint(n).surface.box.width+=1.1],['missing-layer',n=>paint(n).shadowLayers.pop()],
 ['missing-graph',n=>layer(n).attached=false],['wrong-layer',n=>layer(n).name='PART_ShadowNear'],
 ['unmasked',n=>layer(n).masked=false],['shadow-color',n=>layer(n).color[0]=0],
 ['shadow-opacity',n=>layer(n).opacity=.2],['shadow-width',n=>layer(n).width++],
 ['shadow-height',n=>layer(n).height++],['shadow-x',n=>layer(n).x++],['shadow-y',n=>layer(n).y++],
 ['glyph-motion',n=>checkbox(n).glyphOpacity=.01],['chrome-opacity',n=>checkbox(n).chromeOpacity=.99],
 ['fill-motion',n=>checkbox(n).fill.r-=2],['stroke-motion',n=>checkbox(n).stroke.r-=2],
 ['fill-alpha-motion',n=>checkbox(n).fill.a=.99],['stroke-alpha-motion',n=>checkbox(n).stroke.a=.99]
];
for(const[name,mutate]of mutations){const bad=structuredClone(native);mutate(bad);assert.throws(()=>verifyDataTablePaint(bad,reference),undefined,name);}
const counterexamples=JSON.parse(await fs.readFile('tests/Reference/DataTablePaintCounterexamples.json'));
assert.equal(counterexamples.format,'orchard-data-table-paint-counterexamples-v1');
let actualWrongPaletteCaptures=0,actualUnclippedCaptures=0;
for(const entry of counterexamples.files){
 const bytes=await fs.readFile(entry.frame);assert.equal(hash(bytes),entry.frameSha256);const png=readRgbaPng(bytes);
 if(entry.kind==='generic-light-palette'){
  assert.equal(png.width,900);assert.equal(png.height,1000);
  assert.throws(()=>verifyDataTablePaintPixels(native,{file:entry.nativeFrame},png,reference),/border paint channel/);actualWrongPaletteCaptures++;
 }else{
  assert.equal(entry.kind,'unclipped-shadow-overflow');
  assert.equal(png.width,934);assert.equal(png.height,1000);assert.throws(()=>verifyDataTablePaintPixels(native,{file:entry.nativeFrame},png,reference),/capture width/);actualUnclippedCaptures++;
 }
}
assert.equal(actualWrongPaletteCaptures,1);assert.equal(actualUnclippedCaptures,1);assert.equal(counterexamples.files.length,2);
const audit={passed:true,corruptReportsRejected:mutations.length,actualWrongPaletteCapturesRejected:actualWrongPaletteCaptures,actualUnclippedCapturesRejected:actualUnclippedCaptures,compositionShadowRasterVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
