import fs from 'node:fs/promises';
import {readFileSync} from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {verifyTableTemplate} from './verify-table-template.mjs';
import {readRgbaPng} from './reference/read-rgba-png.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const steps=['selector','bound','refreshed','notified','selectors-cleared','bindings-cleared'];
function tokenColors(file,key,themeKeys){
 const text=readFileSync(new URL(file,import.meta.url),'utf8'),result={};
 for(const [theme,dictionary] of Object.entries(themeKeys)){
  const blocks=[...text.matchAll(/<ResourceDictionary\s+x:Key="([^"]+)">([\s\S]*?)<\/ResourceDictionary>/g)].filter(m=>m[1]===dictionary);assert.equal(blocks.length,1,'One generated theme dictionary');
  const values=[...blocks[0][2].matchAll(/<SolidColorBrush\s+x:Key="([^"]+)"\s+Color="#([0-9a-f]{6}(?:[0-9a-f]{2})?)"/gi)].filter(m=>m[1]===key);assert.equal(values.length,1,'Explicit generated token color');
  const hex=values[0][2].toUpperCase();result[theme]='#'+(hex.length===6?'FF':'')+hex;
 }
 return result;
}
const activeColors=tokenColors('../src/Orchard/Themes/TablePalette.g.xaml','BleeckerTableSortActive',{Light:'Light',Dark:'Dark'});
// The central token generator names its dark dictionary Default.
const normalColors=tokenColors('../src/Orchard/Themes/Tokens.g.xaml','BleeckerTextSecondaryBrush',{Light:'Light',Dark:'Default'});
export function verifyTableLabelRaster(n){
 assert(['orchard-table-label-raster-native-v1','orchard-table-label-raster-native-v2'].includes(n.format));
 // The diagnostic executes the existing template history. Its distinct format
 // prevents a diagnostic run from passing the normal command by accident.
 const template=verifyTableTemplate({...n,format:'orchard-table-template-native-v1'}),d=n.labelRaster;
 assert.equal(d.diagnosticOnly,true);assert.equal(d.normalTimingRun,false);assert.equal(d.originalFramesReplaced,false);assert.equal(d.imageMasksApplied,false);
 assert.equal(d.frames.length,72);assert.equal(d.pngs.length,48);
 const files=new Map([...n.pngs,...d.pngs].map(p=>[p.file,p]));assert.equal(files.size,146);
 const seen=new Set(),trackingMarginMismatches=[];let snapshots=0;
 for(const scheme of ['Light','Dark'])for(const rtl of [false,true])for(const step of steps)for(const phase of ['original','repeat','after-layout']){
  const caseId=`table-template-${scheme}-${rtl?'True':'False'}-${step}`,matches=d.frames.filter(f=>f.caseId===caseId&&f.phase===phase);assert.equal(matches.length,1);const f=matches[0];
  assert(!seen.has(f.file));seen.add(f.file);assert(files.has(f.file));assert.equal(path.basename(f.file),f.file);assert(f.file.endsWith('.png'));
  if(phase==='original')assert.equal(f.file,caseId+'.png');else assert.equal(f.file,caseId+'-label-'+phase+'.png');
  for(const s of [f.before,f.after]){
   snapshots++;assert.equal(s.text,'FICTIONAL ITEM');assert.equal(s.field,'name');assert.equal(s.sorted,steps.indexOf(step)>=2);assert.equal(s.theme,scheme);assert.equal(s.requestedTheme,scheme);
   assert.equal(s.fontFamily,s.headerFontFamily);assert.equal(typeof s.fontFamily,'string');assert(s.fontFamily.length>0);
   assert.equal(s.fontSize,11);assert.equal(s.fontSize,s.headerFontSize);assert.equal(s.weight,600);assert.equal(s.weight,s.headerWeight);assert.equal(s.spacing,80);assert.equal(s.spacing,s.headerSpacing);
   assert.equal(typeof s.fontStyle,'string');assert.equal(typeof s.fontStretch,'string');assert.equal(typeof s.language,'string');assert(s.language.length>0);
   assert(Number.isFinite(s.rasterScale)&&s.rasterScale>0);assert(Number.isFinite(s.baseline)&&s.baseline>0);assert(Number.isFinite(s.lineHeight)&&s.lineHeight>0);
   for(const paint of [s.foreground,s.buttonForeground,s.palette]){assert.match(paint.color,/^#[0-9a-f]{8}$/i);assert(Number.isFinite(paint.opacity)&&paint.opacity>=0&&paint.opacity<=1);}
   assert.deepEqual(s.foreground,s.palette,'Label must use its current authoritative native palette');assert.deepEqual(s.buttonForeground,s.palette);
   assert.equal(s.palette.color,(s.sorted?activeColors:normalColors)[scheme],'Live brush must match the generated theme token');assert.equal(s.palette.opacity,1);
   assert(Array.isArray(s.ancestry)&&s.ancestry.length>=5&&s.ancestry.length<=64);
   for(const box of [s.label,s.button,s.heading,...s.ancestry]){
    for(const k of ['x','y','width','height','desiredWidth','desiredHeight','left','top','right','bottom','xx','xy','yx','yy','opacity'])assert(Number.isFinite(box[k]),k);
    assert(box.width>=0&&box.height>=0);assert.equal(typeof box.type,'string');assert.equal(typeof box.rounding,'boolean');assert(Array.isArray(box.columns));assert(box.columns.every(v=>Number.isFinite(v)&&v>=0));
    if(n.format==='orchard-table-label-raster-native-v2')for(const key of ['marginLeft','marginTop','marginRight','marginBottom'])assert(Number.isFinite(box[key]),key);
   }
   assert(s.label.width>0&&s.label.height>0);assert.equal(s.label.rounding,false);assert.equal(s.button.rounding,false);assert.equal(s.heading.direction,rtl?'RightToLeft':'LeftToRight');
   if(n.format==='orchard-table-label-raster-native-v2'){
    const advance=s.fontSize*s.spacing/1000,left=rtl?advance:0,right=rtl?0:advance;
    if(Math.abs(s.label.marginLeft-left)>1e-8||Math.abs(s.label.marginRight-right)>1e-8)trackingMarginMismatches.push({caseId,phase,expectedLeft:left,expectedRight:right,actualLeft:s.label.marginLeft,actualRight:s.label.marginRight});
   }
  }
 }
 return{passed:trackingMarginMismatches.length===0,diagnosticOnly:true,normalTimingRun:false,alignmentStates:24,labelFrames:72,beforeAfterSnapshots:snapshots,originalTemplateCaptures:98,additionalRawCaptures:48,paletteAndTypographyVerified:true,generatedThemeTokenColorsVerified:true,trackingMarginMismatches,templateSemantics:template,librarySha256:n.librarySha256,hostSha256:n.hostSha256,nativeComponentAccepted:false};
}
export async function auditTableLabelRaster(folder,host){
 const bytes=await fs.readFile(path.join(folder,'table-label-raster.json')),n=JSON.parse(bytes),audit=verifyTableLabelRaster(n),exit=JSON.parse(await fs.readFile(path.join(folder,'process-exit.json')));
 assert.equal(exit.exited,true);assert.equal(exit.exitCode,0);assert.equal(exit.timedOut,false);
 for(const [name,sha]of [['Orchard.dll',n.librarySha256],[host+'.dll',n.hostSha256]])assert.equal(hash(await fs.readFile(path.join(folder,'app-identity',name))),sha);
 for(const png of [...n.pngs,...n.labelRaster.pngs]){assert.equal(path.basename(png.file),png.file);const b=await fs.readFile(path.join(folder,png.file));assert.equal(hash(b),png.sha256);const rgba=readRgbaPng(b);assert.equal(rgba.width,png.width);assert.equal(rgba.height,png.height);}
 return{...audit,pngsVerified:146,reportSha256:hash(bytes)};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const [folder,host,output]=process.argv.slice(2);assert(folder&&['Orchard.Catalog','PackageConsumer'].includes(host)&&output);
 const audit=await auditTableLabelRaster(folder,host);await fs.writeFile(output,JSON.stringify(audit,null,2)+'\n');console.log(JSON.stringify(audit));
 if(!audit.passed)process.exitCode=1;
}
