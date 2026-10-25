import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return process.argv[at+1];};
const native=path.resolve(option('--native')),app=path.resolve(option('--app')),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const read=async file=>JSON.parse(await fs.readFile(path.join(native,file)));
const report=await read('scroll-area-primitive.json'),exit=await read('process-exit.json'),build=await read('result.json'),cleanup=await read('cleanup.json');
assert.equal(report.passed,true);assert.deepEqual(report.failures,[]);assert.equal(report.scope,'hidden-scroll-area-native-capability-probe');assert.equal(report.diagnosticOnly,true);
for(const field of ['sourceContractVerified','activateCalled','showCalled','focusCalled','inputSent','nativeScrollAreaImplemented','nativeComponentAccepted'])assert.equal(report[field],false,field);
assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);assert.equal(report.activations,0);assert.equal(report.focus,0);assert(report.guards>=334);assert.equal(report.processArchitecture,'X64');assert.equal(report.osArchitecture,'X64');
assert.equal(build.buildPassed,true);assert.equal(build.runtimeVerified,false);assert.equal(build.platform,'x64');assert.equal(build.packageSha256,hash(await fs.readFile(path.resolve(option('--package')))));
assert.equal(build.packageSha256,'39c827308782765118b3e559dcd2a9cdb962687df67f0ea4b8a142ef2d1da99a');assert.equal(report.librarySha256,'98dfc37b389c50e4ca74e1a0c449f293dbec0d69a5d00e44f2733e7665d7e765');
assert.equal(cleanup.removed,true);assert.equal(cleanup.isolatedConsumer,build.isolatedConsumer);await assert.rejects(fs.access(cleanup.isolatedConsumer),{code:'ENOENT'});
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.hostSha256,hash(await fs.readFile(path.join(app,'PackageConsumer.dll'))));
const sourceBytes=await fs.readFile('tests/Reference/ScrollArea.json'),source=JSON.parse(sourceBytes);
assert.equal(hash(sourceBytes),'255e5a390a0f738e54b2f5cc8f73765a9474a66515f656f87fce6641b107808e');
const reserved=['vertical-ltr','vertical-rtl','horizontal-ltr','horizontal-rtl','both-ltr','both-rtl'];
assert.deepEqual(report.cases.map(r=>r.name),['legacy-hidden-axis','legacy-disabled-axis','modern-vertical-content','modern-both-content',...reserved.map(s=>'modern-reserved-'+s)]);
let geometry=0,pixels=0,sourceThumbs=0;
const same=(actual,expected,message)=>{assert.deepEqual(actual,expected,message);geometry++;};
for(const row of report.cases){
 const isReserved=row.name.startsWith('modern-reserved-'),id=row.name.slice('modern-reserved-'.length),rtl=id.endsWith('-rtl'),horizontal=id.startsWith('horizontal'),vertical=id.startsWith('vertical');
 assert.equal(row.nativeScrollViewerSubclassable,false);assert.equal(row.nativeScrollViewSubclassable,true);assert.equal(row.sourceHiddenAxisOffsetMatched,row.name!=='legacy-disabled-axis');
 assert.deepEqual(row.samples.map(r=>r.stage),['initial','programmatic-hidden-axis','auto-width','content-shrink']);
 for(const sample of row.samples)assert.equal(sample.originalRetained,true,row.name+'/'+sample.stage);
 const [initial,scrolled,natural,shrunk]=row.samples;
 if(isReserved){
  const frozenRow=source.cases.find(r=>r.scheme==='Light'&&r.id===id+'-overflow'),frozen=frozenRow.measurement;
  same(initial.viewport,frozen.area,row.name);same(initial.content,frozen.content,row.name);
  same([initial.viewportWidth,initial.viewportHeight,initial.extentWidth,initial.extentHeight],[frozen.client.width,frozen.client.height,frozen.scroll.width,frozen.scroll.height],row.name);
  const input=source.interactions.find(r=>r.scheme==='Light'&&r.id===id).samples.find(r=>r.label===(rtl?'programmatic-negative':'programmatic-positive')).measurement;
  same(scrolled.content,input.content,row.name);same([scrolled.horizontalOffset*(rtl?-1:1),scrolled.verticalOffset],[input.scroll.left,input.scroll.top],row.name);
  same(scrolled.presenter.bounds,{x:100+(rtl&&!horizontal?6:0),y:100,width:frozen.client.width,height:frozen.client.height},row.name);
  same(initial.verticalBar.visibility,horizontal?'Collapsed':'Visible',row.name);same(initial.horizontalBar.visibility,vertical?'Collapsed':'Visible',row.name);
  if(!horizontal)same(initial.verticalBar.bounds,{x:rtl?100:350,y:100,width:6,height:vertical?240:225},row.name);
  if(!vertical)same(initial.horizontalBar.bounds,{x:100+(rtl&&!horizontal?6:0),y:325,width:horizontal?256:250,height:15},row.name);
  const originalBytes=await fs.readFile(path.join('tests/Reference/ScrollArea',frozenRow.file));assert.equal(hash(originalBytes),frozenRow.pngSha256);const original=readRgbaPng(originalBytes);
  for(const [bar,thumb,enabled] of [[initial.verticalBar,initial.verticalThumb,!horizontal],[initial.horizontalBar,initial.horizontalThumb,!vertical]])if(enabled){
   assert.equal(thumb.visibility,'Visible');const b=bar.bounds,points=[];
   for(let y=b.y;y<b.y+b.height;y++)for(let x=b.x;x<b.x+b.width;x++){const at=(y*800+x)*4;if(original.rgba[at]!==255||original.rgba[at+1]!==255||original.rgba[at+2]!==255)points.push([x,y]);}
   assert(points.length>0);const xs=points.map(p=>p[0]),ys=points.map(p=>p[1]),x=Math.min(...xs),y=Math.min(...ys);same(thumb.bounds,{x,y,width:Math.max(...xs)-x+1,height:Math.max(...ys)-y+1},row.name+' source thumb');sourceThumbs++;
  }
  same([natural.viewport.width,natural.viewport.height,natural.viewportWidth,natural.viewportHeight,natural.content.width,natural.horizontalOffset,natural.verticalOffset],[256,240,horizontal?256:250,240,horizontal?256:250,0,120],row.name);
  same([shrunk.viewport.width,shrunk.viewport.height,shrunk.viewportWidth,shrunk.viewportHeight,shrunk.content.width,shrunk.content.height,shrunk.horizontalOffset,shrunk.verticalOffset],[256,80,256,80,256,80,0,0],row.name);
 }else{
  same(initial.viewport,{x:100,y:100,width:256,height:240},row.name);same([initial.viewportWidth,initial.viewportHeight,initial.extentWidth,initial.extentHeight],[256,240,row.name==='legacy-disabled-axis'?256:520,480],row.name);
  same([scrolled.horizontalOffset,scrolled.verticalOffset],[row.name==='legacy-disabled-axis'?0:80,120],row.name);
  if(row.name==='modern-vertical-content'||row.name==='legacy-disabled-axis')same(natural.content.width,256,row.name);else assert(natural.content.width>520,row.name+' incorrectly constrained natural content.');
  if(row.name.startsWith('legacy'))assert.equal(row.changeAccepted,true);else assert(row.completions.some(r=>r.CorrelationId===row.correlation));
 }
 assert.equal(row.file,row.name+'.png');const bytes=await fs.readFile(path.join(native,row.file));assert.equal(hash(bytes),row.pngSha256);const png=readRgbaPng(bytes);assert.equal(png.width,800);assert.equal(png.height,700);
 const expected=row.name==='legacy-disabled-axis'?[255,0,0,255]:[25,80,120,255],at=(110*800+110)*4;assert.deepEqual(row.ancestorPixel,expected,row.name);assert.deepEqual([...png.rgba.subarray(at,at+4)],expected,row.name);pixels++;
 if(isReserved)for(const [thumb,enabled]of [[scrolled.verticalThumb,!horizontal],[scrolled.horizontalThumb,!vertical]])if(enabled){const b=thumb.bounds,x=Math.floor(b.x+b.width/2),y=Math.floor(b.y+b.height/2),at=(y*800+x)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],[51,102,153,255],row.name+' diagnostic native thumb');pixels++;}
}
const result={passed:true,primitiveOnly:true,cases:10,sourceOrientationDirectionCases:6,geometryComparisons:geometry,sourceThumbBoundsMatched:sourceThumbs,fullClientPngs:10,pixelSamples:pixels,guardChecks:report.guards,referenceSha256:hash(sourceBytes),librarySha256:report.librarySha256,hostSha256:report.hostSha256,baselinePackageSha256:build.packageSha256,modernHiddenAxisAndNaturalWidthVerified:true,reservedGuttersAndRtlVerified:true,nativeScrollAreaImplemented:false,sourceContractVerified:false,nativeComponentAccepted:false};
await fs.writeFile(path.resolve(option('--output')),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));
