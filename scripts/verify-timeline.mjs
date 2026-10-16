import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
export async function auditTimeline(native,viewport,fullSuite=false){
 assert([1024,639,640].includes(viewport));
 const bytes=await fs.readFile(new URL('../tests/Reference/Timeline.json',import.meta.url)),reference=JSON.parse(bytes);
 assert.equal(hash((await fs.readFile(new URL('./reference/capture-timeline.cjs',import.meta.url),'utf8')).replace(/\r\n/g,'\n')),reference.scriptSha256);
 const runtime=await read(path.join(native,'result.json'));assert.equal(runtime.passed,true);
 if(fullSuite){assert.equal(runtime.component,undefined);assert.equal(runtime.systemAppearance,true);assert.equal(runtime.captures.length,2);}
 else assert.equal(runtime.component,'timeline');
 assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.highContrast,false);
 let cases=0,baselines=0,crops=0,maximumGeometryError=0,maximumBaselineError=0;
 const near=(actual,expected)=>{assert(Number.isFinite(actual));const delta=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,delta);assert(delta<=.05,`${actual} differs from source ${expected}`);};
 const box=(actual,expected)=>{for(const key of['x','y','width','height'])near(actual[key[0].toUpperCase()+key.slice(1)],expected[key]);};
 const text=(actual,expected)=>{
  assert(actual&&Number.isFinite(actual.baseline),'Missing measured text baseline.');
  const delta=Math.abs(actual.baseline-expected.firstBaseline);maximumBaselineError=Math.max(maximumBaselineError,delta);near(actual.baseline,expected.firstBaseline);
  assert.equal(actual.text,expected.text.replace(/[ \t\r\n\f]+/g,' ').trim());baselines++;
 };
 for(const scheme of['Light','Dark']){
  const expected=reference.cases.filter(test=>test.viewportWidth===viewport&&test.scheme===scheme),report=await read(path.join(native,`timeline-source-${viewport}-${scheme}.json`));
  assert.equal(report.cases.length,12);assert.deepEqual(report.failures,[]);assert.deepEqual(report.cases.map(test=>test.id),expected.map(test=>test.id));
  const input=await read(path.join(native,`timeline-input-${scheme}.json`));assert.equal(input.physicalActivations,3);
  for(const flag of['passed','originalContent','inheritedDisabled','keyedReorder','keyedReplacement','ordinalDefaultStatus','templateReplacement','unloadRemount','removal','signedZero','liveShadowBrush','originalTemplateContext','liveTemplateBinding','liveBreakpoint'])assert.equal(input[flag],true);
  const captures=(await read(path.join(native,`timeline-viewport-${scheme}.json`))).reports;
  assert.equal(captures.length,12);assert.deepEqual(captures.map(capture=>capture.file),expected.map(test=>test.file));
  const restoration=await read(path.join(native,`timeline-viewport-restored-${scheme}.json`));assert(restoration.restored&&restoration.overlayRemoved);
  for(const [index,test]of expected.entries()){
   const actual=report.cases[index];assert.deepEqual(actual.errors,[]);assert.equal(actual.rows.length,test.normal.rows.length);
   for(const [rowIndex,target]of test.normal.rows.entries()){
    const row=actual.rows[rowIndex];box(row.row,target.box);box(row.icon,target.icon.box);box(row.body,target.body);box(row.header,target.header);
    assert.equal(row.status.toLowerCase(),target.status);
    if(target.connector)box(row.connector,target.connector.box);else assert.equal(row.connector,null);
    for(const key of['title','timestamp','description'])if(target[key]){
     box(row[key],target[key].box);if(target[key].firstBaseline!=null){text(row[key+'Typography'],target[key]);near(row[key+'Typography'].LineHeight,parseFloat(target[key].lineHeight));}
    }
    const icon=test.props.items[rowIndex].icon;
    if((!icon||typeof icon!=='object')&&target.icon.firstBaseline!=null){text(row.iconTypography,target.icon);if(target.icon.child)box(row.iconContent,target.icon.child);}
   }
   const capture=captures[index],image=readRgbaPng(await fs.readFile(path.join(native,test.file)));
   assert.equal(capture.masks,0);assert.equal(capture.resampling,false);
   assert.equal(capture.frame.width,test.viewportWidth);assert.equal(capture.frame.height,Math.max(900,Math.ceil(test.normal.box.height+40)));
   assert.equal(image.width,capture.frame.width);assert.equal(image.height,capture.frame.height);
   for(const value of Object.values(capture.frame))assert(Number.isInteger(value),'Viewport crop has a fractional coordinate.');
   if(capture.clientFile){
    assert.equal(capture.pipeline,'owned-client-integer-crop');
    const client=readRgbaPng(await fs.readFile(path.join(native,capture.clientFile)));
    assert.equal(client.width,capture.client.width);assert.equal(client.height,capture.client.height);
    assert(capture.frame.x>=0&&capture.frame.y>=0&&capture.frame.x+image.width<=client.width&&capture.frame.y+image.height<=client.height);
    for(let y=0;y<image.height;y++){
     const start=((capture.frame.y+y)*client.width+capture.frame.x)*4;
     assert.deepEqual(image.rgba.subarray(y*image.width*4,(y+1)*image.width*4),client.rgba.subarray(start,start+image.width*4),'Viewport crop changed original client pixels.');
    }
    crops++;
   }else{assert.equal(test.id,'empty');assert.equal(capture.pipeline,'empty-bitmap');}
   cases++;
  }
 }
 const desktops=await Promise.all((await fs.readdir(native)).filter(name=>name.startsWith('timeline-')&&name.endsWith('-desktop.json')).map(name=>read(path.join(native,name))));
 const moves=desktops.flatMap(report=>report.pointerMoves),gates=desktops.flatMap(report=>report.pointerPresentations),keys=desktops.flatMap(report=>report.keyActions),clicks=desktops.flatMap(report=>report.pointerClicks);
 for(const move of moves){assert.notEqual(move.deliveryFailed,true);assert(move.stationary||move.delivered);assert.equal(move.positioned,true);}
 for(const gate of gates){assert(gate.passed&&gate.stable>=2&&gate.milliseconds<2000);}
 for(const key of keys){assert.equal(key.downs,1);assert.equal(key.ups,1);assert.equal(key.targetRetained,true);}
 for(const click of clicks){assert.equal(click.delivered,true);assert(click.receipts.some(receipt=>receipt.exact&&!receipt.IsGenerated));}
 assert.equal(cases,24);assert.equal(baselines,178);assert.equal(crops,22);assert.equal(keys.length,4);assert(clicks.length>=2);
 return{passed:true,viewport,cases,baselines,verifiedViewportCrops:crops,maximumGeometryError,maximumBaselineError,geometryTolerance:.05,masks:0,resampling:false,
  referenceSha256:hash(bytes),sourcePixelParityVerified:false,nativeComponentAccepted:false,pointerMoves:moves.length,pointerGates:gates.length,ownedKeyPairs:keys.length,physicalClicks:clicks.length};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],`Pass ${name} <value>.`);return process.argv[at+1];};
 const report=await auditTimeline(path.resolve(option('--native')),Number(option('--viewport')),process.argv.includes('--full-suite'));
 await fs.writeFile(path.resolve(option('--output')),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}
