import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),output=option('--output'),live=process.argv.includes('--live'),hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL(`../tests/Reference/DataTableFilters${live?'Live':''}.json`,import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
const baseline=await read(new URL('../tests/Reference/DataTablePagination.json',import.meta.url));
for(const key of ['sourceCommit','compiledCssSha256','browser'])assert.equal(reference[key],baseline[key]);assert.deepEqual(reference.toolVersions,baseline.toolVersions);assert.deepEqual(reference.fontFiles,baseline.fontFiles);
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),referenceSha256);
assert.equal(reference.format,live?'orchard-data-table-filters-live-v1':'orchard-data-table-filters-v1');assert.equal(reference.sourceCommit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.sourceLockGitBlob,'0b6113dd13d74b1023208cbf96e080915f3b47a2');
for(const[field,file]of[['scriptSha256',live?'capture-data-table-filters-live.cjs':'capture-data-table-filters.cjs'],['clientSha256','data-table-filters-client.tsx']])assert.equal(reference[field],hash(await fs.readFile(new URL('./reference/'+file,import.meta.url))));
for(const item of reference.dependencies)assert.equal(hash(await fs.readFile(path.join('artifacts/line-chart-browser/frozen-source',item.file))),item.sha256);
for(const item of reference.dependencies)if(item.file==='src/components/data-table-filters.tsx')assert.equal(item.sha256,reference.sourceSha256);else assert.equal(item.sha256,baseline.dependencies.find(prior=>prior.file===item.file)?.sha256);
for(const item of reference.fontFiles)assert.equal(hash(await fs.readFile(path.join('src/Orchard/Assets/Fonts',item.file))),item.sha256);
const lockBytes=await fs.readFile('artifacts/chart-theme-reference/bleecker-package-lock.json'),lock=JSON.parse(lockBytes);assert.equal(crypto.createHash('sha1').update(Buffer.concat([Buffer.from('blob '+lockBytes.length+'\0'),lockBytes])).digest('hex'),reference.sourceLockGitBlob);
for(const item of reference.bundledRuntime)assert.ok(Object.entries(lock.packages).some(([key,value])=>(key==='node_modules/'+item.name||key.endsWith('/node_modules/'+item.name))&&value.version===item.version&&value.integrity===item.integrity));
let states=0,openStates=0,physicalEvents=0;const images=[],keyReceipts=[],cases=[];
for(const sample of reference.cases){
 let previous=[],previousState=null;const snapshots=[],props={...sample.props};
 for(const point of sample.snapshots){
  if(sample.updates?.[point.phase])Object.assign(props,sample.updates[point.phase]);
  const state=point.state,table=state.table;assert.equal(table.sameTable,true);assert.equal(table.sameRows,true);assert.ok(table.columns.every(item=>item.same));assert.ok(table.rows.every(item=>item.same));assert.ok(state.inputEvents.every(item=>item.trusted));assert.deepEqual(state.inputEvents.slice(0,previous.length),previous);physicalEvents+=state.inputEvents.length-previous.length;previous=state.inputEvents;
  if(sample.updates?.[point.phase]){assert.deepEqual(table,previousState.table);assert.deepEqual(state.events,previousState.events);assert.equal(state.visibleRows,previousState.visibleRows);assert.ok(point.focusIdentity);}
  assert.ok(point.presentation.stable>=3&&point.presentation.milliseconds<2000);
  const data=await fs.readFile(path.join(source,point.file));assert.equal(hash(data),point.sha256);const png=readRgbaPng(data);assert.deepEqual([png.width,png.height],[sample.viewportWidth,745]);
  if(table.columnPresent){assert.ok(state.trigger);assert.equal(state.trigger.box.height,40);assert.equal(state.trigger.box.width,sample.viewportWidth>=640?176:sample.hostWidth);}else{assert.equal(state.trigger,null);assert.equal(state.popupOpen,false);}
  if(state.popupOpen){assert.ok(point.accessibility.startsWith('- listbox:'));assert.equal(state.popup.role,'listbox');assert.equal(state.popup.box.width,state.trigger.box.width);assert.equal(state.options.length,1+(props.options?.length??2));openStates++;}else{assert.equal(state.popup,null);assert.equal(point.accessibility,null);}
  const key={down:'ArrowDown',home:'Home',end:'End',enter:'Enter',escape:'Escape',tab:'Tab','reverse-tab':'Tab','type-p':'p','type-d':'d',focus:'Tab'}[point.phase];
  if(key){const events=state.inputEvents.filter(event=>event.key===key&&['keydown','keyup'].includes(event.type)).slice(-2);assert.deepEqual(events.map(item=>item.type),['keydown','keyup']);keyReceipts.push({id:sample.id,scheme:sample.scheme,phase:point.phase,events,settledFocus:state.focused});}
  images.push({file:point.file,sha256:point.sha256,width:png.width,height:png.height});snapshots.push({phase:point.phase,focus:state.focused,trigger:state.trigger?.text,placeholder:state.trigger?.placeholder,callbacks:state.events.length,outsideClicks:state.outsideClicks,rows:state.visibleRows});previousState=state;states++;
 }
 cases.push({id:sample.id,scheme:sample.scheme,states:snapshots});
}
assert.equal(reference.cases.length,live?32:56);assert.equal(states,live?140:276);assert.equal((await fs.readdir(source)).filter(name=>name.endsWith('.png')).length,live?140:276);
for(const scheme of ['Light','Dark']){
 const sample=id=>reference.cases.find(item=>item.id===id&&item.scheme===scheme);
 const state=(id,phase)=>sample(id).snapshots.find(item=>item.phase===phase).state;
 if(live){
  for(const id of ['remove-focused','disable-focused','change-focused-value']){const point=sample(id).snapshots.find(item=>sample(id).updates[item.phase]);assert.equal(point.state.focused,'listbox');assert.equal(point.focusIdentity.retained,false);}
  for(const id of ['relabel-selected','relabel-highlighted','reorder-selected','reorder-highlighted','placeholder-open','disable-unfocused','enable-unfocused','remove-unfocused','empty-options','long-reorder','sentinel-relabel','duplicate-empty-relabel','duplicate-empty-reorder']){const point=sample(id).snapshots.find(item=>sample(id).updates[item.phase]);assert.equal(point.focusIdentity.retained,true);assert.equal(point.focusIdentity.previousConnected,true);}
  assert.equal(state('long-reorder','reorder').viewport.scrollTop,400);
  continue;
 }
 assert.equal(state('default','same-all').events.length,2);assert.equal(state('default','outside').outsideClicks,0);assert.equal(state('default','outside').popupOpen,false);
 for(const id of ['numeric-zero','boolean-false'])assert.equal(state(id,'closed').trigger.placeholder,true);
 for(const id of ['numeric-value','array-value']){assert.equal(state(id,'closed').trigger.placeholder,false);assert.equal(state(id,'closed').trigger.text,'');}
 assert.equal(state('duplicate-empty','closed').trigger.text,'AllExplicit empty');assert.equal(state('duplicate-empty','open').options.filter(item=>item.state==='checked').length,2);
 assert.equal(state('sentinel-collision','closed').trigger.text,'AllLiteral sentinel');assert.equal(state('sentinel-collision','open').options.filter(item=>item.state==='checked').length,2);
 assert.equal(state('selected','tab').focused,'option:1');assert.equal(state('selected','reverse-tab').focused,'option:1');
}
const report={format:'orchard-data-table-filters-reference-audit-v1',referenceSha256,verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),sourceGatesPassed:true,nativeComponentAccepted:false,nativeRuntimeVerified:false,cases:reference.cases.length,states,openStates,physicalEvents,keys:keyReceipts.length,captures:images.length,sourceDependencies:reference.dependencies.length,bundledRuntime:reference.bundledRuntime.length,keyReceipts,images,consumerStates:cases};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,keyReceipts:undefined,images:undefined,consumerStates:undefined}));
