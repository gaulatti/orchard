import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=n=>{const i=process.argv.indexOf(n);assert(i>=0);return path.resolve(process.argv[i+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const source=await read(new URL('../tests/Reference/RadioGroup.json',import.meta.url)),sourcePngs=option('--source'),errors=[],raster=[];let maximumGeometryError=0,frames=0,options=0;
const near=(a,b,context)=>{const delta=Math.abs(a-b);maximumGeometryError=Math.max(maximumGeometryError,delta);if(!Number.isFinite(delta)||delta>=.001)errors.push({context,actual:a,expected:b,delta});};
const box=(a,b,context)=>{for(const key of ['x','y','width','height'])near(a[key],b[key],context+'/'+key);};
for(const width of [320,640]){
 const folder=option('--native-'+width),runtime=await read(path.join(folder,'result.json')),native=await read(path.join(folder,'radio-reference.json'));assert(runtime.passed&&native.passed);assert.equal(runtime.environment.displayScale,1);assert.equal(runtime.environment.textScale,1);assert.equal(runtime.environment.animationsEnabled,false);assert.equal(runtime.environment.highContrast,false);
 for(const expected of source.cases.filter(frame=>frame.width===width)){
  const actual=native.frames.find(frame=>frame.id===expected.id&&frame.scheme===expected.scheme);assert(actual,expected.file);box(actual.box,expected.measurement.box,expected.file+'/group');assert.equal(actual.options.length,expected.measurement.options.length);
  for(const[index,wanted]of expected.measurement.options.entries()){
   const found=actual.options[index],context=expected.file+'/'+wanted.value;assert.equal(found.Value,wanted.value);assert.equal(found.selected,wanted.checked);assert.equal(found.disabled,wanted.disabled);near(found.opacity,Number(wanted.opacity),context+'/opacity');
   box(found.box,wanted.box,context+'/option');box(found.ring.box,wanted.ring.box,context+'/ring');box(found.dot.box,wanted.dot.box,context+'/dot');near(found.ring.StrokeThickness,parseFloat(wanted.ring.paint.borderWidth),context+'/stroke');
   for(const[nativePaint,css,role]of [[found.ring.stroke,wanted.ring.paint.borderColor,'ring'],[found.dot.fill,wanted.dot.paint.backgroundColor,'dot']])try{assertNativePaint(nativePaint,css,context+'/'+role);}catch(error){errors.push({context:context+'/'+role,message:error.message});}
   for(const role of ['label','description']){assert.equal(found[role]===null,wanted[role]===null);if(!wanted[role])continue;assert(found[role].fontFamily.includes(role==='label'?'EncodeSans-Medium.ttf':'EncodeSans-Regular.ttf'));try{assertNativePaint(found[role].foreground,wanted[role].paint.color,context+'/'+role+'/foreground');}catch(error){errors.push({context:context+'/'+role+'/foreground',message:error.message});}box(found[role].box,wanted[role].box,context+'/'+role);near(found[role].FontSize,parseFloat(wanted[role].paint.fontSize),context+'/'+role+'/font');near(found[role].LineHeight,parseFloat(wanted[role].paint.lineHeight),context+'/'+role+'/line-height');near(found[role].weight,Number(wanted[role].paint.fontWeight),context+'/'+role+'/weight');}
   near(found.dot.scale,wanted.checked?1:0,context+'/dot-scale');options++;
  }
  const bytes=await fs.readFile(path.join(sourcePngs,expected.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),expected.pngSha256);
  const wanted=readRgbaPng(bytes),found=readRgbaPng(await fs.readFile(path.join(folder,expected.file)));assert.deepEqual([found.width,found.height],[wanted.width,wanted.height]);
  let differingPixels=0,absoluteRgbaError=0,maximumChannelDifference=0;
  for(let at=0;at<found.rgba.length;at+=4){let different=false;for(let c=0;c<4;c++){const delta=Math.abs(found.rgba[at+c]-wanted.rgba[at+c]);absoluteRgbaError+=delta;maximumChannelDifference=Math.max(maximumChannelDifference,delta);different||=delta!==0;}differingPixels+=Number(different);}
  if(expected.spec.options.length===0)assert.equal(differingPixels,0,'Empty source/native canvas differs: '+expected.file);
  raster.push({file:expected.file,differingPixels,absoluteRgbaError,maximumChannelDifference});frames++;
 }
}
const result={passed:errors.length===0,frames,options,maximumGeometryError,errors,raster,masks:0,pixelTolerance:0,resampling:false,sourceRasterParityVerified:raster.every(frame=>frame.differingPixels===0),normalMotionVerified:false,nativeComponentAccepted:false};await fs.writeFile(option('--output'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({...result,raster:undefined,errors:errors.slice(0,12)}));assert(result.passed,'Radio source/native contract differs.');
