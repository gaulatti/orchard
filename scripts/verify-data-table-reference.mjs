import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyDataTableNavigationInk} from './verify-data-table-navigation-ink.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
export function verifyDataTableReference(n,reference){
 assert.equal(n.format,'orchard-data-table-reference-native-v1');assert.equal(n.passed,true);assert.equal(n.cases.length,42);assert.equal(n.histories.length,34);assert.equal(n.pngs.length,208);assert(n.guards>=2000);assert.equal(n.focus,0);assert.equal(n.activations,0);assert.equal(n.publicApisOnly,true);assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');
 const expectedFrames=[...reference.cases.map(c=>`data-table-reference-${c.scheme}-${c.name}.png`),...reference.histories.flatMap(h=>h.snapshots.map(s=>`data-table-reference-${h.scheme}-history-${h.action}-${s.stage}.png`))];assert.equal(new Set(n.pngs.map(p=>p.file)).size,208);assert.deepEqual(n.pngs.map(p=>p.file).sort(),expectedFrames.sort());
 for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','completeSourceGeometryVerified','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(n[key],false,key);
 let snapshots=0,geometryValues=0,maximum=0,renderedCells=0;
 function close(a,b,label){assert(Number.isFinite(a)&&Number.isFinite(b),label);const difference=Math.abs(a-b);geometryValues++;maximum=Math.max(maximum,difference);assert(difference<=1,label+`: ${a} versus ${b}`);}
 function box(a,b,keys,label){for(const key of ['x','y','width','height'])assert(Number.isFinite(a[key]),label+key);assert(a.width>=0&&a.height>=0,label);for(const key of keys)close(a[key],b[key],label+'/'+key);}
 function compare(s,r,label,spec){
  snapshots++;assert.equal(s.originalNativeContentVerified,true,label);
  for(const key of ['state','pageCount','canPrevious','canNext','filteredRows','filteredSelectedRows','sortedRows','visibleRows','headerGroups','events','originalActions'])assert.deepEqual(s[key],r[key],label+'/'+key);
  box(s.fixture,r.fixture,['x','y','width','height'],label+'/fixture');box(s.table,{...r.table,width:r.scrollClientWidth},['x','y','width','height'],label+'/table viewport');
  close(s.scrollWidth,r.scrollWidth,label+'/scroll width');
  const headers=r.headerGroups.flatMap(g=>g.headers),columns=r.visibleRows[0]?.values.map(v=>v.column)??[];
  for(const key of ['header','body']){assert.equal(s[key].length,r[key].length,label+'/'+key+' count');s[key].forEach((c,i)=>{
   const expected=r[key][i],name=label+'/'+key+'/'+i;assert.equal(c.text,expected.text,name+' original text');assert.equal(c.colSpan,expected.colSpan,name+' rendered span');box(c.box,expected.box,['x','y','width','height'],name);
   let kind='text';if(key==='header'){const h=headers[i];if(!h.placeholder)kind=h.column==='select'?'checkbox':!spec.plainHeaders&&['name','profile_name','count','flag'].includes(h.column)?'button':'text';}else{const column=columns[i%columns.length];kind=column==='select'?'checkbox':column==='status'?'button':'text';}
   assert.equal(c.renderKind,kind,name+' native producer kind');
   if(kind!=='checkbox')assert.equal(c.renderedText,kind==='button'?expected.text:expected.styles.textTransform==='uppercase'?expected.text.toLocaleUpperCase('en-US'):expected.text,name+' actual displayed text');
   if(kind==='button'){
    const b=c.buttonPresentation;assert(b,name+' original native button');for(const k of ['wrapperSpacing','actionSpacing','presenterSpacing'])assert.equal(b[k],0,name+'/'+k);assert.equal(b.actionFontSize,14);assert.equal(b.actionFontWeight,500);assert.match(b.actionFontFamily,/EncodeSans-Medium\.ttf#Encode Sans$/);
    assert.equal(b.text.length,expected.text.length?1:0);for(const t of b.text){assert.equal(t.Text,expected.text);assert.equal(t.CharacterSpacing,0);assert.equal(t.FontSize,14);assert.equal(t.fontWeight,500);}
   }else assert.equal(c.buttonPresentation,null,name+' unexpected button');
   renderedCells++;
  });}
  assert.equal(s.rows.length,r.rows.length,label+'/rows');s.rows.forEach((row,i)=>{assert.equal(row.state,r.rows[i].state==='selected'?'true':r.rows[i].state,label+'/context selection metadata');assert.equal(row.selected,r.rows[i].selected,label+'/primitive selection attr');assert.equal(row.primitiveSelected,false,label+'/ordinary row paint');box(row.box,r.rows[i].box,['x','y','width','height'],label+'/row/'+i);});
  assert.equal(s.search.length,r.search.length,label+'/search');s.search.forEach((search,i)=>{assert.equal(search.value,r.search[i].value,label+'/controlled input');assert.equal(search.placeholder,r.search[i].placeholder,label+'/placeholder');box(search.box,r.search[i].box,['x','y','width','height'],label+'/search box');});
  assert.equal(s.checkboxes.length,r.checkboxes.length,label+'/checkboxes');s.checkboxes.forEach((c,i)=>{for(const key of ['name','checked','indeterminate'])assert.equal(c[key],r.checkboxes[i][key],label+'/checkbox/'+i+'/'+key);box(c.box,r.checkboxes[i].box,['x','y','width','height'],label+'/checkbox box/'+i);});
  assert.equal(s.footer.text,r.footer.text,label+'/selection summary');assert.equal(s.footer.buttons.length,2);s.footer.buttons.forEach((b,i)=>{assert.equal(b.disabled,r.footer.buttons[i].disabled,label+'/page limit');box(b.box,r.footer.buttons[i].box,['x','y','width','height'],label+'/navigation/'+i);});
 }
 assert.deepEqual(n.cases.map(c=>[c.scheme,c.name]),reference.cases.map(c=>[c.scheme,c.name]));for(const c of n.cases){const expected=reference.cases.find(r=>r.scheme===c.scheme&&r.name===c.name);compare(c.snapshot,expected.snapshot,c.scheme+'/'+c.name,expected.spec);}
 assert.deepEqual(n.histories.map(h=>[h.scheme,h.action]),reference.histories.map(h=>[h.scheme,h.action]));let histories=0;for(const h of n.histories){const expected=reference.histories.find(r=>r.scheme===h.scheme&&r.action===h.action);assert.deepEqual(h.snapshots.map(s=>s.stage),expected.snapshots.map(s=>s.stage),h.action+' stages');for(let i=0;i<h.snapshots.length;i++)compare(h.snapshots[i].snapshot,expected.snapshots[i],h.scheme+'/'+h.action+'/'+h.snapshots[i].stage,expected.spec);histories+=h.snapshots.length;}assert.equal(histories,166);assert.equal(snapshots,208);
 return{passed:true,staticStates:42,histories:34,historySnapshots:166,totalNativeSnapshots:208,renderedCells,geometryValues,maximumGeometryDifferenceDip:maximum,geometryToleranceDip:1,originalNativeContentVerified:true,sourceStateAndRenderedTextVerified:true,actualDisplayedTextVerified:true,sourceVerticalAndViewportGeometryVerified:true,sourceHorizontalColumnGeometryVerified:true,buttonTypographyVerified:true,completeSourceGeometryVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
export function verifyDataTableReferencePaint(n,entry,png){
 let c=n.cases.find(c=>entry.file===`data-table-reference-${c.scheme}-${c.name}.png`);
 if(!c)for(const h of n.histories){const s=h.snapshots.find(s=>entry.file===`data-table-reference-${h.scheme}-history-${h.action}-${s.stage}.png`);if(s){c={scheme:h.scheme,name:h.action+'/'+s.stage,snapshot:s.snapshot};break;}}assert(c,'Unknown native frame');let count=0;
 return verifyDataTableNavigationInk({scheme:c.scheme,stage:c.name,buttons:c.snapshot.footer.buttons.map((b,i)=>({name:i===0?'previous':'next',cx:Math.round(b.box.x+b.box.width/2),cy:Math.round(b.box.y+b.box.height/2),enabled:!b.disabled})),image:png});
}
async function main(){
 const option=k=>{const i=process.argv.indexOf(k);assert(i>=0&&process.argv[i+1],k);return process.argv[i+1];};const folder=option('--native'),bytes=await fs.readFile(path.join(folder,'data-table-reference.json')),n=JSON.parse(bytes),sourceBytes=await fs.readFile('tests/Reference/DataTable.json');assert.deepEqual(await fs.readFile(path.join(folder,'source-reference.json')),sourceBytes);const audit=verifyDataTableReference(n,JSON.parse(sourceBytes));
 assert.equal(hash(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))),n.librarySha256);assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',option('--host-assembly')))),n.hostSha256);
 let pixels=0,arrowPaintProbes=0;for(const p of n.pngs){const bytes=await fs.readFile(path.join(folder,p.file));assert.equal(hash(bytes),p.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,900);assert.equal(png.height,1000);assert.equal(p.width,900);assert.equal(p.height,1000);pixels+=png.width*png.height;arrowPaintProbes+=verifyDataTableReferencePaint(n,p,png);}
 const exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);Object.assign(audit,{rawPixelsValidated:pixels,arrowPaintProbes,librarySha256:n.librarySha256,hostSha256:n.hostSha256,referenceSha256:hash(sourceBytes),reportSha256:hash(bytes)});await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))await main();
