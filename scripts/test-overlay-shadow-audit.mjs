import fs from 'node:fs/promises';import path from 'node:path';import assert from 'node:assert/strict';import {verifyShadow,verifyShadowModel} from './verify-overlay-shadow.mjs';
const[folder,host,output]=process.argv.slice(2);await verifyShadow(folder,host);const r=JSON.parse(await fs.readFile(path.join(folder,'shadow.json'))),s=JSON.parse(await fs.readFile(path.join(folder,'shadow-reference.json'))),paint=JSON.parse(await fs.readFile(path.join(folder,'shadow-paint-reference.json'))),rejected=[];
const changes=[['failed run',x=>x.passed=false],['wrong format',x=>x.format='old'],['hardware input',x=>x.hardwareInputSent=true],['visible host',x=>x.hostVisible=true],['focus',x=>x.focusEvents=1],['activation',x=>x.activations=1],['invented acceptance',x=>x.nativeComponentAccepted=true],['guards missing',x=>x.guards=0],['case missing',x=>x.cases.pop()],['case identity changed',x=>x.cases[0].id='other'],['prototype mode',x=>x.cases[0].mode='prototype'],['legacy reader omitted',x=>x.cases[0].legacyShadowReaderVerified=false],['background replaced',x=>x.cases[0].originalBackgroundRetained=false],['interior shadow present',x=>x.cases[0].interiorShadowAbsent=false],['shadow layer missing',x=>x.cases[0].shadowLayers=1],['unstable on frame',x=>x.cases[0].stableFrames=1],['on attempts omitted',x=>x.cases[0].attempts=0],['unstable off frame',x=>x.cases[0].noShadowImage.stableFrames=1],['off attempts omitted',x=>x.cases[0].noShadowImage.attempts=0],['on-off interior changed',x=>x.cases[0].noShadowInside[0]^=1],['lifecycle missing',x=>x.lifecycle.pop()],['lifecycle identity changed',x=>x.lifecycle[0].id='other'],['lifecycle failed',x=>x.lifecycle[0].passed=false],['container replaced',x=>x.lifecycle[0].containerRetained=false],['layers replaced',x=>x.lifecycle[0].layerRecordsRetained=false],['effect brush replaced',x=>x.lifecycle[0].effectBrushesRetained=false],['resources leaked',x=>x.lifecycle[5].resourcesCleared=false],['clip leaked',x=>x.lifecycle[5].clipCleared=false]];
changes.push(
 ['wrong paint encoding',x=>x.paintEncoding='scalar-opacity'],
 ['wrong pixel scale',x=>x.rasterizationScale=2],
 ['paint reference changed',x=>x.paintReferenceSha256='0'.repeat(64)],
 ['paint case missing',x=>x.paintCases.pop()],
 ['paint layer color changed',x=>x.cases[0].paintLayers[0].rgba[0]^=1],
 ['scalar opacity applied twice',x=>x.cases[0].paintLayers[0].opacity=.06],
 ['paint source changed',x=>x.paintCases[0].source='other.png'],
 ['paint frame unstable',x=>x.paintCases[0].stableFrames=1],
 ['paint background replaced',x=>x.paintCases[0].nullBackgroundRetained=false],
 ['paint graph leaked',x=>x.paintCases[0].unloadedGraphDetached=false],
 ['projected paint changed',x=>x.paintCases[0].observedColor[0]^=1]
);
for(const[name,change]of changes){const x=structuredClone(r);change(x);assert.throws(()=>verifyShadowModel(x,s,paint),name);rejected.push(name);}const report={passed:true,rejected:rejected.length,counterexamples:rejected};await fs.writeFile(output,JSON.stringify(report,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify({passed:true,rejected:rejected.length}));
