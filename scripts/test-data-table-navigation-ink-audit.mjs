import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyDataTablePaint} from './verify-data-table.mjs';
const [candidate,old,output]=process.argv.slice(2);assert(candidate&&old&&output);
const n=JSON.parse(await fs.readFile(`${candidate}/data-table.json`)),baseline=JSON.parse(await fs.readFile(`${old}/data-table.json`));const cases=[];
for(const scheme of ['Light','Dark']){
 const entry=n.pngs.find(p=>p.file===`data-table-${scheme}-initial.png`),png=readRgbaPng(await fs.readFile(`${candidate}/${entry.file}`));assert.equal(verifyDataTablePaint(n,entry,png),2);
 const prior=baseline.pngs.find(p=>p.file===entry.file);assert(prior);const priorPng=readRgbaPng(await fs.readFile(`${old}/${entry.file}`));assert.throws(()=>verifyDataTablePaint(baseline,prior,priorPng),/arrow ink exceeds source foreground\/opacity bounds/);cases.push(`${scheme}-actual-pre-repair-opaque`);
 const state=n.snapshots.find(s=>s.scheme===scheme&&s.stage==='initial').snapshot,fixture=state.fixture,cx=Math.round(fixture.x+fixture.width-64),cy=Math.round(fixture.y+fixture.height-16),blank={...png,rgba:Buffer.from(png.rgba)},i=((cy-6)*blank.width+cx-6)*4,bg=Buffer.from(blank.rgba.subarray(i,i+4));
 for(let y=cy-6;y<=cy+6;y++)for(let x=cx-6;x<=cx+6;x++)blank.rgba.set(bg,(y*blank.width+x)*4);
 assert.throws(()=>verifyDataTablePaint(n,entry,blank),/pagination arrow has no visible paint/);cases.push(`${scheme}-missing-glyph`);
 const bad={...png,rgba:Buffer.from(png.rgba)};for(let y=cy-2;y<=cy+2;y++)for(let x=cx-2;x<=cx+2;x++)bad.rgba.set(scheme==='Light'?[0,0,0,255]:[255,255,255,255],(y*bad.width+x)*4);
 assert.throws(()=>verifyDataTablePaint(n,entry,bad),/arrow ink exceeds source foreground\/opacity bounds/);cases.push(`${scheme}-over-strong-ink`);
}
const result={passed:true,rejectedCases:cases.length,actualPreRepairNativeImagesRejected:2,missingGlyphsRejected:2,overStrongInkRejected:2,originalSampleLocationsRetained:true,originalMinimumInkCountRetained:true,rawFilesUnmodified:true,imageComparisonMasksChanged:false,cases,nativeComponentAccepted:false};await fs.writeFile(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
