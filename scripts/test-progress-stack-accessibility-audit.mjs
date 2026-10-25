import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {auditProgressStackAccessibility,loadProgressStackAccessibility} from './verify-progress-stack-accessibility.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const original=await loadProgressStackAccessibility(option('--native'));auditProgressStackAccessibility(original);
const cases=[
 ['painted-name',data=>{data.report.snapshots.find(row=>row.id==='soft-hyphen').tree.elements[0].name='publication‐\nworkflow';}],
 ['dropped-control',data=>{data.report.snapshots[0].tree.elements[0].control=false;}],
 ['lost-text-pattern',data=>{data.report.snapshots[0].tree.elements[0].hasText=false;}],
 ['wrong-document-text',data=>{data.report.snapshots[0].tree.elements[0].text='wrong';}],
 ['duplicate-text',data=>{data.report.snapshots[0].tree.elements.push(data.report.snapshots[0].tree.elements[0]);}],
 ['same-process',data=>{data.report.snapshots[0].tree.clientProcess=data.report.snapshots[0].tree.process;}],
 ['interactive-group',data=>{data.report.snapshots[0].tree.group.hasInvoke=true;}],
 ['missing-live-phase',data=>{data.updates[0].states=data.updates[0].states.filter(row=>row.phase!=='retemplate');}],
 ['lost-provider-identity',data=>{data.updates[0].states.findLast(row=>row.phase==='live-name-event').tree.retained.runtimeId=[1,2,3];}],
 ['missing-name-event',data=>{for(const row of data.updates[0].states)row.tree.nameEvents=[];}],
 ['stale-live-label',data=>{data.updates[0].states.findLast(row=>row.phase==='live-name-event').tree.retained.name='old';}],
 ['missing-theme',data=>{data.report.snapshots=data.report.snapshots.filter(row=>row.scheme!=='Dark');}]
];
const rejected=[];
for(const [name,mutate]of cases){const data=structuredClone(original);mutate(data);assert.throws(()=>auditProgressStackAccessibility(data),undefined,name);rejected.push(name);}
const report={passed:true,rejected};await fs.writeFile(option('--output'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
