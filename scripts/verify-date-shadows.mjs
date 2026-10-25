import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
import {verifyShadowPaintSource,verifyOverlayShadowPaint} from './reference/verify-overlay-shadow-paint-source.mjs';

const option = name => {const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1],name);return path.resolve(process.argv[index+1]);};
const read = async file => JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const source = option('--source'),before = option('--before'),after = option('--after');
const reference = await read(new URL('../tests/Reference/DateControls.json',import.meta.url));
const overlayPaint = await verifyShadowPaintSource(new URL('../tests/Reference/OverlayShadowPaint.json',import.meta.url),new URL('../tests/Reference/OverlayShadowPaint',import.meta.url));
const near = (actual,expected,context) => assert(Number.isFinite(actual)&&Math.abs(actual-expected)<.001,`${context}: ${actual} != ${expected}`);
const layers = css => [...css.matchAll(/rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+([\d.]+)px\s+(-?[\d.]+)px/g)]
  .map(item=>({R:+item[1],G:+item[2],B:+item[3],Opacity:item[4]===undefined?1:+item[4],x:+item[5],y:+item[6],blur:+item[7],spread:+item[8]})).filter(item=>item.Opacity>0&&item.blur>0);
let dayChecks=0,dayShadows=0,triggerChecks=0,triggerShadows=0,popupLayers=0,staleShadowReleases=0;
function shadow(actual,expected,box,paint,context){
  if(!expected){assert.equal(actual,null,context+'/absent');return false;}
  assert(actual,context+'/present'); near(actual.ActualWidth,box.width,context+'/width');near(actual.ActualHeight,box.height,context+'/height');
  near(actual.radius,parseFloat(paint.borderRadius),context+'/radius');near(actual.BlurRadius,expected.blur,context+'/blur');near(actual.sigma,expected.blur/2,context+'/sigma');
  near(actual.x,expected.x,context+'/x');near(actual.y,expected.y,context+'/y');
  for(const channel of ['R','G','B'])assert.equal(actual[channel],expected[channel],context+'/'+channel);assert.equal(actual.A,255,context+'/opaque-tint');
  // Edge serializes the authored .025 opacity as .024. Compare the exact
  // 8-bit alpha represented by those values; the live sprite retains the authored
  // scalar while colour is applied only after the white alpha mask is filtered.
  assert.equal(Math.round(actual.Opacity*255),Math.round(expected.Opacity*255),context+'/alpha-byte');
  near(actual.Opacity,expected.Opacity===.024?.025:expected.Opacity,context+'/authored-opacity');
  near(actual.insetX,-expected.spread,context+'/insetX');near(actual.insetY,-expected.spread,context+'/insetY');
  near(actual.width,Math.max(0,box.width+2*expected.spread),context+'/caster-width');near(actual.height,Math.max(0,box.height+2*expected.spread),context+'/caster-height');
  assert.equal(actual.mask,true,context+'/mask');assert.equal(actual.exteriorMask,true,context+'/exterior');assert.equal(actual.paddedKernel,true,context+'/kernel');return true;
}
const reports=[];
for(const width of [320,640])for(const kind of ['calendar','presentation']){
  const folder=kind==='calendar'?`dates-native-${width}-current`:`dates-presentation-${width}`,json=kind==='calendar'?'date-controls.json':'date-presentation.json';
  for(const root of [before,after]){const result=await read(path.join(root,folder,'result.json'));assert.equal(result.passed,true);assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.animationsEnabled,false);assert.equal(result.environment.highContrast,false);
    const window=await read(path.join(root,folder,'capture-window.json'));if(window.roundedCornersApiAvailable)assert.equal(window.preferenceAfter,1,'Native fixture corners must be square.');}
  const current=await read(path.join(after,folder,json));assert.equal(current.passed,true);
  if(kind==='calendar'){const lifecycle=current.phases.filter(phase=>phase.phase.endsWith('-lifecycle'));assert.equal(lifecycle.length,2);for(const phase of lifecycle){assert.equal(phase.staleShadowReleased,true);staleShadowReleases++;}}
  const expectedFrames=reference.cases.filter(frame=>frame.width===width&&(kind==='calendar'?frame.kind==='calendar':frame.kind!=='calendar'));assert.equal(current.frames.length,expectedFrames.length);
  for(const expected of expectedFrames){
    const actual=current.frames.filter(frame=>frame.id===expected.id&&frame.state===expected.state&&frame.scheme===expected.scheme&&(kind==='calendar'||frame.kind===expected.kind));assert.equal(actual.length,1,expected.file);const frame=actual[0];let addedShadows=0;
    const nativeDays=kind==='calendar'?frame.days:frame.calendar?.days??[];
    for(const day of expected.calendar?.days??[]){
      const found=nativeDays.find(item=>Number(item.day)===day.day);assert(found,expected.file+'/'+day.day);
      const paint=reference.paints[day.paintRef],wanted=layers(paint.boxShadow);assert(wanted.length<=1);
      if(shadow(found.shadow,wanted[0],day.box,paint,expected.file+'/day-'+day.day)){dayShadows++;addedShadows++;}dayChecks++;
    }
    if(kind==='presentation'){
      for(const [index,trigger]of expected.triggers.entries()){
        const paint=reference.paints[trigger.paintRef],wanted=layers(paint.boxShadow);assert(wanted.length<=1);
        if(shadow(frame.triggers[index].shadow,wanted[0],trigger.box,paint,expected.file+'/trigger-'+index)){triggerShadows++;addedShadows++;}triggerChecks++;
      }
      if(expected.popup){
        const wanted=layers(reference.paints[expected.popup.paintRef].boxShadow).sort((a,b)=>b.blur-a.blur),found=frame.popupShadow;assert(found);assert.equal(wanted.length,2);assert.equal(found.layers.length,2);
        near(found.ActualWidth,expected.popup.box.width,'popup width');near(found.ActualHeight,expected.popup.box.height,'popup height');
        for(const [index,layer]of wanted.entries()){
          const actual=found.layers[index];near(actual.BlurRadius,layer.blur,'popup blur');near(actual.sigma,layer.blur/2,'popup sigma');near(actual.x,layer.x,'popup x');near(actual.y,layer.y,'popup y');
          verifyOverlayShadowPaint(actual,layer,overlayPaint.s);assert.equal(actual.mask,true);
          near(actual.insetX,-layer.spread,'popup inset');near(actual.insetY,-layer.spread,'popup inset');popupLayers++;
        }
      }else assert.equal(frame.popupShadow,null);
    }
    const sourceBytes=await fs.readFile(path.join(source,expected.file)),oldBytes=await fs.readFile(path.join(before,folder,expected.file)),newBytes=await fs.readFile(path.join(after,folder,expected.file));
    assert.equal(hash(sourceBytes),expected.pngSha256,expected.file+'/source hash');
    const target=readRgbaPng(sourceBytes),old=readRgbaPng(oldBytes),next=readRgbaPng(newBytes);
    for(const image of [old,next])assert.deepEqual([image.width,image.height],[target.width,target.height]);
    // These pixels stay in the whole-frame comparison. This additional gate
    // rejects desktop leakage at rounded native window corners.
    for(const image of [old,next])for(const [left,top]of [[0,0],[0,image.height-8],[image.width-8,0],[image.width-8,image.height-8]]){
      const innerX=left===0?8:image.width-9,innerY=top===0?8:image.height-9,innerAt=(innerY*image.width+innerX)*4,background=image.rgba.subarray(innerAt,innerAt+4);
      for(let y=top;y<top+8;y++)for(let x=left;x<left+8;x++){const at=(y*image.width+x)*4;assert.deepEqual(image.rgba.subarray(at,at+4),background,expected.file+'/rectangular client corner');}
    }
    assert.deepEqual(next.rgba.subarray(0,next.width*4),target.rgba.subarray(0,target.width*4),expected.file+'/viewport background');
    const viewportBackgroundChanged=!old.rgba.subarray(0,old.width*4).equals(next.rgba.subarray(0,next.width*4));
    let beforeError=0,afterError=0,beforePixels=0,afterPixels=0,changedPixels=0;
    for(let at=0;at<target.rgba.length;at+=4){let a=false,b=false,c=false;for(let channel=0;channel<4;channel++){const x=Math.abs(target.rgba[at+channel]-old.rgba[at+channel]),y=Math.abs(target.rgba[at+channel]-next.rgba[at+channel]);beforeError+=x;afterError+=y;a||=x!==0;b||=y!==0;c||=old.rgba[at+channel]!==next.rgba[at+channel];}beforePixels+=Number(a);afterPixels+=Number(b);changedPixels+=Number(c);}
    reports.push({file:expected.file,kind:expected.kind,scheme:expected.scheme,width,addedShadows,viewportBackgroundChanged,beforeError,afterError,beforeDifferingPixels:beforePixels,afterDifferingPixels:afterPixels,changedPixels,sourceSha256:expected.pngSha256,beforeSha256:hash(oldBytes),afterSha256:hash(newBytes)});
  }
}
assert.equal(reports.length,188);assert.equal(dayChecks,4680);assert.equal(triggerChecks,84);assert.equal(popupLayers,40);
const changed=reports.filter(frame=>frame.addedShadows>0),regressed=changed.filter(frame=>frame.afterError>frame.beforeError),sum=(items,key)=>items.reduce((total,item)=>total+item[key],0);
const result={format:'orchard-date-shadow-audit-v1',passed:regressed.length===0&&sum(changed,'afterError')<sum(changed,'beforeError'),frames:188,dayChecks,dayShadows,triggerChecks,triggerShadows,popupLayers,staleShadowReleases,
  comparedRegion:'entire-client',masks:0,resampling:false,pixelTolerance:0,opacityComparison:'same exact 8-bit alpha; unrounded authored opacity retained',
  shadowFrames:changed.length,improvedShadowFrames:changed.filter(frame=>frame.afterError<frame.beforeError).length,regressedShadowFrames:regressed.map(frame=>frame.file),
  rectangularNativeCornersVerified:true,viewportBackgroundFramesVerified:reports.length,viewportBackgroundChangedFrames:reports.filter(frame=>frame.viewportBackgroundChanged).map(frame=>frame.file),
  beforeShadowAbsoluteRgbaError:sum(changed,'beforeError'),afterShadowAbsoluteRgbaError:sum(changed,'afterError'),
  sameBackdropShadowFrames:changed.filter(frame=>!frame.viewportBackgroundChanged).length,beforeSameBackdropShadowAbsoluteRgbaError:sum(changed.filter(frame=>!frame.viewportBackgroundChanged),'beforeError'),afterSameBackdropShadowAbsoluteRgbaError:sum(changed.filter(frame=>!frame.viewportBackgroundChanged),'afterError'),
  sourceRasterParityVerified:reports.every(frame=>frame.afterDifferingPixels===0),normalMotionVerified:false,nativeComponentAccepted:false,reports};
await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,reports:undefined}));assert.equal(result.passed,true,'Date shadow changes regressed full-frame source error.');
