import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const input=option('--input'),reference=option('--reference'),output=option('--output');
const original=JSON.parse((await fs.readFile(input,'utf8')).replace(/^\uFEFF/,''));await fs.mkdir(output,{recursive:true});
for(const row of [...original.frames,...original.paintFrames]){assert.equal(path.basename(row.capture.file),row.capture.file);await fs.copyFile(path.join(path.dirname(input),row.capture.file),path.join(output,row.capture.file));}
const fixture=path.join(output,'independent-report.json');const audit=path.join(output,'audit.json');
const run=()=>spawnSync(process.execPath,['scripts/verify-hidden-progress-stack.mjs','--input',fixture,'--reference',reference,'--output',audit],{encoding:'utf8'});
await fs.writeFile(fixture,JSON.stringify(original));const baseline=run();assert.equal(baseline.status,0,baseline.stdout+baseline.stderr);
const unicode=(report,id)=>report.frames.find(row=>row.reference==='ProgressStackText.json'&&row.id===id);
const mutations=[
 ['visible-window',r=>r.visible=true],['native-visible-window',r=>r.nativeVisible=true],['activation',r=>r.activationEvents=1],['input-sent',r=>r.inputSent=true],
 ['missing-guards',r=>r.guardChecks=0],['missing-frame',r=>r.frames.pop()],['reference-hash',r=>r.references[0].sha256='0'.repeat(64)],
 ['label-geometry',r=>unicode(r,'cjk-zero').measurement.items[0].label.box.width+=.01],
 ['consumer-label',r=>unicode(r,'word-joiner').labels[0]='Reviewqueue'],
 ['tooltip-text',r=>unicode(r,'zero-width-space').measurement.segments[0].title='Consumerownedpublication: 6'],
 ['discretionary-hyphen',r=>{const row=unicode(r,'soft-hyphen');row.measurement.items[0].label.renderedText=row.measurement.items[0].label.renderedText.replace('\u2010','');}],
 ['theme-color',r=>r.frames[0].measurement.items[0].label.color.r^=1],['interactive-pattern',r=>r.frames[0].measurement.hasRange=true],
 ['scale-mismatch',r=>r.displayScale=1.25],['component-acceptance-claim',r=>r.nativeComponentAccepted=true],['visible-runtime-claim',r=>r.visibleRuntimeVerified=true]
 ,['stale-font-runs',r=>r.fontTransitions.find(row=>row.phase==='custom').explicitRunFamilies=['Microsoft YaHei']],['missing-font-transition',r=>r.fontTransitions.pop()],
 ['capture-hash',r=>r.frames[0].capture.pngSha256='0'.repeat(64)],['capture-dimensions',r=>r.frames[0].capture.pixelWidth=639],['capture-filename',r=>r.frames[0].capture.file='../unrelated.png'],
 ['missing-collection-update',r=>r.collectionUpdates.pop()],['collection-ownership',r=>r.collectionUpdates[0].retainedOriginalItems=false],['collection-order',r=>r.collectionUpdates[0].measurement.items.reverse()],['empty-legend-spacing',r=>r.collectionUpdates.find(row=>row.id==='clear').measurement.root.box.height=12],
 ['missing-paint-frame',r=>r.paintFrames.pop()],['paint-alpha',r=>r.paintFrames[0].brush.a=50],['paint-capture-hash',r=>r.paintFrames[0].capture.pngSha256='0'.repeat(64)]
];
const rejected=[];for(const[name,change]of mutations){const altered=structuredClone(original);change(altered);await fs.writeFile(fixture,JSON.stringify(altered));assert.notEqual(run().status,0,`Hidden audit accepted ${name}`);rejected.push(name);}
await fs.writeFile(fixture,JSON.stringify(original));const capture=path.join(output,original.frames[0].capture.file),bytes=await fs.readFile(capture),corrupted=Buffer.from(bytes);corrupted[8]^=1;
try{await fs.writeFile(capture,corrupted);assert.notEqual(run().status,0,'Hidden audit accepted a corrupted capture file.');rejected.push('corrupted-capture-file');}finally{await fs.writeFile(capture,bytes);}
const paintCapture=path.join(output,original.paintFrames[0].capture.file),paintBytes=await fs.readFile(paintCapture),otherPaint=await fs.readFile(path.join(output,original.paintFrames[1].capture.file)),changedPaint=structuredClone(original);
changedPaint.paintFrames[0].capture.pngSha256=crypto.createHash('sha256').update(otherPaint).digest('hex');
try{await fs.writeFile(paintCapture,otherPaint);await fs.writeFile(fixture,JSON.stringify(changedPaint));assert.notEqual(run().status,0,'Hidden audit accepted different paint pixels with a valid PNG hash.');rejected.push('paint-pixels-with-valid-hash');}finally{await fs.writeFile(paintCapture,paintBytes);await fs.writeFile(fixture,JSON.stringify(original));}
await fs.writeFile(path.join(output,'result.json'),JSON.stringify({passed:true,rejected,independentReportCopy:true,independentCaptureCopies:true},null,2)+'\n');console.log(JSON.stringify({passed:true,rejected}));
