import fs from'node:fs/promises';import path from'node:path';import crypto from'node:crypto';import assert from'node:assert/strict';import{fileURLToPath}from'node:url';import{readRgbaPng}from'./read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyTableTextSource(r){
 assert.equal(r.format,'orchard-table-text-source-v1');assert.equal(r.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(r.osInputSent,false);assert.equal(r.cases.length,18);assert.equal(r.histories.length,6);assert.equal(r.captures.length,18);assert.equal(r.cssSourceFiles.length,122);
 const expected={expansions:'STRASSE FFI Ϊ́ ԵՒ CAFÉ 東京 👩🏽‍💻',turkish:'İYİ IŞIK İ̇',azerbaijani:'İLHAM I İ',lithuanian:'Í J̀ Į',greek:'Ή ΗΧΟΣ ΑΫΛΟΣ ΜΑΪΟΥ ΠΑΪΣΙΟΣ ΑΙ','greek-decomposed':'Α Ϋ Ή',dutch:'IJSSEL FFI STRASSE',supplementary:'𐐀 𞤀 Ᲊ',empty:''};let snapshots=0;
 for(const scheme of ['Light','Dark']){
  for(const[name,display]of Object.entries(expected)){const found=r.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(found.length,1);const c=found[0];assert.equal(c.snapshot.language,c.spec.language);assert.equal(c.snapshot.header.length,3);for(const h of c.snapshot.header){assert.equal(h.raw,c.spec.label);assert.equal(h.display,display);assert(h.box.width>0&&h.box.height>=44);}}
  for(const name of ['ordinal-keys','dictionary-key-case','live-language']){const found=r.histories.filter(h=>h.scheme===scheme&&h.name===name);assert.equal(found.length,1);const h=found[0];assert.equal(h.retainedReactInstance,true);snapshots+=h.snapshots.length;
   if(name==='ordinal-keys'){assert.equal(h.snapshots.length,5);assert.deepEqual(h.snapshots.map(s=>s.checkboxes.filter(c=>c.checked).map(c=>c.name)),[['Select row i','Select row é'],['Select row i','Select row é'],['Select row cedar','Select row i','Select row é'],['Select all','Select row cedar','Select row Cedar','Select row i','Select row ı','Select row İ','Select row é','Select row é'],[]]);assert.deepEqual(h.snapshots[1].events,[{kind:'selection',keys:['CEDAR','i','é','cedar']}]);assert.deepEqual(h.snapshots[3].events[1].keys,['cedar','Cedar','i','ı','İ','é','é']);assert.deepEqual(h.snapshots[4].events.at(-1),{kind:'selection',keys:[]});}
   if(name==='dictionary-key-case'){assert.equal(h.snapshots.length,1);assert.deepEqual(h.snapshots[0].body.map(row=>row.cells),[['','','1'],['','Lower original','2']]);}
   if(name==='live-language'){assert.equal(h.snapshots.length,3);assert.deepEqual(h.snapshots.map(s=>s.header.map(h=>h.display)),[['IYI STRASSE FFI','IYI STRASSE FFI','IYI STRASSE FFI'],['İYİ STRASSE FFI','İYİ STRASSE FFI','İYİ STRASSE FFI'],['IYI STRASSE FFI','IYI STRASSE FFI','IYI STRASSE FFI']]);}
  }
 }
 assert.equal(snapshots,18);return{passed:true,cases:18,histories:6,snapshots:18,captures:18,unicodeLabelsAndLanguageChangesVerified:true,ordinalKeysAndPropertyNamesVerified:true};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};const r=JSON.parse(await fs.readFile(arg('--reference'))),result=verifyTableTextSource(r);let pixels=0;for(const p of r.captures){const bytes=await fs.readFile(path.join(arg('--frames'),p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,900);assert.equal(png.height,760);pixels+=png.width*png.height;}result.rawPixelsValidated=pixels;await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
}
