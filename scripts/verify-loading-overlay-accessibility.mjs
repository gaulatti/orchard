import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output');
const read=async name=>JSON.parse((await fs.readFile(path.join(native,name),'utf8')).replace(/^\uFEFF/,''));
const result=await read('result.json'),report=await read('loading-overlay-accessibility.json');
assert.equal(result.passed,true);assert.equal(result.component,'loading-overlay-accessibility');
assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);assert.equal(report.reports.length,22);
const names={default:'Loading...','live-label':'Preparing visible content','explicit-name':'Preparing independent content','hidden-label':'Preparing independent content','bound-name':'Préparer le dossier 日本語','live-bound-name':'Live consumer operation','linked-caption':null,'live-linked-caption':'Updated linked operation','name-priority':'Explicit priority','template-replacement':'Explicit priority'};
let processId;
for(const theme of['Light','Dark']){
 const png=await fs.readFile(path.join(native,`catalog-${theme.toLowerCase()}.png`));assert.equal(png.subarray(1,4).toString(),'PNG');
 for(const[id,name]of Object.entries(names)){
  const matches=report.reports.filter(item=>item.id===id&&item.scheme===theme);assert.equal(matches.length,1);
  const state=matches[0],expected=name??(theme==='Light'?'Linked operation':'Updated linked operation');
  assert.equal(state.expected,expected);assert.equal(state.actual,expected);assert.equal(state.role,'ProgressBar');
  assert.equal(state.IsTabStop,false);assert.equal(state.focusPreserved,true);assert.equal(state.hasRangeValue,false);
  const tree=await read(`uia-${id}-${theme}.json`);assert(Number.isInteger(tree.process)&&tree.process>0);
  processId??=tree.process;assert.equal(tree.process,processId);assert.equal(tree.progress.length,1);
  const peer=tree.progress[0];assert.equal(peer.name,expected);assert.equal(peer.className,'BleeckerLoadingSpinner');assert.equal(peer.controlType,'ControlType.ProgressBar');
  assert.equal(peer.isKeyboardFocusable,false);assert.equal(peer.isOffscreen,false);assert.equal(peer.hasRangeValuePattern,false);
  assert(Object.values(peer.bounds).every(Number.isFinite)&&peer.bounds.width>0&&peer.bounds.height>0);
 }
 const hidden=report.reports.filter(item=>item.id==='hidden'&&item.scheme===theme);assert.equal(hidden.length,1);assert.equal(hidden[0].progressPeers,0);
 const tree=await read(`uia-hidden-${theme}.json`);assert.equal(tree.process,processId);assert.deepEqual(tree.progress,[]);
}
const audit={passed:true,componentAccepted:false,themes:2,visibleStates:20,externalTrees:22,explicitAndBoundNamesVerified:true,linkedCaptionsVerified:true,focusPreserved:true,hiddenPeerRemovalVerified:true,narratorVerified:false};
await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
