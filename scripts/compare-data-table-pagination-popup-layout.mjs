import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const option=name=>{const index=process.argv.indexOf(name);assert.ok(index>=0&&process.argv[index+1],`Pass ${name} <path>.`);return path.resolve(process.argv[index+1]);};
const nativeDirectory=option('--native'),output=option('--output'),auditPath=option('--keyboard-audit');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTablePaginationKeyboard.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=crypto.createHash('sha256').update(bytes).digest('hex');
const audit=await read(auditPath);assert.equal(audit.checkedGatesPassed,true);assert.equal(audit.referenceSha256,referenceSha256);assert.equal(audit.states,136);
const reports=[];
for(const scheme of ['Light','Dark']){
 const actual=await read(path.join(nativeDirectory,`data-table-pagination-keyboard-${scheme}.json`));assert.equal(actual.passed,true);assert.equal(actual.referenceSha256,referenceSha256);
 const expected=reference.cases.filter(item=>item.scheme===scheme).flatMap(item=>item.snapshots.map(point=>({id:item.id,...point})));
 assert.equal(expected.length,actual.reports.length);
 for(const [index,point]of expected.entries()){
  const native=actual.reports[index];assert.equal(native.id,point.id);assert.equal(native.phase,point.phase);assert.deepEqual(native.errors,[]);
  if(!point.state.popupOpen){assert.equal(native.popup,null);continue;}
  assert.ok(native.popup&&native.chooser);const popup=point.state.popup,surface=point.state.surface,box=native.popup,chooser=native.chooser;
  const source={x:popup.x-surface.x,gap:popup.y-surface.y-surface.height,width:popup.width,height:popup.height};
  const measured={x:box.X-native.surface.X,gap:box.Y-chooser.Y-chooser.Height,width:box.Width,height:box.Height};
  const differences=[];for(const field of Object.keys(source)){assert.ok(Number.isFinite(source[field])&&Number.isFinite(measured[field]));if(Math.abs(source[field]-measured[field])>1/32)differences.push({field,source:source[field],native:measured[field]});}
  reports.push({id:point.id,scheme,phase:point.phase,source,native:measured,placement:native.placement,differences});
 }
}
assert.equal(reports.length,34);const exact=reports.filter(item=>!item.differences.length).length;
const summary={states:reports.length,exact,passed:exact===reports.length,gapsMatched:reports.filter(item=>Math.abs(item.source.gap-item.native.gap)<=1/32).length,widthsMatched:reports.filter(item=>Math.abs(item.source.width-item.native.width)<=1/32).length,heightsMatched:reports.filter(item=>Math.abs(item.source.height-item.native.height)<=1/32).length};
await fs.writeFile(output,JSON.stringify({format:'orchard-data-table-pagination-popup-layout-v1',referenceSha256,...summary,nativeComponentAccepted:false,numericBound:1/32,reports},null,2)+'\n');console.log(JSON.stringify(summary));if(!summary.passed)process.exitCode=1;
