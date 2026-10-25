import fs from 'node:fs/promises';import assert from 'node:assert/strict';
import {verifySimpleDataTable,verifySimplePaint}from './verify-simple-data-table.mjs';
const opts=Object.fromEntries(process.argv.slice(2).reduce((r,v,i,a)=>i%2===0?[...r,[v.replace(/^--/,''),a[i+1]]]:r,[]));
const native=JSON.parse(await fs.readFile(opts.native+'/simple-data-table.json')),source=JSON.parse(await fs.readFile(opts.reference));verifySimpleDataTable(native,source);let rejected=0;
function reject(change){const copy=structuredClone(native);change(copy);assert.throws(()=>verifySimpleDataTable(copy,source));rejected++;}
for(const key of ['passed','publicApisOnly'])reject(n=>n[key]=false);
for(const key of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','physicalInputVerified','sourceRasterParityVerified','nativeComponentAccepted'])reject(n=>n[key]=true);
reject(n=>n.focus=1);reject(n=>n.activations=1);reject(n=>n.guards=0);
reject(n=>n.cases.pop());reject(n=>n.cases[1]=n.cases[0]);reject(n=>n.histories.pop());reject(n=>n.histories[1]=n.histories[0]);reject(n=>n.contracts.pop());reject(n=>n.pngs.pop());
reject(n=>n.cases[0].snapshot.header[1].box.width+=2);reject(n=>n.cases[0].snapshot.body[0].colSpan++);
reject(n=>n.cases[0].sparseGridMetadataVerified=false);
reject(n=>n.cases.find(c=>c.name==='default').snapshot.rows[0].id='replaced');
reject(n=>n.cases.find(c=>c.name==='partial-selection').snapshot.checkboxes[0].indeterminate=false);
reject(n=>n.cases.find(c=>c.name==='sorted-asc').snapshot.sortButtons[1].icon='PART_Down');
reject(n=>n.cases[0].snapshot.sortButtons[0].fontSize=14);
reject(n=>n.cases[0].snapshot.sortButtons[0].color=[255,0,0,255]);
reject(n=>n.histories[0].snapshots[1].snapshot.events=[]);reject(n=>n.histories[0].snapshots[1].snapshot.footerOrder='wrong');
reject(n=>n.histories[0].snapshots[1].originalRowsAndFooterProducerRetained=false);reject(n=>n.histories[0].snapshots[1].nativeRequestsMatchSource=false);
for(const key of ['originalNativeActionInvoked','originalCellsRetainedAfterSort','originalTableAndContentRestored','collectionChangesObserved','rowPropertyNotificationsObserved','controlledBindingsRetained','removedItemObserversReleased'])reject(n=>n.contracts[0][key]=false);
const actual=JSON.parse(await fs.readFile('tests/Reference/SimpleDataTableCounterexamples.json'));let actualRejected=0;
for(const old of actual.actualCases){
 const copy=structuredClone(native),target=copy.cases.find(c=>c.scheme===old.scheme&&c.name===old.name).snapshot;
 // Replay the measured old geometry inside the current report schema, so
 // rejection must be caused by the real failed layout rather than a missing
 // field added to the report after that runtime was captured.
 for(const key of ['fixture','table'])target[key]=old.snapshot[key];
 for(const group of ['header','body','footer'])for(let i=0;i<target[group].length;i++)target[group][i].box=old.snapshot[group][i].box;
 if(target.caption)target.caption.box=old.snapshot.caption.box;
 assert.throws(()=>verifySimpleDataTable(copy,source),assert.AssertionError);actualRejected++;
}
for(const old of actual.actualHistorySnapshots){const copy=structuredClone(native),target=copy.histories.find(h=>h.scheme===old.scheme&&h.action===old.action).snapshots.find(s=>s.stage===old.stage).snapshot;for(let i=0;i<old.footer.length;i++)target.footer[i].box=old.footer[i].box;assert.throws(()=>verifySimpleDataTable(copy,source),assert.AssertionError);actualRejected++;}
let paintRejected=0;for(const [a,b]of [[[0,0,0,255],[255,255,255,255]],[[24,37,51,255],[32,45,58,255]]]){assert.throws(()=>verifySimplePaint(a,b));paintRejected++;}
const result={passed:true,corruptReportsRejected:rejected,actualLayoutCounterexamplesRejected:actualRejected,wrongPaintSamplesRejected:paintRejected};await fs.writeFile(opts.output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
