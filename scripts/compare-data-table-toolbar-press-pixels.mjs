import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const i=process.argv.indexOf(name);if(i<0||!process.argv[i+1])throw Error('Pass '+name+' <path>.');return path.resolve(process.argv[i+1]);};
const sourceDirectory=option('--source'),nativeDirectory=option('--native'),output=option('--output'),hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const referenceBytes=await fs.readFile(new URL('../tests/Reference/DataTableToolbarPress.json',import.meta.url)),source=JSON.parse(referenceBytes),referenceSha256=hash(referenceBytes);
assert.equal(hash(await fs.readFile(path.join(sourceDirectory,'reference.json'))),referenceSha256);
assert.equal(source.scriptSha256,hash(await fs.readFile(new URL('./reference/capture-data-table-toolbar-press.cjs',import.meta.url))));
const audit=await read(path.join(nativeDirectory,'press-audit.json'));assert.equal(audit.checkedGatesPassed,true);assert.equal(audit.referenceSha256,referenceSha256);assert.equal(audit.states,54);assert.equal(audit.captures,56);
const runtime=await read(path.join(nativeDirectory,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-toolbar-press');assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
const images=new Map();for(const item of source.results)for(const point of item.states){const bytes=await fs.readFile(path.join(sourceDirectory,point.file));assert.equal(hash(bytes),point.sha256);const png=readRgbaPng(bytes);assert.equal(png.width,800);assert.equal(png.height,36);images.set(point.file,png);}
assert.equal(images.size,108);const pairs=[];
for(const scheme of ['Light','Dark']){
 const report=await read(path.join(nativeDirectory,`data-table-toolbar-press-${scheme}.json`));assert.equal(report.passed,true);assert.equal(report.referenceSha256,referenceSha256);
 const expected=source.results.filter(item=>item.scheme===scheme&&item.reducedMotion==='reduce').flatMap(item=>item.states.map(point=>({id:item.id,...point})));
 for(const [index,point]of expected.entries()){
  const actual=report.reports[index];assert.equal(actual.id,point.id);assert.equal(actual.phase,point.state.phase);assert.deepEqual(actual.errors,[]);
  const bytes=await fs.readFile(path.join(nativeDirectory,actual.file)),native=readRgbaPng(bytes),browser=images.get(point.file),crop=actual.surface;
  assert.equal(native.width,1024);assert.equal(native.height,745);assert.deepEqual([crop.width,crop.height],[800,36]);assert.equal(crop.x,Math.round(crop.x));assert.equal(crop.y,Math.round(crop.y));
  let differentPixels=0,maxChannel=0;for(let y=0;y<36;y++)for(let x=0;x<800;x++){const a=(y*800+x)*4,b=((y+crop.y)*native.width+x+crop.x)*4;let changed=false;for(let channel=0;channel<4;channel++){const delta=Math.abs(browser.rgba[a+channel]-native.rgba[b+channel]);changed||=delta!==0;maxChannel=Math.max(maxChannel,delta);}if(changed)differentPixels++;}
  pairs.push({id:point.id,scheme,phase:actual.phase,sourceFile:point.file,sourceSha256:point.sha256,nativeFile:actual.file,nativeSha256:hash(bytes),crop,differentPixels,maxChannel,masks:0,tolerance:0});
 }
}
assert.equal(pairs.length,54);const exactPairs=pairs.filter(item=>item.differentPixels===0).length,passed=exactPairs===pairs.length;
await fs.writeFile(output,JSON.stringify({format:'orchard-data-table-toolbar-press-pixels-v1',referenceSha256,sourceCapturesVerified:images.size,nativeComponentAccepted:false,exactPairs,passed,masks:0,tolerance:0,pairs},null,2)+'\n');console.log(JSON.stringify({pairs:pairs.length,exactPairs,passed,minDifferentPixels:Math.min(...pairs.map(item=>item.differentPixels)),maxDifferentPixels:Math.max(...pairs.map(item=>item.differentPixels))}));if(!passed)process.exitCode=1;
