import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
import {verifyEdgeDialogSource} from './verify-edge-dialog-source.mjs';
const option=name=>{const i=process.argv.indexOf(name);assert(i>=0&&process.argv[i+1]);return process.argv[i+1];};
const original=await Promise.all(['--source','--motion','--presence'].map(async name=>JSON.parse(await fs.readFile(option(name)))));
verifyEdgeDialogSource(...original);
const changes=[
 ['wrong source pin',([r])=>r.commit='HEAD'],
 ['wrong Drawer bytes',([r])=>r.dependencies[0].sha256='0'.repeat(64)],
 ['missing source state',([r])=>r.cases.pop()],
 ['duplicated source state',([r])=>r.cases[1]=structuredClone(r.cases[0])],
 ['missing custom-height state',([r])=>r.cases.find(c=>c.spec.height===1000).spec.height=999],
 ['Drawer close button invented',([r])=>r.cases[0].measurement.close={}],
 ['Drawer header incorrectly retained',([r])=>r.cases.find(c=>c.spec.kind==='drawer'&&c.spec.shape==='no-heading').measurement.header={}],
 ['Drawer cap exceeded',([r])=>r.cases.find(c=>c.spec.kind==='drawer'&&c.spec.shape==='long').measurement.dialog.box.height=1000],
 ['Sheet horizontal viewport cap invented',([r])=>{const c=r.cases.find(c=>c.spec.kind==='sheet'&&c.spec.side==='top'&&c.spec.shape==='long');c.measurement.dialog.box.height=c.viewport.height;}],
 ['Sheet padding breakpoint changed',([r])=>r.cases.find(c=>c.spec.kind==='sheet'&&c.viewport.width===768).measurement.inner.paint.padding=[24,24,24,24]],
 ['description font changed',([r])=>r.cases[0].measurement.description.paint.fontFamily='Arial'],
 ['snap points falsely applied',([r])=>r.cases.find(c=>c.spec.shape==='snap').measurement.body.box.height++],
 ['refused close discards original input',([r])=>r.histories.find(h=>!h.accept).after.editor='lost'],
 ['accepted close retains surface',([r])=>r.histories.find(h=>h.accept).after.dialog={}],
 ['missing close history',([r])=>r.histories.pop()],
 ['wrong touch threshold at sixty',([r])=>r.gestures.find(g=>g.delta===60).after.events=['close']],
 ['touch history omitted',([r])=>r.gestures.pop()],
 ['opening timing changed',([,m])=>m.timelines[0].opening.panel.duration=180],
 ['Drawer backdrop exit curve conflated',([,m])=>m.timelines[0].closing.backdrop.easing='cubic-bezier(0.4, 0, 1, 1)'],
 ['motion sample altered',([,m])=>m.timelines[0].opening.points[1].translateY++],
 ['motion timeline omitted',([,m])=>m.timelines.pop()],
 ['presence trace omitted',([,,p])=>p.traces.pop()],
 ['presence timer shortened',([,,p])=>p.traces[0].steps.find(s=>s.phase==='exit'&&s.age===179).present=false],
 ['closing surface remains interactive',([,,p])=>p.traces[0].steps.find(s=>s.phase==='exit'&&s.age===0).inert=false],
 ['reopen removal not canceled',([,,p])=>p.traces[0].steps.find(s=>s.phase==='reopen'&&s.age===200).present=false],
 ['OS clock driver substituted',([,,p])=>p.traces[0].driver='system-clock'],
 ['reduced-motion presence fabricated',([,,p])=>p.traces.find(t=>t.reduced).steps.find(s=>s.phase==='enter'&&s.age===0).visible=true],
 ['missing font producer',([r])=>r.fonts.pop()],
 ['stale motion bundle input',([,m])=>m.bundledFiles[0].sha256='0'.repeat(64)]
];
for(const [name,change]of changes){const damaged=structuredClone(original);change(damaged);assert.throws(()=>verifyEdgeDialogSource(...damaged),name);}
const result={passed:true,negativeCasesRejected:changes.length,cases:changes.map(([name])=>name),nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({passed:true,negativeCasesRejected:changes.length}));
