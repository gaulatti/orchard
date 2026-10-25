import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { fileURLToPath,pathToFileURL } from 'node:url';
import { readRgbaPng } from './reference/read-rgba-png.mjs';
import { cssRgba } from './reference/css-paint.mjs';
import { verifyHeroCarouselPaint } from './reference/verify-hero-carousel-source.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
export function verifyHeroCarouselNative(inputFolder,hostAssembly,output){
const folder=path.resolve(inputFolder);
const counts={states:0,contracts:0,pngs:0,pngPixels:0,pngPaletteProbes:0,geometryValues:0,paletteValues:0,originalBindings:0,imageBindings:0,backdropBindings:0,tagShadowBindings:0,backdropChangedPixels:0,tagShadowChangedPixels:0};
const geometryMismatches=[];
const box=(actual,expected,label)=>{for(const key of ['x','y','width','height']){assert(Number.isFinite(actual?.[key]),label+'/'+key);const difference=Math.abs(actual[key]-expected[key]);if(difference>1/1024)geometryMismatches.push({label:label+'/'+key,actual:actual[key],expected:expected[key],difference});counts.geometryValues++;}};
const normalized=text=>text.replace(/\s+/gu,' ').trim();
const paint=(actual,css,label)=>{const polar=/^oklch\(([^)]+)\)$/.exec(css);if(polar){const [l,c,h,alpha=1]=polar[1].split(/[\s/]+/).map(Number);css=`oklab(${l} ${c*Math.cos(h*Math.PI/180)} ${c*Math.sin(h*Math.PI/180)} / ${alpha})`;}const rgba=cssRgba(css);assert.deepEqual([actual.r,actual.g,actual.b],rgba.slice(0,3),label+'/rgb');assert(Math.abs(actual.a/255*actual.opacity-rgba[3])<.000001,label+'/alpha');counts.paletteValues++;};
try {
  assert(['Orchard.Catalog.dll','PackageConsumer.dll'].includes(hostAssembly));
  const referencePath=path.join(folder,'source-reference.json');
  const referenceHash='1a16b5f6698507791b3f7a396eafe145a8432cc32f858b0654af698aca261227';
  assert.equal(hash(referencePath),referenceHash);
  assert.equal(hash(path.join(root,'tests/Reference/HeroCarousel.json')),referenceHash);
  const reference=read(referencePath),report=read(path.join(folder,'hero-carousel.json'));
  assert.equal(report.format,'orchard-hero-carousel-native-v5');assert.equal(report.passed,true);
  for(const field of ['hardwareInputSent','physicalInputVerified','forcedTemplateStatesArePhysicalInput','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[field],false,field);
  assert.equal(report.focus,0);assert.equal(report.activations,0);assert(report.guards>100);
  assert.equal(report.processArchitecture,'X64');
  assert.equal(report.osArchitecture,'X64');
  const peMachine=file=>{const bytes=fs.readFileSync(file),offset=bytes.readUInt32LE(0x3c);assert.equal(bytes.subarray(offset,offset+4).toString('hex'),'50450000');return bytes.readUInt16LE(offset+4);};
  assert.equal(peMachine(path.join(folder,'app-identity',hostAssembly.replace(/\.dll$/,'.exe'))),0x8664);
  assert.equal(peMachine(path.join(folder,'app-identity/Microsoft.UI.Xaml.dll')),0x8664);
  assert.equal(hash(path.join(folder,'app-identity/Orchard.dll')),report.librarySha256);
  assert.equal(hash(path.join(folder,'app-identity',hostAssembly)),report.hostSha256);
  const processResult=read(path.join(folder,'process-exit.json'));assert.equal(processResult.exited,true);assert.equal(processResult.exitCode,0);assert.equal(processResult.timedOut,false);
  const expected=reference.cases.filter(c=>!c.id.endsWith('focus'));
  assert.equal(report.states.length,82);assert.equal(expected.length,82);
  assert.equal(new Set(report.states.map(s=>s.scheme+'/'+s.id)).size,82);
  for(const source of expected){
    const state=report.states.find(s=>s.id===source.id&&s.scheme===source.scheme);assert(state,`Missing ${source.scheme}/${source.id}`);
    const section=source.measurement.sections[0],snapshot=state.snapshot;
    box(snapshot.box,section.box,source.scheme+'/'+source.id+'/host');
    assert.equal(snapshot.slides.length,section.slides.length);
    for(const [index,slide] of snapshot.slides.entries()){
      const original=section.slides[index],label=source.scheme+'/'+source.id+'/slide'+index;
      box(slide.box,original.box,label);box(slide.link.box,original.link.box,label+'/link');
      assert.equal(slide.link.href,original.link.href);assert.equal(slide.link.name,original.link.name);assert.equal(slide.link.external,source.spec.items?!!source.spec.items[index].external:reference.cases.find(c=>c.scheme===source.scheme&&c.id==='multiple').measurement.sections[0].slides[index].link.target==='_blank');
      assert.equal(slide.opacity,original.paint.opacity);
      for(const name of ['title','description','kicker']){assert.equal(normalized(slide[name].text),normalized(name==='kicker'?original[name].text.toUpperCase():original[name].text),label+'/'+name+'/text');box(slide[name].box,original[name].box,label+'/'+name);assert.equal(slide[name].fontSize,parseFloat(original[name].paint.fontSize));assert.equal(slide[name].lineHeight,parseFloat(original[name].paint.lineHeight));assert.equal(slide[name].fontWeight,Number(original[name].paint.fontWeight));paint(slide[name].foreground,original[name].paint.color,label+'/'+name);}
      assert.equal(slide.cta.text,original.cta.text);box(slide.cta.box,original.cta.box,label+'/cta');
      paint(slide.cta.foreground,original.cta.paint.color,label+'/cta/foreground');paint(slide.cta.background,original.cta.paint.background,label+'/cta/background');
      assert.equal(slide.tags.length,original.tags.length);for(const [tagIndex,tag]of slide.tags.entries()){assert.equal(tag.text,original.tags[tagIndex].text);box(tag.box,original.tags[tagIndex].box,label+'/tag'+tagIndex);}
      assert.equal(slide.originalRetained,true);assert.equal(slide.contextOriginalRetained,true);counts.originalBindings++;
      assert.equal(slide.active,index===snapshot.currentIndex);assert.equal(slide.inputEnabled,true);assert.equal(slide.hitTestVisible,true);assert.equal(slide.link.canExecute,true);
      assert.equal(slide.image.originalSourceRetained,true);assert.equal(slide.image.stretch,'UniformToFill');
      assert.equal(slide.image.sourceRasterParityVerified,false);
      if(slide.image.hasSource){assert.equal(slide.image.graphMounted,true);assert.deepEqual(slide.image.bindings.map(b=>b.name),['original','vertical','horizontal','tint','grain','vignette']);for(const binding of slide.image.bindings){assert.equal(binding.sourceVisualRetained,true);assert(binding.width>0&&binding.height>0);assert.equal(binding.offsetX,binding.name==='original'?Math.max(0,(binding.sourceWidth-binding.width)/2):0);assert.equal(binding.offsetY,binding.name==='original'?Math.max(0,(binding.sourceHeight-binding.height)/2):0);counts.imageBindings++;}}
    }
    assert.equal(snapshot.buttons.length,section.buttons.length);
    for(const button of snapshot.buttons){const original=section.buttons.find(b=>b.name===button.name);assert(original);box(button.box,original.box,source.scheme+'/'+source.id+'/'+button.name);assert.equal(button.opacity,original.paint.opacity);if(original.indicator){assert(button.indicator);box(button.indicator.box,original.indicator.box,source.scheme+'/'+source.id+'/'+button.name+'/dot');paint(button.indicator.foreground,original.indicator.paint.background,source.scheme+'/'+source.id+'/'+button.name+'/paint');}}
    counts.states++;
  }
  fs.writeFileSync(path.join(folder,'hero-carousel-geometry-differences.json'),JSON.stringify({geometryValues:counts.geometryValues,tolerance:1/1024,mismatches:geometryMismatches},null,2)+'\n');
  const contracts=['original-command-parameter','inactive-input-with-hidden-peer','owning-window-arrow-boundary','native-button-invocation','numeric-index-retained-through-shrink','retained-original-editor-and-reorder','removed-key-revokes-context','template-replacement-revokes-context','native-hover-pause-and-resume','unload-retires-timer-and-input','disabled-hidden-owner','original-empty-content-and-peer','original-live-image-failure-cleanup','live-theme-retained','caller-font-family-live-restore','caller-foreground-live-restore','backdrop-tag-shadow-rendering'];
  assert.equal(report.contracts.length,contracts.length*2);
  for(const scheme of ['Light','Dark'])for(const id of contracts){const matches=report.contracts.filter(c=>c.scheme===scheme&&c.id===id);assert.equal(matches.length,1);assert.equal(matches[0].passed,true);counts.contracts++;}
  assert.equal(report.customizations.length,2);
  for(const scheme of ['Light','Dark']){
    const inactive=report.contracts.find(c=>c.scheme===scheme&&c.id==='inactive-input-with-hidden-peer').details;
    for(const key of ['inactiveEnabled','inactiveTabEligible','inactiveNativeInvoke','originalInactiveItemRetained'])assert.equal(inactive[key],true,key);assert.equal(inactive.automationChildren,6);
    const input=report.contracts.find(c=>c.scheme===scheme&&c.id==='owning-window-arrow-boundary').details;
    for(const key of ['owningRootRetained','outsideArrows','wrapping','contenteditableAdvances','unrelatedKeysIgnored','independentReceiversAdvance','unloadDetaches','detachedCallbackIgnored','remountRestores','inactiveHitTestEligible','productionBoundaryInvokedDirectly'])assert.equal(input[key],true,key);
    assert.deepEqual(input.ignoredInputTypes,['TextBox','PasswordBox','CheckBox','RadioButton','Slider','ToggleSwitch','BleeckerFileInput','BleeckerSwitch','BleeckerSlider','BleeckerRadioGroup','BleeckerOtpInput']);
    assert.equal(input.inactiveHitTestSlideIds[0],'third');assert.equal(input.emptyHitOriginalItem,'third');assert.equal(input.visibleContentHitOriginalItem,'first');assert.equal(input.hitTargetNativeInvokesVerified,true);assert.equal(input.physicalRoutingVerified,false);assert.equal(input.tabTraversalVerified,false);
    const remount=report.contracts.find(c=>c.scheme===scheme&&c.id==='unload-retires-timer-and-input').details;for(const key of ['noLateTicks','oldCommandRevoked','navigationRestored','remountedArrowInvoked'])assert.equal(remount[key],true,key);assert.equal(remount.remountIndex,0);
    const fonts=report.contracts.find(c=>c.scheme===scheme&&c.id==='caller-font-family-live-restore').details;
    for(const key of ['retainedText','editorRetained','emptyContentVerified','styleRestored','templateHeldUnchanged'])assert.equal(fonts[key],true,key);
    assert.equal(fonts.preparedSlots,6);assert.equal(fonts.initialFonts.length,6);assert.deepEqual(fonts.restoredFonts,fonts.initialFonts);
    for(const [key,family]of [['boundFonts','Segoe UI'],['updatedFonts','Consolas']]){assert.equal(fonts[key].length,6);fonts[key].forEach((font,i)=>{assert.equal(font.family,family,'caller font family');assert.equal(font.weight,fonts.initialFonts[i].weight);assert.equal(font.size,fonts.initialFonts[i].size);assert.equal(font.name,fonts.initialFonts[i].name);});}
    assert(fonts.initialFonts.find(f=>f.name==='HeroTitleText').family.endsWith('EncodeSans-SemiBold.ttf#Encode Sans'));
    const ink=report.contracts.find(c=>c.scheme===scheme&&c.id==='caller-foreground-live-restore').details;
    for(const key of ['originalGradientRetained','hoverRetained','gradientMutationRetained','boundReplacementRetained','originalSolidRetained','semanticColorRestored'])assert.equal(ink[key],true,key);
    const frames=report.customizations.filter(c=>c.scheme===scheme);assert.equal(frames.length,1);const frame=frames[0];for(const file of [frame.fontFile,frame.beforeInk,frame.afterInk])assert.equal(path.basename(file),file);
    const before=readRgbaPng(fs.readFileSync(path.join(folder,frame.beforeInk))),after=readRgbaPng(fs.readFileSync(path.join(folder,frame.afterInk)));assert.equal(before.width,after.width);assert.equal(before.height,after.height);
    const b=frame.titleBox;for(const value of Object.values(b))assert(Number.isFinite(value));assert(b.x>=0&&b.y>=0&&b.width>0&&b.height>0&&b.x+b.width<=before.width&&b.y+b.height<=before.height);
    let visibleInkChanges=0;for(let y=Math.ceil(b.y);y<Math.floor(b.y+b.height);y++)for(let x=Math.ceil(b.x);x<Math.floor(b.x+b.width);x++){const offset=(y*before.width+x)*4;const max=Math.max(...[0,1,2].map(c=>Math.abs(before.rgba[offset+c]-after.rgba[offset+c])));if(max>=64)visibleInkChanges++;}
    assert(visibleInkChanges>0,'caller brush mutation did not change rendered title pixels');
  }
  assert.equal(report.effects.length,2);
  for(const scheme of ['Light','Dark']){
    const frames=report.effects.filter(e=>e.scheme===scheme);assert.equal(frames.length,1);const frame=frames[0];for(const key of ['removedTagReleased','replacedArrowReleased','unloadedGraphsReleased'])assert.equal(frame[key],true,key);assert.equal(frame.physicalInputVerified,false);assert.equal(frame.sourceRasterParityVerified,false);assert.equal(frame.tagShadowSuppression,'sprite-opacity-zero');
    assert.equal(frame.backdrops.length,9);for(const b of frame.backdrops){assert.equal(b.liveBackdrop,true);assert.equal(b.sigma,2);assert.equal(b.clipType,'path');assert.deepEqual(b.box,b.targetBox);counts.backdropBindings++;}assert.equal(frame.tagShadows.length,3);for(const s of frame.tagShadows){assert.equal(s.originalGlyphRetained,true);assert.equal(s.layers,1);assert.equal(s.blurSigma,2);counts.tagShadowBindings++;}
    for(const file of [frame.allEffects,frame.noBackdrops,frame.noTagShadows,frame.shadowPlacement,frame.shadowMask])assert.equal(path.basename(file),file);
    const on=readRgbaPng(fs.readFileSync(path.join(folder,frame.allEffects))),off=readRgbaPng(fs.readFileSync(path.join(folder,frame.noBackdrops))),shadowOff=readRgbaPng(fs.readFileSync(path.join(folder,frame.noTagShadows)));
    const changed=(other,b,threshold)=>{assert.equal(on.width,other.width);assert.equal(on.height,other.height);assert(b.width>0&&b.height>0&&b.x>=0&&b.y>=0&&b.x+b.width<=on.width&&b.y+b.height<=on.height);let n=0;for(let y=Math.ceil(b.y);y<Math.floor(b.y+b.height);y++)for(let x=Math.ceil(b.x);x<Math.floor(b.x+b.width);x++){const i=(y*on.width+x)*4;if(Math.max(...[0,1,2].map(c=>Math.abs(on.rgba[i+c]-other.rgba[i+c])))>=threshold)n++;}return n;};
    assert.deepEqual(frame.regions.map(r=>r.role),['cta','tag','tag','arrow','arrow','navigation']);for(const region of frame.regions){const n=changed(off,region.box,8);assert(n>0,region.role+' backdrop did not change rendered stripe pixels');counts.backdropChangedPixels+=n;}
    const placement=readRgbaPng(fs.readFileSync(path.join(folder,frame.shadowPlacement))),mask=readRgbaPng(fs.readFileSync(path.join(folder,frame.shadowMask)));
    assert.equal(frame.shadowRegions.length,2);for(const region of frame.shadowRegions){assert(changed(placement,region,64)>0,'tag shadow placement did not reach captured pixels');assert(changed(mask,region,4)>0,'tag native glyph mask did not reach captured pixels');const n=changed(shadowOff,region,4);assert(n>0,'tag shadow did not change rendered pixels');counts.tagShadowChangedPixels+=n;}
    for(const key of ['originalTextRetained','originalGlyphSurfaceRetained','fourCornerClipUpdated','resizedRtlBoundsMatched','emptyTagReleased','restoredTagMounted','emptyTitleReleased','restoredTitleMounted'])assert.equal(frame.live[key],true,key);
    const liveOn=readRgbaPng(fs.readFileSync(path.join(folder,frame.live.on))),liveOff=readRgbaPng(fs.readFileSync(path.join(folder,frame.live.off)));assert.equal(frame.live.regions.length,2);for(const b of frame.live.regions){let n=0;for(let y=Math.ceil(b.y);y<Math.floor(b.y+b.height);y++)for(let x=Math.ceil(b.x);x<Math.floor(b.x+b.width);x++){const i=(y*liveOn.width+x)*4;if(Math.max(...liveOn.rgba.subarray(i,i+4).map((value,c)=>Math.abs(value-liveOff.rgba[i+c])))>=4)n++;}assert(n>0,'live font/text tag shadow did not change pixels');}
  }
  assert.equal(report.catalog.length,hostAssembly==='Orchard.Catalog.dll'?2:0);
  if(report.catalog.length){assert.deepEqual(report.catalog.map(c=>c.scheme),['Light','Dark']);for(const c of report.catalog){assert.equal(c.publicStylesVerified,5);assert.equal(c.originalItemCount,3);assert.equal(c.callerCustomizationVerified,true);}}
  const files=new Set([...report.states.map(s=>s.file),...report.catalog.map(c=>c.file),...report.customizations.flatMap(c=>[c.fontFile,c.beforeInk,c.afterInk]),...report.effects.flatMap(c=>[c.allEffects,c.noBackdrops,c.noTagShadows,c.shadowPlacement,c.shadowMask,c.live.on,c.live.off])]);
  assert.equal(report.pngs.length,files.size);
  for(const png of report.pngs){assert.equal(png.stableConsecutiveFrames,2);assert(Number.isInteger(png.samples)&&png.samples>=2&&png.samples<=6);assert(files.delete(png.file));assert.equal(path.basename(png.file),png.file);const file=path.join(folder,png.file);assert.equal(hash(file),png.sha256);const decoded=readRgbaPng(fs.readFileSync(file));assert.equal(decoded.width,png.width);assert.equal(decoded.height,png.height);const state=report.states.find(s=>s.file===png.file);const source=state?reference.cases.find(c=>c.id===state.id&&c.scheme===state.scheme):null;assert.equal(png.width,source?.spec.viewportWidth??1000);assert.equal(png.height,760);if(source)counts.pngPaletteProbes+=verifyHeroCarouselPaint(source,decoded);counts.pngs++;counts.pngPixels+=png.width*png.height;}
  assert.equal(files.size,0);
  assert.equal(geometryMismatches.length,0,'Source/native captured geometry differs; inspect hero-carousel-geometry-differences.json');
  const result={format:'orchard-hero-carousel-native-audit-v1',passed:true,...counts,reportSha256:hash(path.join(folder,'hero-carousel.json')),librarySha256:report.librarySha256,hostSha256:report.hostSha256,sourceReferenceSha256:referenceHash,hiddenBoundaryVerified:true,forcedTemplateStatesVerified:true,physicalInputVerified:false,capturedGeometryVerified:true,fullGeometryVerified:false,fullPaintVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
  fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');return result;
}catch(error){fs.writeFileSync(output,JSON.stringify({format:'orchard-hero-carousel-native-audit-v1',passed:false,...counts,error:error.stack},null,2)+'\n');throw error;}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href){
  const args=process.argv.slice(2);const value=name=>{const index=args.indexOf(name);assert(index>=0&&args[index+1],`Missing ${name}`);return args[index+1];};
  console.log(JSON.stringify(verifyHeroCarouselNative(value('--native'),value('--host-assembly'),value('--output'))));
}
