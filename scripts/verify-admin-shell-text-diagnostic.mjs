import fs from 'node:fs/promises';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const option=name=>{const index=process.argv.indexOf(name);assert(index>=0&&process.argv[index+1]);return path.resolve(process.argv[index+1]);};
const folder=option('--native'),app=option('--app'),source=option('--source');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const referenceBytes=await fs.readFile(new URL('../tests/Reference/AdminShell.json',import.meta.url)),reference=JSON.parse(referenceBytes);
const report=JSON.parse(await fs.readFile(path.join(folder,'admin-shell-text-diagnostic.json'))),result=JSON.parse(await fs.readFile(path.join(folder,'result.json')));
const processExit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));assert.equal(processExit.component,'admin-shell-text-diagnostic');assert.equal(processExit.exited,true);assert.equal(processExit.exitCode,0,'Native text process did not exit cleanly.');
assert.equal(reference.commit,'a86059013cd59684ce452849278ea81e734f2066');assert.equal(reference.sourceSha256,'535f760672696f04aa108eb79bf537055358641f121ae24dde0a0c98cb29c864');
assert.equal(report.passed,true);assert.equal(report.scope,'diagnostic-admin-shell-text-rendering');assert.equal(report.referenceSha256,hash(referenceBytes));
for(const flag of ['diagnosticOnly','rawClientFrames','foregroundCapture','opaqueTextProbe'])assert.equal(report[flag],true,flag);
for(const flag of ['sourcePixelsPainted','productionLibraryChanged','osSettingsChanged','narratorLaunched','sourceRasterParityVerified','nativeComponentAccepted'])assert.equal(report[flag],false,flag);
assert.equal(report.librarySha256,hash(await fs.readFile(path.join(app,'Orchard.dll'))));assert.equal(report.catalogSha256,hash(await fs.readFile(path.join(app,'Orchard.Catalog.dll'))));
assert.equal(result.passed,true);assert.equal(result.component,'admin-shell-text-diagnostic');assert.equal(result.environment.displayScale,1);assert.equal(result.environment.textScale,1);assert.equal(result.environment.processArchitecture,result.environment.osArchitecture);assert(['X64','Arm64'].includes(result.environment.processArchitecture));
const names=['native','auto-default','cleartype-default','cleartype-natural','cleartype-symmetric','cleartype-gdi-natural','cleartype-no-grid','grayscale-symmetric'];
const profiles={'auto-default':['Auto','Default','Default'],'cleartype-default':['ClearType','Default','Default'],'cleartype-natural':['ClearType','Natural','Default'],'cleartype-symmetric':['ClearType','NaturalSymmetric','Default'],'cleartype-gdi-natural':['ClearType','GdiNatural','Default'],'cleartype-no-grid':['ClearType','NaturalSymmetric','Disable'],'grayscale-symmetric':['Grayscale','NaturalSymmetric','Default']};
const custom=new Map();for(const gamma of [1,1.4,1.8,2.2])for(const contrast of [0,.5,1])for(const mode of ['Natural','NaturalSymmetric']){const name=`custom-g${gamma.toFixed(1)}-c${contrast.toFixed(1)}-${mode}`;names.push(name);profiles[name]=['ClearType',mode,'Default'];custom.set(name,{gamma,contrast,mode:mode==='Natural'?4:5});}
for(const order of ['rgb','bgr']){const name=`coverage-${order}565`;names.push(name);profiles[name]=['ClearType','NaturalSymmetric','Default'];custom.set(name,{gamma:1,contrast:0,mode:5});}
assert.equal(report.frames.length,2);assert.equal(report.variants.length,68);assert.equal(Object.keys(report.captures).length,68);assert.equal(Object.keys(report.rasterMatches).length,68);
assert.deepEqual(report.frames.map(row=>row.scheme+'/'+row.id).sort(),['Dark/header-text','Light/header-text']);
assert.deepEqual(report.variants.map(row=>row.scheme+'/'+row.variant).sort(),['Light','Dark'].flatMap(scheme=>names.map(name=>scheme+'/'+name)).sort());
const rows=[];
for(const variant of report.variants){
 const wanted=reference.cases.find(row=>row.id===variant.id&&row.scheme===variant.scheme);assert(wanted&&wanted.id==='header-text');assert.equal(variant.sourceFile,wanted.file);
 assert.equal(variant.text,'Caller header');assert.equal(variant.FontSize,16);assert.equal(variant.fontHash,reference.fonts.find(font=>font.name==='EncodeSans-Regular.ttf').sha256);
 assert.equal(variant.fontHash,hash(await fs.readFile(path.join(app,'Orchard/Assets/Fonts/EncodeSans-Regular.ttf'))));
 let maximumGeometryError=0;
 for(const part of ['shell','row','main','content','header','sidebar','body','footer','footerBody']){assert.equal(variant.measurement[part]===null,wanted.measurement[part]===null);if(wanted.measurement[part])for(const field of ['x','y','width','height']){const actual=variant.measurement[part][field],expected=wanted.measurement[part][field];assert(Number.isFinite(actual));maximumGeometryError=Math.max(maximumGeometryError,Math.abs(actual-expected));}}
 assert.equal(maximumGeometryError,0,'Text probe changed source layout.');
 if(variant.variant==='native')assert.equal(variant.rendering,null);else{assert.deepEqual([variant.rendering.antialiasing,variant.rendering.mode,variant.rendering.gridFit],profiles[variant.variant]);assert.equal(variant.rendering.alpha,'Ignore');assert.equal(variant.rendering.dpi,96);assert(Number.isFinite(variant.rendering.draw.x)&&Number.isFinite(variant.rendering.draw.y));}
 if(variant.rendering){const applied=variant.rendering.applied;assert(applied);assert(Number.isFinite(applied.Gamma)&&applied.Gamma>0&&applied.Gamma<=256);assert(Number.isFinite(applied.EnhancedContrast)&&applied.EnhancedContrast>=0);assert(applied.ClearTypeLevel>=0&&applied.ClearTypeLevel<=1);assert([0,1,2].includes(applied.PixelGeometry));assert(Number.isInteger(applied.RenderingMode)&&applied.RenderingMode>=0&&applied.RenderingMode<=7);const requested=custom.get(variant.variant);if(requested){assert.equal(variant.rendering.requestedGamma,requested.gamma);assert.equal(variant.rendering.requestedContrast,requested.contrast);assert.equal(applied.Gamma,requested.gamma);assert.equal(applied.EnhancedContrast,requested.contrast);assert.equal(applied.ClearTypeLevel,1);assert.equal(applied.PixelGeometry,1);assert.equal(applied.RenderingMode,requested.mode);}else{assert.equal(variant.rendering.requestedGamma,null);assert.equal(variant.rendering.requestedContrast,null);}}
 if(variant.rendering)assert.equal(variant.rendering.coverageFormat,variant.variant==='coverage-rgb565'?'RGB565':variant.variant==='coverage-bgr565'?'BGR565':'None');
 const wantedBytes=await fs.readFile(path.join(source,wanted.file)),actualBytes=await fs.readFile(path.join(folder,variant.file));assert.equal(hash(wantedBytes),wanted.pngSha256);assert.equal(hash(actualBytes),report.captures[variant.file]);
 const expected=readRgbaPng(wantedBytes),actual=readRgbaPng(actualBytes);assert.deepEqual([actual.width,actual.height],[expected.width,expected.height]);assert.deepEqual([actual.width,actual.height],[wanted.spec.viewport.width,wanted.spec.viewport.height]);
 let differingPixels=0,absoluteRgbaError=0;for(let i=0;i<expected.rgba.length;i+=4){let different=false;for(let c=0;c<4;c++){const delta=Math.abs(expected.rgba[i+c]-actual.rgba[i+c]);different||=delta!==0;absoluteRgbaError+=delta;}differingPixels+=Number(different);}
 assert.equal(report.rasterMatches[variant.file],differingPixels===0);
 rows.push({scheme:variant.scheme,variant:variant.variant,file:variant.file,maximumGeometryError,differingPixels,absoluteRgbaError,applied:variant.rendering?.applied??null,sha256:hash(actualBytes)});
}
const audit={passed:true,scope:'diagnostic-admin-shell-text-rendering',librarySha256:report.librarySha256,catalogSha256:report.catalogSha256,sourceStates:2,variants:68,rawPngs:68,customProfiles:52,coverageProfiles:4,parameterReadbackVerified:true,rows,masks:0,pixelTolerance:0,resampling:false,opaqueTextProbe:true,arbitraryBackgroundsVerified:false,sourceRasterParityVerified:false,nativeComponentAccepted:false};
await fs.writeFile(option('--output'),JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
