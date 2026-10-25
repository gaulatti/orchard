import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {assertNativePaint} from './reference/css-paint.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';

const option=name=>{const at=process.argv.indexOf(name);assert(at>=0&&process.argv[at+1]);return path.resolve(process.argv[at+1]);};
const read=async file=>JSON.parse((await fs.readFile(file,'utf8')).replace(/^\uFEFF/,''));
const native=option('--native'),source=option('--source'),reference=await read(new URL('../tests/Reference/Panel.json',import.meta.url));
const result=await read(path.join(native,'result.json')),report=await read(path.join(native,'panel.json'));
assert(result.passed&&report.passed&&result.component==='panel');assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);
const key=row=>row.id+'/'+row.scheme;assert.equal(reference.cases.length,266);assert.deepEqual(report.frames.map(key).sort(),reference.cases.map(key).sort());
const errors=[],raster=[];let maximumGeometryError=0;
const near=(actual,expected,context)=>{assert(Number.isFinite(actual)&&Number.isFinite(expected),context);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context,actual,expected,error});};
const box=(a,b,context)=>{assert.equal(a===null,b===null,context+'/presence');if(b)for(const field of ['x','y','width','height'])near(a[field],b[field],context+'/'+field);};
for(const row of reference.cases){
 const frame=report.frames.find(frame=>key(frame)===key(row)),a=frame.measurement,b=row.measurement,context=key(row);
 for(const field of ['panel','header','row','dot','drag','filter','toolbar','feed'])box(a[field],b[field],context+'/'+field);
 box(a.title.box,b.title.box,context+'/title');near(a.title.minimumWidth,b.title.minimumWidth,context+'/title/minimum');near(a.title.naturalWidth,b.title.naturalWidth,context+'/title/natural');
 assert.equal(a.title.value,b.title.value);assert.equal(a.title.painted.replace(/\s+/g,' '),b.title.value.toUpperCase().replace(/\s+/g,' '));
 assert.equal(a.title.painted?a.title.painted.split('\n').length:0,b.title.box.height/parseFloat(b.title.lineHeight));
 assert.equal(a.title.fontSize,11);assert.equal(a.title.lineHeight,16.5);assert.equal(a.title.tracking,100);assert(a.title.fontFamily.includes('EncodeSans-SemiBold.ttf'));
 assert.equal(a.count===null,b.count===null,context+'/count/presence');if(b.count){box(a.count.box,b.count.box,context+'/count');assert.equal(a.count.value,b.count.value);}
 assert.deepEqual(Object.keys(a.bodies).sort(),Object.keys(b.bodies).sort());for(const name of Object.keys(b.bodies))box(a.bodies[name],b.bodies[name],context+'/body/'+name);
 for(const field of ['offset','clientWidth','clientHeight','extent'])near(a.scroll[field],b.scroll[field],context+'/scroll/'+field);
 for(const flag of ['passive','group','contentRetained','filterRetained','toolbarRetained','dragRetained'])assert.equal(a[flag],true,context+'/'+flag);
 assert.equal(a.name,a.title.value);assert.equal(a.viewportFocusable,b.scroll.extent>b.scroll.clientHeight&&b.feed.height>0,context+'/viewport-keyboard');
 const paintPairs=[['headerBackground',b.paint.header.background],['headerBorder',b.paint.header.border],['title',b.paint.title.color],['left',b.paint.left],['right',b.paint.right],['feedBackground',b.paint.feed.background],['dot',b.paint.dot.background]];
 if(b.count)paintPairs.push(['countBackground',b.paint.count.background],['countBorder',b.paint.count.border],['count',b.paint.count.color]);
 if(b.toolbar)paintPairs.push(['toolbarBackground',b.paint.toolbar.background],['toolbarBorder',b.paint.toolbar.border]);
 for(const [name,css]of paintPairs)assertNativePaint(a.paint[name],css,context+'/'+name);assert.equal(a.paint.dotOpacity,Math.fround(Number(b.paint.dot.opacity)));
}
assert.equal(report.interactions.length,2);
for(const theme of ['Light','Dark']){
 const row=report.interactions.find(row=>row.theme===theme),browser=reference.interactions.find(row=>row.scheme===theme);assert(row);
 for(const flag of ['passed','inputsRetained','countBindingRetained','sourceTabOrder','liveScrollRetained','templateReplacement','remount','originalTemplateValues','selectorRetained','metadataPrecedence','absentSlots','viewportKeyboard','consumerFocusRestored','intrinsicWidth'])assert.equal(row[flag],true,theme+'/'+flag);
 assert.equal(row.actions,2);assert.equal(row.handles,1);assert.notEqual(row.filterValue,'Filter value');assert.notEqual(row.bodyValue,'Body value');assert(row.scrollOffset>0&&row.pageDown>0);assert.equal(row.home,0);
 assertNativePaint(row.idleHandlePaint,browser.handleIdle.color,theme+'/handle-idle');assertNativePaint(row.hoverHandlePaint,browser.handleHover.color,theme+'/handle-hover');
}
if(errors.length){await fs.writeFile(option('--output'),JSON.stringify({passed:false,frames:report.frames.length,maximumGeometryError,errors},null,2)+'\n');assert.fail('Panel geometry differs.');}
assert.deepEqual(Object.keys(report.captures).sort(),reference.cases.filter(row=>row.file).map(row=>row.file).sort());
for(const row of reference.cases.filter(row=>row.file)){
 const bytes=await fs.readFile(path.join(source,row.file));assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),row.pngSha256);
 const actualBytes=await fs.readFile(path.join(native,row.file));assert.equal(crypto.createHash('sha256').update(actualBytes).digest('hex'),report.captures[row.file],'Native capture differs from its recorded image.');
 const expected=readRgbaPng(bytes),actual=readRgbaPng(actualBytes);assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);
 let differingPixels=0,absoluteRgbaError=0;for(let i=0;i<expected.rgba.length;i+=4){let different=false;for(let c=0;c<4;c++){const error=Math.abs(actual.rgba[i+c]-expected.rgba[i+c]);different||=error>0;absoluteRgbaError+=error;}differingPixels+=Number(different);}raster.push({file:row.file,differingPixels,absoluteRgbaError});
}
const audit={passed:true,frames:report.frames.length,maximumGeometryError,errors,realInputThemes:2,keyboardViewportThemes:2,sourceMotionSamples:reference.motion.length,raster,
 sourceRasterParityVerified:raster.every(row=>row.differingPixels===0),masks:0,resampling:false,pixelTolerance:0,nativeNormalMotionVerified:false,nativeArm64RuntimeVerified:false,actualScalingVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify({...audit,raster:undefined,differingPixels:raster.reduce((sum,row)=>sum+row.differingPixels,0)}));
