import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const native=option('--native'),output=option('--output'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/SparklinePointer.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(reference.format,'orchard-sparkline-pointer-v1');assert.equal(reference.originalReferenceSha256,hash(await fs.readFile(new URL('../tests/Reference/SparklineLayout.json',import.meta.url))));
assert.equal(reference.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-sparkline-pointer.cjs',import.meta.url))));
const runtime=await read(path.join(native,'result.json'));assert.equal(runtime.passed,true);assert.ok(['sparkline',undefined].includes(runtime.component));for(const [key,value]of[['processArchitecture','X64'],['osArchitecture','X64'],['displayScale',1],['textScale',1],['animationsEnabled',false],['highContrast',false]])assert.equal(runtime.environment[key],value);
const near=(actual,expected)=>assert.ok(Number.isFinite(actual)&&Math.abs(actual-expected)<.001,`${actual}/${expected}`);let cases=0,keys=0,moves=0,gates=0;const reports=[];
for(const scheme of ['Light','Dark']){
 const actual=await read(path.join(native,`sparkline-${scheme}.json`)),source=reference.cases.filter(item=>item.scheme===scheme);assert.equal(actual.length,18);assert.equal(source.length,18);
 const input=await read(path.join(native,`sparkline-${scheme}-desktop.json`));assert.equal(input.pointerMoves.length,38);assert.equal(input.pointerPresentations.length,38);assert.equal(input.keyActions.length,19);
 for(const [index,expected]of source.entries()){
  const sample=actual[index];assert.equal(sample.id,expected.id);assert.equal(sample.pointerReferenceSha256,referenceSha256);
  for(const [field,phase]of[['hoverInspection','hover'],['exitInspection','exit']]){const snapshot=sample[field],state=expected[phase],visible=state.tooltip?.visible==='visible';assert.equal(snapshot.visible,visible);assert.equal(snapshot.text,state.tooltip?.text??'');assert.equal(snapshot.dots.length,state.dots.length);for(const [i,dot]of snapshot.dots.entries()){near(dot.x,+state.dots[i].cx);near(dot.y,+state.dots[i].cy);near(dot.r,+state.dots[i].r);}if(visible){near(snapshot.y,state.tooltip.box.y);near(snapshot.height,state.tooltip.box.height);}}
  const hover=input.pointerMoves[index*2],exit=input.pointerMoves[index*2+1];assert.equal(hover.target,'PART_Chart');assert.equal(exit.target,'SparklineAfter');assert.deepEqual(hover.localPoint,{X:expected.point.x,Y:expected.point.y});assert.equal(exit.bounds.X,hover.bounds.X);assert.equal(exit.bounds.Y-hover.bounds.Y,expected.outside.y);assert.equal(exit.bounds.Width,160);assert.equal(exit.bounds.Height,40);
  assert.ok(sample.pointerTrace.some(event=>!event.IsGenerated&&event.position.X===expected.point.x&&event.position.Y===expected.point.y));
  reports.push({id:sample.id,scheme,hover:sample.hoverInspection,exit:sample.exitInspection});cases++;
 }
 for(const gate of input.pointerPresentations){assert.equal(gate.passed,true);assert.ok(gate.stable>=2);gates++;}
 for(const move of input.pointerMoves){assert.equal(move.positioned,true);assert.equal(move.deliveryFailed,undefined);assert.equal(move.stationary,false);assert.equal(move.delivered,true);assert.deepEqual([move.actual.X,move.actual.Y],[move.requested.x,move.requested.y]);assert.ok(move.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated&&receipt.screenX===move.requested.x&&receipt.screenY===move.requested.y&&receipt.milliseconds<2000));moves++;}
 for(const key of input.keyActions){assert.equal(key.key,39);assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);keys++;}
 const mutation=await read(path.join(native,`sparkline-mutation-input-${scheme}.json`));assert.equal(mutation.passed,true);
}
assert.equal(cases,36);assert.equal(moves,76);assert.equal(gates,76);assert.equal(keys,38);
const report={format:'orchard-sparkline-pointer-audit-v1',checkedSourceStateGatesPassed:true,nativeComponentAccepted:false,sourcePixelParityVerified:false,sourceRouteParityVerified:false,referenceSha256,verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),cases,hoverExitStates:72,originalKeyboardStates:72,genuinePointerMoves:moves,pointerPresentationGates:gates,ownedRightKeyPairs:keys,originalKeyboardReferenceUnchanged:true,tolerance:.001,reports};await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,reports:undefined}));
