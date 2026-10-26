import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {verifyHeroCarouselNative} from './verify-hero-carousel.mjs';

const args=process.argv.slice(2),option=name=>{const i=args.indexOf(name);assert(i>=0&&args[i+1],name);return args[i+1];};
const gold=path.resolve(option('--native')),host=option('--host-assembly'),output=path.resolve(option('--output')),casesRoot=output+'.cases';
assert(!fs.existsSync(casesRoot),'Use a fresh counterexample output');
const reference=JSON.parse(fs.readFileSync(path.join(gold,'hero-carousel.json'),'utf8'));
assert.equal(verifyHeroCarouselNative(gold,host,output+'.baseline.json').passed,true);
fs.mkdirSync(casesRoot,{recursive:true});
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const first=report=>report.states.find(s=>s.id==='single-project'&&s.scheme==='Light').snapshot.slides[0];
const multi=report=>report.states.find(s=>s.id==='multiple'&&s.scheme==='Light').snapshot;
const image=report=>report.states.find(s=>s.id==='image-landscape'&&s.scheme==='Light').snapshot.slides[0].image;
const mutations=[
 ['missing native state',r=>r.states.pop()],['duplicate state',r=>r.states[1]=structuredClone(r.states[0])],
 ['invented acceptance',r=>r.nativeComponentAccepted=true],['invented raster equivalence',r=>r.sourceRasterParityVerified=true],
 ['invented physical input',r=>r.physicalInputVerified=true],['forced template state called physical',r=>r.forcedTemplateStatesArePhysicalInput=true],
 ['hardware input sent',r=>r.hardwareInputSent=true],['focus escaped hidden boundary',r=>r.focus=1],['activation escaped hidden boundary',r=>r.activations=1],
 ['unverified architecture',r=>r.processArchitecture='Arm64'],['different library',r=>r.librarySha256='0'.repeat(64)],['different executed host',r=>r.hostSha256='0'.repeat(64)],
 ['original item replaced',r=>first(r).originalRetained=false],['original context replaced',r=>first(r).contextOriginalRetained=false],
 ['original href normalized',r=>first(r).link.href='/invented'],['external intent lost',r=>multi(r).slides[1].link.external=false],
 ['inactive slide disabled',r=>multi(r).slides[1].inputEnabled=false],['inactive slide unhittable',r=>multi(r).slides[1].hitTestVisible=false],['inactive command disabled',r=>multi(r).slides[1].link.canExecute=false],
 ...['owningRootRetained','outsideArrows','wrapping','contenteditableAdvances','unrelatedKeysIgnored','independentReceiversAdvance','unloadDetaches','detachedCallbackIgnored','remountRestores','inactiveHitTestEligible'].map(key=>['window input '+key,r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details[key]=false]),
 ['inactive native invoke omitted',r=>r.contracts.find(c=>c.id==='inactive-input-with-hidden-peer').details.inactiveNativeInvoke=false],
 ['input exclusion lost',r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details.ignoredInputTypes.pop()],
 ['invented physical key routing',r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details.physicalRoutingVerified=true],
 ['inactive empty hit silently repaired',r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details.emptyHitOriginalItem='first'],
 ['visible content hit stolen',r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details.visibleContentHitOriginalItem='third'],
 ['hit target invokes omitted',r=>r.contracts.find(c=>c.id==='owning-window-arrow-boundary').details.hitTargetNativeInvokesVerified=false],
 ['title silently shortened',r=>first(r).title.text='Shortened'],['RTL source position changed',r=>r.states.find(s=>s.id==='rtl'&&s.scheme==='Light').snapshot.slides[2].kicker.box.x+=.015625],
 ['responsive height clamped',r=>r.states.find(s=>s.id==='font200'&&s.scheme==='Light').snapshot.box.height=760],
 ['wrong title color',r=>first(r).title.foreground.g=0],['fractional CTA opacity lost',r=>first(r).cta.background.opacity=1],
 ['original ImageSource replaced',r=>image(r).originalSourceRetained=false],['live image graph removed',r=>image(r).graphMounted=false],
 ['image overlay dropped',r=>image(r).bindings.pop()],['image source binding substituted',r=>image(r).bindings[0].sourceVisualRetained=false],['cropping lost',r=>image(r).stretch='Fill'],
 ['hidden hover arrows',r=>r.states.find(s=>s.id==='multiple-hover'&&s.scheme==='Light').snapshot.buttons[0].opacity=0],
 ['missing lifecycle contract',r=>r.contracts.pop()],['failed lifecycle contract',r=>r.contracts[0].passed=false],
 ['caller font ignored',r=>r.contracts.find(c=>c.id==='caller-font-family-live-restore').details.boundFonts[0].family='Encode Sans'],
 ['caller font restore lost',r=>r.contracts.find(c=>c.id==='caller-font-family-live-restore').details.restoredFonts[0].weight=999],
 ['caller editor replaced by style',r=>r.contracts.find(c=>c.id==='caller-font-family-live-restore').details.editorRetained=false],
 ['remounted arrows lost',r=>r.contracts.find(c=>c.id==='unload-retires-timer-and-input').details.navigationRestored=false],
 ['missing backdrop host',r=>r.effects[0].backdrops.pop()],
 ['wrong backdrop sigma',r=>r.effects[0].backdrops[0].sigma=1],
 ['backdrop source substituted',r=>r.effects[0].backdrops[0].liveBackdrop=false],
 ['backdrop bounds detached',r=>r.effects[0].backdrops[0].box.x+=1],
 ['backdrop pixels unchanged',r=>r.effects[0].noBackdrops=r.effects[0].allEffects],
 ['tag glyph source replaced',r=>r.effects[0].tagShadows[0].originalGlyphRetained=false],
 ['tag shadow sigma changed',r=>r.effects[0].tagShadows[0].blurSigma=1],
 ['tag shadow suppression unverified',r=>r.effects[0].tagShadowSuppression='unverified'],
 ...['originalTextRetained','originalGlyphSurfaceRetained','fourCornerClipUpdated','resizedRtlBoundsMatched','emptyTagReleased','restoredTagMounted','emptyTitleReleased','restoredTitleMounted'].map(key=>['live effect '+key,r=>r.effects[0].live[key]=false]),
 ['live tag shadow pixels unchanged',r=>r.effects[0].live.off=r.effects[0].live.on],
 ['tag shadow pixels unchanged',r=>r.effects[0].noTagShadows=r.effects[0].allEffects],
 ['tag shadow placement pixels unchanged',r=>r.effects[0].shadowPlacement=r.effects[0].allEffects],
 ['tag glyph mask pixels unchanged',r=>r.effects[0].shadowMask=r.effects[0].allEffects],
 ['removed tag retained graph',r=>r.effects[0].removedTagReleased=false],
 ['replaced arrow retained graph',r=>r.effects[0].replacedArrowReleased=false],
 ['unloaded effect graph retained',r=>r.effects[0].unloadedGraphsReleased=false],
 ['caller gradient replaced',r=>r.contracts.find(c=>c.id==='caller-foreground-live-restore').details.originalGradientRetained=false],
 ['caller gradient visually unchanged',r=>r.customizations[0].afterInk=r.customizations[0].beforeInk],
 ['missing caller customization captures',r=>r.customizations.pop()],
 ['missing capture',r=>r.pngs.pop()],['forged PNG digest',r=>r.pngs[0].sha256='0'.repeat(64)],['PNG path traversal',r=>r.pngs[0].file='../escape.png'],
 ['unstable rendered capture',r=>r.pngs[0].stableConsecutiveFrames=1],['unverified capture count',r=>r.pngs[0].samples=1],
 ...(host==='Orchard.Catalog.dll'?[['missing actual catalog',r=>r.catalog.pop()],['catalog styles omitted',r=>r.catalog[0].publicStylesVerified=4],['catalog customization omitted',r=>r.catalog[0].callerCustomizationVerified=false]]:[]),
 ['wrong actual executable architecture',(_r,folder)=>{const file=path.join(folder,'app-identity',host.replace(/\.dll$/,'.exe'));const data=fs.readFileSync(file);data.writeUInt16LE(0xaa64,data.readUInt32LE(0x3c)+4);fs.unlinkSync(file);fs.writeFileSync(file,data);} ],
 ['real wrong-theme PNG with recomputed hash',(r,folder)=>{const target=r.states.find(s=>s.id==='empty'&&s.scheme==='Light'),source=r.states.find(s=>s.id==='empty'&&s.scheme==='Dark'),file=path.join(folder,target.file);fs.unlinkSync(file);fs.copyFileSync(path.join(gold,source.file),file);r.pngs.find(p=>p.file===target.file).sha256=hash(file);}]
];
const rejected=[];
for(const [index,[name,mutate]]of mutations.entries()){
 const folder=path.join(casesRoot,String(index).padStart(2,'0'));fs.mkdirSync(folder);fs.mkdirSync(path.join(folder,'app-identity'));
 for(const entry of fs.readdirSync(path.join(gold,'app-identity')))fs.linkSync(path.join(gold,'app-identity',entry),path.join(folder,'app-identity',entry));
 for(const file of ['source-reference.json','process-exit.json',...reference.pngs.map(p=>p.file)])fs.linkSync(path.join(gold,file),path.join(folder,file));
 const candidate=structuredClone(reference);mutate(candidate,folder);fs.writeFileSync(path.join(folder,'hero-carousel.json'),JSON.stringify(candidate,null,2)+'\n');
 let failure=null;try{verifyHeroCarouselNative(folder,host,path.join(folder,'audit.json'));}catch(error){failure=error;}
 assert(failure&&failure.code==='ERR_ASSERTION',name+' was accepted or failed for an unrelated reason');rejected.push({name,error:failure.message});
}
const result={format:'orchard-hero-carousel-native-rejections-v1',passed:true,negativeCasesRejected:rejected.length,rejected,sourceReportSha256:hash(path.join(gold,'hero-carousel.json')),sourceRasterParityVerified:false,nativeComponentAccepted:false};
fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({passed:true,negativeCasesRejected:rejected.length,output}));
// The rejected cases are reproducible from the source report. Keep them only when
// an assertion fails, so successful audits do not retain dozens of hard links.
fs.rmSync(casesRoot,{recursive:true,force:true});
