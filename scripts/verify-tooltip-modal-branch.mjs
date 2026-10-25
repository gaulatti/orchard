import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const rgb=value=>{const values=value.match(/^rgb\((\d+), (\d+), (\d+)\)$/);assert(values,value);return '#'+values.slice(1).map(x=>Number(x).toString(16).padStart(2,'0')).join('')+'ff';};
const keys=['x','y','width','height'];

export function verifyTooltipBranchModel(native,source) {
 assert.equal(native.format,'orchard-tooltip-modal-branch-v1');assert.equal(native.passed,true);
 assert.equal(source.format,'orchard-tooltip-reference-v1');assert.equal(source.commit,'a86059013cd59684ce452849278ea81e734f2066');
 assert.equal(source.dependencies.find(d=>d.path==='src/components/tooltip.tsx')?.sha256,'31006ce0674960ac4feb5ca94581a6dbfb88b507478007f952623398460f789b');
 assert.equal(source.cases.length,40);assert.equal(source.histories.length,4);assert.equal(native.frames.length,72);assert.equal(native.cases.length,13);
 for(const field of ['activateCalled','showWindowCalled','focusCalled','hardwareInputSent','nativeInputFocusVerified','nativeComponentAccepted','sourceRasterParityVerified'])assert.equal(native[field],false,field);
 assert.equal(native.focus,0);assert.equal(native.activations,0);assert(native.guards>=400);
 assert.equal(new Set(native.frames.map(f=>f.mode+':'+f.sourceFile)).size,72);
 const geometryErrors=[],textSourceDifferences=[];let exactSourceGeometryValues=0,baselineGeometryValues=0;
 const compare=(actual,expected,label,errors)=>{for(const key of keys){assert(Number.isFinite(actual[key])&&Number.isFinite(expected[key]));if(actual[key]!==expected[key])errors.push({label,key,actual:actual[key],expected:expected[key]});}};
 for(const h of source.histories){assert.equal(h.after.tooltip,null);assert.equal(h.after.focused,'tip-trigger');assert.deepEqual(h.after.events,[]);assert.equal(h.after.dialogs,h.scope==='modal'?1:0);}
 for(const f of native.frames){
  const s=source.cases.find(c=>c.file===f.sourceFile);assert(s,f.sourceFile);assert.equal(f.scheme,s.scheme);assert.deepEqual(f.spec,s.spec);assert.equal(f.side,s.measurement.tooltip.side);assert.equal(f.role,'ToolTip');assert.equal(f.originalContentRetained,true);assert.equal(f.describedBy,true);
  assert.equal(f.popupCount,f.mode==='native'?1:0);assert.equal(f.zIndex,f.mode==='native'?null:f.mode==='owned'?51:180);assert.equal(s.measurement.tooltip.zIndex,s.spec.scope==='modal'?51:180);
  assert.equal(f.background.toLowerCase(),rgb(s.measurement.tooltip.paint.background));assert.equal(f.foreground.toLowerCase(),rgb(s.measurement.tooltip.paint.color));assert.equal(f.arrowFill.toLowerCase(),f.background.toLowerCase());
  assert.equal(s.measurement.tooltip.paint.fontSize,'12px');assert.equal(s.measurement.tooltip.paint.lineHeight,'19.5px');assert.deepEqual(s.measurement.tooltip.paint.padding,[12,8,12,8]);assert.equal(s.measurement.tooltip.paint.radius,'6px');
  const differences=f.spec.slot==='fixed'?geometryErrors:textSourceDifferences;
  compare(f.trigger,s.measurement.trigger,f.mode+':'+f.sourceFile+':trigger',differences);compare(f.body,s.measurement.tooltip.box,f.mode+':'+f.sourceFile+':body',differences);compare(f.arrow,s.measurement.tooltip.arrow.box,f.mode+':'+f.sourceFile+':arrow',differences);
  exactSourceGeometryValues+=12;
  if(f.mode==='hosted') {const baseline=native.frames.find(x=>x.mode==='native'&&x.sourceFile===f.sourceFile);assert(baseline);for(const part of ['body','arrow','trigger']){compare(f[part],baseline[part],f.sourceFile+':'+part+':native-vs-hosted',geometryErrors);baselineGeometryValues+=4;}assert.equal(f.side,baseline.side);}
 }
 for(const scheme of ['Light','Dark'])for(const name of ['inactive-owned-tooltip','resume-and-escape-priority']){const matches=native.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(matches.length,1);const c=matches[0];if(name==='inactive-owned-tooltip'){assert(c.requestRetained&&c.inactiveEscapeRejected&&c.callerEnabled);}else{assert(c.originalContentRetained&&c.parentRetained);assert.equal(c.actualKeyboardDeliveryVerified,false);}}
 for(const scheme of ['Light','Dark'])for(const name of ['select-suspends-and-resumes-tooltip','tooltip-in-select-and-owner-unload']){const matches=native.cases.filter(c=>c.scheme===scheme&&c.name===name);assert.equal(matches.length,1);const c=matches[0];if(name==='select-suspends-and-resumes-tooltip')assert(c.requestRetained&&c.inactivePointerBlocked&&c.inactivePeerExcluded&&c.resumedPeerPresent);else assert(c.activePointerAllowed&&c.activePeerIncluded&&c.logicalContainment&&c.closedOnce);}
 const closed=native.cases.filter(c=>c.name==='window-close-with-live-tooltip');assert.equal(closed.length,1);assert(closed[0].closedOnce&&closed[0].requestClosed&&closed[0].branchReleased);
 for(const scheme of ['Light','Dark']){const matches=native.cases.filter(c=>c.name==='actual-catalog-tooltip-popover'&&c.scheme===scheme);assert.equal(matches.length,1);assert(matches[0].originalFixture&&matches[0].logicalContainment&&matches[0].closedWithOwner);}
 for(const scheme of ['Light','Dark']){const matches=native.cases.filter(c=>c.name==='field-help-service-lifetime'&&c.scheme===scheme);assert.equal(matches.length,1);assert(matches[0].originalTooltip&&matches[0].descriptionLinked&&matches[0].emptyHelpClosedOnce&&matches[0].descriptionReleased);}
 assert.deepEqual(geometryErrors,[]);
 assert.deepEqual(textSourceDifferences,[]);
 return {sourceStates:40,nativeFrames:72,ownedFrames:8,ownershipCases:13,catalogCompositionCases:2,fieldHelpCases:2,exactSourceGeometryValues,baselineGeometryValues,geometryErrors,textSourceDifferences,completeTextSourceGeometryVerified:true};
}

export async function verifyTooltipBranch(folder,reference) {
 const bytes=await fs.readFile(reference),source=JSON.parse(bytes),native=JSON.parse(await fs.readFile(path.join(folder,'tooltip-modal-branch.json'))),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));
 assert(exit.exited&&!exit.timedOut&&exit.exitCode===0);assert.equal(native.sourceSha256,hash(bytes));
 assert.equal(native.librarySha256,hash(await fs.readFile(path.join(folder,'app-identity','Orchard.dll'))));assert.equal(native.hostSha256,hash(await fs.readFile(path.join(folder,'app-identity','Orchard.Catalog.dll'))));
 const comparison=verifyTooltipBranchModel(native,source);
 for(const capture of native.captures){assert.equal(path.basename(capture.file),capture.file);const bytes=await fs.readFile(path.join(folder,capture.file));assert.equal(hash(bytes),capture.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,900);assert.equal(png.height,760);}
 assert.equal(native.captures.length,6);
 return {format:'orchard-tooltip-modal-branch-audit-v1',passed:true,librarySha256:native.librarySha256,hostSha256:native.hostSha256,sourceSha256:native.sourceSha256,...comparison,captures:native.captures.length,guards:native.guards,nativeInputFocusVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){const arg=n=>{const i=process.argv.indexOf(n);assert(i>=0&&process.argv[i+1],n);return process.argv[i+1];};const result=await verifyTooltipBranch(arg('--native'),arg('--reference'));await fs.writeFile(arg('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,textSourceDifferences:result.textSourceDifferences.length}));}
