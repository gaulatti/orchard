import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyTableCustomization(n){
 assert.equal(n.format,'orchard-table-customization-native-v1');assert.equal(n.passed,true);assert.equal(n.publicApisOnly,true);assert.equal(n.snapshots.length,10);assert.equal(n.pngs.length,10);assert(n.guards>=100);assert.equal(n.focus,0);assert.equal(n.activations,0);
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 let cells=0;
 for(const scheme of ['Light','Dark'])for(const stage of ['style','binding','notified','template-restored','style-restored']){
  const matches=n.snapshots.filter(s=>s.scheme===scheme&&s.stage===stage);assert.equal(matches.length,1);const s=matches[0];assert.equal(s.cells.length,7);
  const initial=stage==='style'||stage==='style-restored',bound=stage==='binding';const want={fontStyle:initial?'Italic':bound?'Oblique':'Normal',fontStretch:bound?'Condensed':'Expanded',spacing:initial?120:bound?220:310,fontSize:initial?18:bound?22:26,lineHeight:initial?28:bound?32:38};
  for(const kind of ['plain','heading','sortable','nonsortable','caption','rich','templated']){
   const list=s.cells.filter(c=>c.kind===kind);assert.equal(list.length,1);const c=list[0];assert.equal(c.originalCellRetained,true);assert.equal(c.originalRichContentRetained,true);assert.equal(c.bindingExpressionsRetained,true);assert.equal(c.bindings,initial?0:7);
   for(const key of ['fontStyle','fontStretch','spacing','fontSize','lineHeight'])assert.equal(c[key],want[key],kind+': owner '+key);
   assert.equal(c.renderedFontStyle,want.fontStyle,kind+': displayed FontStyle');assert.equal(c.renderedFontStretch,want.fontStretch,kind+': displayed FontStretch');assert.equal(c.renderedSpacing,want.spacing,kind+': displayed CharacterSpacing');assert.equal(c.renderedFontSize,want.fontSize,kind+': displayed FontSize');
   if(kind!=='rich'&&kind!=='templated')assert(Math.abs(c.renderedLineHeight-want.lineHeight)<.016,kind+': displayed LineHeight');
   assert.equal(c.borderExists,true,kind+': border missing');assert.deepEqual(c.expectedBorderThickness,initial?[3,2,4,5]:bound?[5,4,6,7]:[7,6,8,9]);assert.deepEqual(c.borderThickness,c.expectedBorderThickness,kind+': displayed BorderThickness');assert.deepEqual(c.padding,initial?[7,5,11,9]:bound?[9,7,13,11]:[11,9,15,13]);assert.equal(c.cornerRadius,9);assert.equal(c.expectedCornerRadius,9);
   assert(Number.isFinite(c.minimumWidth)&&c.minimumWidth>150);assert(Number.isFinite(c.cellWidth)&&c.cellWidth>=c.minimumWidth-.05,kind+': intrinsic width clips customized text/border');assert(c.cellHeight>0&&c.scrollableWidth>0);assert(c.paint.x>=0&&c.paint.x<1000&&c.paint.y>=0&&c.paint.y<760);assert.equal(c.paint.rgb.length,3);cells++;
  }
  assert.equal(s.file,`table-customization-${scheme}-${stage}.png`);assert.equal(n.pngs.filter(p=>p.file===s.file).length,1);
 }
 for(const p of n.pngs){assert.equal(p.width,1000);assert.equal(p.height,760);assert.match(p.sha256,/^[0-9a-f]{64}$/);}for(const key of ['librarySha256','hostSha256'])assert.match(n[key],/^[0-9a-f]{64}$/);
 return{passed:true,snapshots:10,cellPresentations:cells,bindingsRetained:true,originalContentRetained:true,liveNotificationsVerified:true,templateRestorationVerified:true,styleRestorationVerified:true,guards:n.guards,librarySha256:n.librarySha256,hostSha256:n.hostSha256,physicalInputVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export async function auditTableCustomization(o){const n=JSON.parse(await fs.readFile(path.join(o.native,'table-customization.json'))),r=verifyTableCustomization(n),exit=JSON.parse(await fs.readFile(path.join(o.native,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);for(const[file,sha]of [['Orchard.dll',n.librarySha256],[o['host-assembly']??'Orchard.Catalog.dll',n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(o.native,'app-identity',file))),sha);let pixels=0,paint=0;for(const p of n.pngs){const bytes=await fs.readFile(path.join(o.native,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,p.width);assert.equal(png.height,p.height);pixels+=png.width*png.height;const s=n.snapshots.find(s=>s.file===p.file);for(const c of s.cells){const i=(c.paint.y*png.width+c.paint.x)*4;for(let channel=0;channel<3;channel++)assert(Math.abs(png.rgba[i+channel]-c.paint.rgb[channel])<=1,c.kind+': actual border pixel differs');assert.equal(png.rgba[i+3],255);paint++;}}return{...r,rawPixelsValidated:pixels,borderPaintSamples:paint};}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){const o=Object.fromEntries(process.argv.slice(2).reduce((r,v,i,a)=>i%2===0?[...r,[v.replace(/^--/,''),a[i+1]]]:r,[]));const r=await auditTableCustomization(o);await fs.writeFile(o.output,JSON.stringify(r,null,2)+'\n');console.log(JSON.stringify(r));}
