import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1],name);return path.resolve(process.argv[at+1]);};
const native=option('--native'),app=option('--app'),packagePath=option('--package'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse(await fs.readFile(path.join(native,file)));
const librarySha256=hash(await fs.readFile(path.join(app,'Orchard.dll'))),hostSha256=hash(await fs.readFile(path.join(app,'PackageConsumer.dll'))),packageSha256=hash(await fs.readFile(packagePath));
for(const [file,expected]of[['Orchard.dll',librarySha256],['PackageConsumer.dll',hostSha256]])assert.equal(hash(await fs.readFile(path.join(native,'app-identity',file))),expected);
const build=await read('result.json'),cleanup=await read('cleanup.json');
assert.equal(build.buildPassed,true);assert.equal(build.runtimeVerified,false);assert.equal(build.platform,'x64');assert.equal(build.packageSha256,packageSha256);assert.equal(cleanup.removed,true);assert.equal(cleanup.isolatedConsumer,build.isolatedConsumer);await assert.rejects(fs.access(cleanup.isolatedConsumer),{code:'ENOENT'});
const suffixes=['actual hidden viewport','zero flow','physical size','unscaled complete raster','ancestor header paint','ancestor menu paint','following body paint','header hit testing','menu hit testing'];
const missing=['ancestor header paint','ancestor menu paint','header hit testing','menu hit testing'];let pixelsChecked=0,checks=0,rejectedChecks=0;
for(const rejected of[false,true]){
 const folder=rejected?'rejected/':'',report=await read(folder+'header-placement.json'),exit=await read(folder+'process-exit.json');
 assert.equal(exit.exited,true);assert.equal(exit.exitCode,rejected?1:0);assert.equal(report.passed,!rejected);assert.equal(report.diagnosticOnly,true);
 assert.equal(report.scope,rejected?'hidden-header-rejected-zero-flow-design-v1':'hidden-header-negative-margin-placement-probe-v1');
 assert.equal(report.processArchitecture,'X64');assert.equal(report.osArchitecture,'X64');assert.equal(report.hostSha256,hostSha256);assert.equal(report.librarySha256,librarySha256);
 for(const field of['activateCalled','showCalled','focusCalled','inputSent','nativeHeaderImplemented','sourceRasterParityVerified','nativeComponentAccepted','visibleRuntimeVerified'])assert.equal(report[field],false,field);
 assert.equal(report.activations,0);assert.equal(report.focus,0);assert.equal(report.guardChecks,rejected?27:18);
 if(rejected)assert.match(report.failure,/Placement contracts failed:/);else assert.equal(report.failure,null);
 const names=rejected?['panel','control','control-fixed']:['margin-control','margin-control-fixed'];
 assert.deepEqual(report.scenarios.map(row=>row.name),names);assert.deepEqual(report.checks.map(row=>row.name),names.flatMap(name=>suffixes.map(suffix=>name+' '+suffix)));
 for(const row of report.checks){const failed=rejected&&missing.some(suffix=>row.name.endsWith(' '+suffix));assert.equal(row.passed,!failed,row.name);if(failed)rejectedChecks++;else checks++;}
 for(const row of report.scenarios){
  const top=row.name.endsWith('-fixed')?0:120;
  const actual=suffix=>report.checks.find(check=>check.name===row.name+' '+suffix).actual;
  const viewport=actual('actual hidden viewport');assert.equal(viewport.IsHostVisible,false);assert.equal(viewport.RasterizationScale,1);assert.deepEqual(viewport.Size,{Width:1280,Height:900,IsEmpty:false});
  assert.deepEqual(actual('zero flow'),{desired:0,followingY:120});assert.deepEqual(actual('physical size'),{X:80,Y:top,ActualWidth:800,ActualHeight:73});assert.deepEqual(actual('unscaled complete raster'),{PixelWidth:1280,PixelHeight:900,Length:4608000});
  for(const suffix of['header hit testing','menu hit testing'])assert.deepEqual(actual(suffix),rejected?['Canvas']:['Border','OverflowPanel','Grid','MarginFlowControl','StackPanel','Canvas']);
  assert.deepEqual(row.root,{x:0,y:0,width:1280,height:900});assert.deepEqual(row.flow,{x:80,y:120,width:400,height:500});assert.equal(row.scale,1);
  assert.deepEqual(row.surface,{x:80,y:top,width:800,height:73});assert.deepEqual(row.menu,{x:80,y:top+72,width:800,height:261});assert.deepEqual(row.following,{x:80,y:120,width:400,height:500});assert.deepEqual(row.desired,{Width:400,Height:0});
  assert.equal(row.raster.file,row.name+'.png');assert.equal(row.raster.width,1280);assert.equal(row.raster.height,900);
  const bytes=await fs.readFile(path.join(native,folder,row.raster.file));assert.equal(hash(bytes),row.raster.pngSha256);
  const image=readRgbaPng(bytes);assert.equal(image.width,1280);assert.equal(image.height,900);const bgra=Buffer.alloc(image.rgba.length);
  for(let y=0;y<900;y++)for(let x=0;x<1280;x++){
   const at=(y*1280+x)*4;let expected=[250,250,250,255];
   if(x>=80&&x<480&&y>=120&&y<620)expected=[20,40,180,255];
   if(!rejected&&x>=80&&x<880&&y>=top&&y<top+72)expected=[210,20,30,255];
   if(!rejected&&x>=80&&x<880&&y>=top+72&&y<top+333)expected=[20,170,70,255];
   for(let channel=0;channel<4;channel++)assert.equal(image.rgba[at+channel],expected[channel],row.name+' pixel '+x+','+y+' channel '+channel);
   bgra[at]=image.rgba[at+2];bgra[at+1]=image.rgba[at+1];bgra[at+2]=image.rgba[at];bgra[at+3]=image.rgba[at+3];pixelsChecked++;
  }
  assert.equal(hash(bgra),row.raster.sha256);
  const pixel=(x,y)=>bgra.subarray((y*1280+x)*4,(y*1280+x)*4+4).toString('hex');
  assert.equal(row.raster.headerPixel,pixel(800,top+20));assert.equal(row.raster.menuPixel,pixel(800,top+160));assert.equal(row.raster.bodyPixel,pixel(100,500));
  assert.equal(actual('ancestor header paint'),row.raster.headerPixel);assert.equal(actual('ancestor menu paint'),row.raster.menuPixel);assert.equal(actual('following body paint'),row.raster.bodyPixel);
 }
}
assert.equal(rejectedChecks,12);
const audit={passed:true,scope:'hidden-header-placement-investigation',positiveScenarios:2,expectedRejectedScenarios:3,checks,rejectedChecks,pixelsChecked,librarySha256,hostSha256,packageSha256,visibleRuntimeVerified:false,nativeHeaderImplemented:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
