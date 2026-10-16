import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert.ok(at>=0&&process.argv[at+1],`Pass ${name} <path>.`);return path.resolve(process.argv[at+1]);};
const source=option('--source'),before=option('--before'),after=option('--after'),output=option('--output');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const bytes=await fs.readFile(new URL('../tests/Reference/DataTableFilters.json',import.meta.url)),reference=JSON.parse(bytes),referenceSha256=hash(bytes);
assert.equal(referenceSha256,'f85940d22b27c187d9c14ae7b2a6ce6fc788fb9e3a52971cf3aff6d2575790d6');
assert.equal(hash(await fs.readFile(path.join(source,'reference.json'))),referenceSha256);
for(const folder of [before,after]){const runtime=await read(path.join(folder,'result.json'));assert.equal(runtime.passed,true);assert.equal(runtime.component,'data-table-filters-native');assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);}
const near=(actual,expected)=>assert.ok(Number.isFinite(actual)&&Math.abs(actual-expected)<=1/32,`${actual}/${expected}`);
let open=0,closed=0,closures=0;const frames=[];
for(const scheme of ['Light','Dark']){
 const old=await read(path.join(before,`data-table-filters-${scheme}.json`)),current=await read(path.join(after,`data-table-filters-${scheme}.json`));
 for(const report of [old,current]){assert.equal(report.passed,true);assert.equal(report.referenceSha256,referenceSha256);assert.deepEqual(report.failures,[]);}
 assert.deepEqual(current.focusDifferences,old.focusDifferences);let index=0;
 for(const sample of reference.cases.filter(item=>item.scheme===scheme)){
  let seenSurface=false;
  for(const point of sample.snapshots){
   const previous=old.reports[index],actual=current.reports[index++],state=point.state;
   assert.equal(previous.id,sample.id);assert.equal(actual.id,sample.id);assert.equal(previous.phase,point.phase);assert.equal(actual.phase,point.phase);assert.deepEqual(previous.errors,[]);assert.deepEqual(actual.errors,[]);
   assert.equal(actual.rows,state.visibleRows);assert.equal(actual.callbacks,state.events.length);assert.equal(actual.outsideClicks,state.outsideClicks);if(point.phase!=='closed')assert.equal(actual.focus,previous.focus);
   let layers=null;
   if(state.popupOpen){
    const geometry=actual.geometry;for(const [field,key]of[['X','x'],['Y','y'],['Width','width'],['Height','height']])near(geometry.popup[field],state.popup.box[key]);
    const shadow=geometry.shadow;assert.ok(shadow);near(shadow.ActualWidth,state.popup.box.width);near(shadow.ActualHeight,state.popup.box.height);assert.equal(shadow.radius,Number.parseFloat(state.popup.style.borderRadius));
    layers=[...state.popup.style.boxShadow.matchAll(/rgba\((\d+),\s*(\d+),\s*(\d+),\s*([\d.]+)\)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+([\d.]+)px\s+(-?[\d.]+)px/g)].map(match=>({R:+match[1],G:+match[2],B:+match[3],Opacity:+match[4],x:+match[5],y:+match[6],BlurRadius:+match[7],spread:+match[8]})).filter(layer=>layer.Opacity>0).sort((a,b)=>b.BlurRadius-a.BlurRadius);
    assert.equal(layers.length,2);assert.equal(shadow.layers.length,2);
    for(const [i,expected]of layers.entries()){const layer=shadow.layers[i];for(const key of ['R','G','B','Opacity','x','y','BlurRadius'])assert.equal(layer[key],expected[key]);assert.equal(layer.sigma,expected.BlurRadius/2);assert.equal(layer.A,255);assert.equal(layer.mask,true);near(layer.insetX,-expected.spread);near(layer.insetY,-expected.spread);near(layer.width,Math.max(0,state.popup.box.width+2*expected.spread));near(layer.height,Math.max(0,state.popup.box.height+2*expected.spread));}
    seenSurface=true;open++;
   }else{if(seenSurface){assert.equal(actual.shadowClosed,true);closures++;}else assert.equal(actual.shadowClosed,null);closed++;}
   const sourceBytes=await fs.readFile(path.join(source,point.file)),oldBytes=await fs.readFile(path.join(before,previous.file)),newBytes=await fs.readFile(path.join(after,actual.file));assert.equal(hash(sourceBytes),point.sha256);
   const target=readRgbaPng(sourceBytes),a=readRgbaPng(oldBytes),b=readRgbaPng(newBytes);assert.deepEqual([a.width,a.height],[target.width,target.height]);assert.deepEqual([b.width,b.height],[target.width,target.height]);
   let previousPixels=0,currentPixels=0,changedNativePixels=0,previousError=0,currentError=0;
   for(let at=0;at<target.rgba.length;at+=4){let oldDiff=false,newDiff=false,changed=false;for(let c=0;c<4;c++){const oldDelta=Math.abs(a.rgba[at+c]-target.rgba[at+c]),newDelta=Math.abs(b.rgba[at+c]-target.rgba[at+c]);previousError+=oldDelta;currentError+=newDelta;oldDiff||=oldDelta!==0;newDiff||=newDelta!==0;changed||=a.rgba[at+c]!==b.rgba[at+c];}if(oldDiff)previousPixels++;if(newDiff)currentPixels++;if(changed)changedNativePixels++;}
   frames.push({id:sample.id,scheme,phase:point.phase,open:state.popupOpen,sourceFile:point.file,sourceSha256:point.sha256,previousFile:previous.file,previousSha256:hash(oldBytes),currentFile:actual.file,currentSha256:hash(newBytes),previousDifferingPixels:previousPixels,currentDifferingPixels:currentPixels,changedNativePixels,previousAbsoluteRgbaError:previousError,currentAbsoluteRgbaError:currentError,layers});
  }
 }
 assert.equal(index,old.reports.length);assert.equal(index,current.reports.length);
}
assert.equal(open,114);assert.equal(closed,162);assert.ok(closures>0);assert.ok(frames.filter(frame=>frame.open).every(frame=>frame.currentAbsoluteRgbaError<frame.previousAbsoluteRgbaError),'Every open frame must reduce the complete client RGBA error.');
const sum=(items,key)=>items.reduce((total,item)=>total+item[key],0),opened=frames.filter(frame=>frame.open),shut=frames.filter(frame=>!frame.open);
const report={format:'orchard-select-shadow-audit-v1',referenceSha256,verifierSha256:hash(await fs.readFile(new URL(import.meta.url))),checkedShadowGatesPassed:true,nativeComponentAccepted:false,sourcePixelParityVerified:frames.every(frame=>frame.currentDifferingPixels===0),comparisonRegion:'entire-client',masks:0,tolerance:0,frames:frames.length,openFrames:open,closedFrames:closed,compositionClosures:closures,nativeLayersVerified:open*2,improvedOpenFrames:opened.length,previousOpenAbsoluteRgbaError:sum(opened,'previousAbsoluteRgbaError'),currentOpenAbsoluteRgbaError:sum(opened,'currentAbsoluteRgbaError'),previousOpenDifferingPixels:sum(opened,'previousDifferingPixels'),currentOpenDifferingPixels:sum(opened,'currentDifferingPixels'),changedClosedFrames:shut.filter(frame=>frame.changedNativePixels>0).length,changedClosedPixels:sum(shut,'changedNativePixels'),previousClosedAbsoluteRgbaError:sum(shut,'previousAbsoluteRgbaError'),currentClosedAbsoluteRgbaError:sum(shut,'currentAbsoluteRgbaError'),reports:frames};
await fs.writeFile(output,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,reports:undefined}));
