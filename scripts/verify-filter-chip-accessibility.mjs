import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
export async function auditFilterChipAccessibility(native){
 const runtime=await read(path.join(native,'result.json')),report=await read(path.join(native,'filter-chip-accessibility.json'));
 assert.equal(runtime.passed,true);assert.equal(runtime.component,'filter-chip-accessibility');assert.equal(report.passed,true);assert.equal(report.originalContentRestored,true);
 assert.equal(runtime.environment.processArchitecture,runtime.environment.osArchitecture);
 const states=['default','tab-order','reordered','live-label','explicit-name','bound-name','live-bound-name','linked-caption','live-linked-caption','name-priority','template-empty','template-replaced','localized-clear','command-disabled','group-disabled','native-disabled','invoke-command','invoke-single','invoke-clear','collection-replaced','empty','unloaded','remounted'];
 const standaloneNames={'live-label':'Remove Révision 日本語 filter','explicit-name':'Remove regional scope','bound-name':'Remove selected desk','live-bound-name':'Remove selected owner','linked-caption':'Caption scope','live-linked-caption':'Updated caption scope'};
 let processId,trees=0;
 for(const theme of ['Light','Dark']){
  const rows=report.reports.filter(item=>item.scheme===theme);assert.deepEqual(rows.map(item=>item.id),[...states,'consumer-callbacks']);
  for(const [index,id]of states.entries()){
   const row=rows[index],tree=await read(path.join(native,`uia-${id}-${theme}.json`));assert(Number.isInteger(tree.process)&&tree.process>0);processId??=tree.process;assert.equal(tree.process,processId);
   const singleName=standaloneNames[id]??(index>=states.indexOf('name-priority')?'Explicit remove priority':'Remove Region filter');
   const singleVisible=!['template-empty','unloaded'].includes(id),hasGroup=index<states.indexOf('empty'),replacement=id==='collection-replaced';
   const expected={'FilterA11y-Before':'Before filters'};
   if(singleVisible)expected['FilterA11y-Single']=singleName;
   if(hasGroup){if(replacement)expected['FilterA11y-Owner']='Remove Owner filter';else for(const name of index>=2?['Desk','Status']:['Status','Desk'])expected['FilterA11y-'+name]='Remove '+name+' filter';expected['FilterA11y-Clear']=index>=states.indexOf('localized-clear')?'Clear filters':'Clear all';}
   expected['FilterA11y-After']='After filters';assert.deepEqual(row.expected,expected);assert.deepEqual(tree.buttons.map(button=>button.id),Object.keys(expected));assert.deepEqual(row.buttonOrder,Object.keys(expected));
   for(const button of tree.buttons){
    assert.equal(button.name,expected[button.id]);assert.equal(button.controlType,'ControlType.Button');assert.equal(button.hasInvoke,true);assert.equal(button.offscreen,false);
    assert.equal(button.className,tree.buttons.find(item=>item.id==='FilterA11y-Before').className);
    assert.equal(button.labeledBy,button.id==='FilterA11y-Single'&&index>=states.indexOf('linked-caption')?(id==='linked-caption'?'Caption scope':'Updated caption scope'):null);
    const groupDisabled=['group-disabled','native-disabled'].includes(id),commandDisabled=['command-disabled','group-disabled','native-disabled'].includes(id);
    const enabled=button.id==='FilterA11y-Before'?!groupDisabled:button.id==='FilterA11y-Single'||button.id==='FilterA11y-After'?true:!groupDisabled&&(button.id!=='FilterA11y-Desk'||!commandDisabled);
    assert.equal(button.enabled,enabled);if(enabled)assert.equal(button.focusable,true);
    assert(Object.values(button.bounds).every(Number.isFinite)&&button.bounds.width>0&&button.bounds.height>0);assert(Array.isArray(button.runtimeId)&&button.runtimeId.length>0);
   }
   assert(tree.texts.includes('Language')&&tree.texts.includes('Português'));assert(!tree.texts.includes('Detached'));
   const invokeIds={'command-disabled':'Desk','group-disabled':'Status','native-disabled':'Before','invoke-command':'Desk','invoke-single':'Single','invoke-clear':'Clear'};
   assert.equal(tree.invokeId,invokeIds[id]?'FilterA11y-'+invokeIds[id]:null);assert.equal(tree.invokeOutcome,invokeIds[id]?(id.includes('disabled')?'element-not-enabled':'invoked'):'not-requested');
   if(id==='tab-order')assert.equal(tree.buttons.find(button=>button.id==='FilterA11y-After').focused,true);
   if(id==='reordered')assert.equal(tree.buttons.find(button=>button.id==='FilterA11y-Status').focused,true);
   trees++;
  }
  const callbacks=rows.at(-1);assert.equal(callbacks.singleCalls,1);assert.equal(callbacks.commandCalls,1);assert.equal(callbacks.clearCalls,1);assert.equal(callbacks.statusCalls,0);assert.equal(callbacks.callerOwnedFilters,true);
  const png=await fs.readFile(path.join(native,`catalog-${theme.toLowerCase()}.png`));assert.equal(png.subarray(1,4).toString(),'PNG');
 }
 assert.equal(report.tabs.length,28);assert.equal(report.tabs.filter(tab=>tab.reverse).length,4);for(const tab of report.tabs){assert.equal(tab.downs,1);assert.equal(tab.ups,1);assert.equal(tab.downOwned,true);assert.equal(tab.upOwned,true);}
 return{passed:true,externalTrees:trees,themes:2,ownedTabTransfers:report.tabs.length,reverseTabTransfers:4,externalInvocations:6,disabledInvocationsRejected:6,liveBoundAndLinkedNames:true,reorderFocusAndOrder:true,consumerOwnership:true,hiddenAndEmptyPeerRemoval:true,narratorVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
 const report=await auditFilterChipAccessibility(option('--native'));await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
