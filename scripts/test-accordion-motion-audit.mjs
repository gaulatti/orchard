import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const at=process.argv.indexOf('--native');assert(at>=0&&process.argv[at+1]);
const source=path.resolve(process.argv[at+1]),temporary=await fs.mkdtemp(path.join(os.tmpdir(),'orchard-accordion-motion-audit-'));
const verifier=fileURLToPath(new URL('./verify-accordion-motion.mjs',import.meta.url)),files=['preference-restored.json',...['enabled','reduced'].flatMap(mode=>[`${mode}/result.json`,`${mode}/accordion-motion.json`])];
async function copy(){for(const file of files){const target=path.join(temporary,file);await fs.mkdir(path.dirname(target),{recursive:true});await fs.copyFile(path.join(source,file),target);}}
const run=()=>spawnSync(process.execPath,[verifier,'--native',temporary,'--output',path.join(temporary,'audit.json')],{stdio:'ignore'});
const expansion=data=>data.reports.find(row=>row.kind==='expansion'&&row.from==='none'&&row.to==='first');
const hover=data=>data.reports.find(row=>row.kind==='hover'&&row.from==='none'&&row.to==='first');
const moving=data=>expansion(data).frames.find(rows=>rows[0].Fraction>0&&rows[0].Fraction<1)[0];
const unquantized=data=>{
 const rows=expansion(data).frames.map(rows=>rows[0]).filter(row=>row.Fraction>0&&row.Fraction<1);
 rows.sort((a,b)=>Math.abs(b.NaturalHeight*b.Fraction-b.Height)-Math.abs(a.NaturalHeight*a.Fraction-a.Height));
 const row=rows[0];assert(row&&Math.abs(row.NaturalHeight*row.Fraction-row.Height)>=.001,'Capture has no measurable unquantized-height corruption.');return row;
};
try{
 await copy();assert.equal(run().status,0,'Unmodified motion receipt failed.');
 const cases=[
  ['enabled/accordion-motion.json',data=>{const row=unquantized(data);row.Height=row.NaturalHeight*row.Fraction;}],
  ['enabled/accordion-motion.json',data=>moving(data).ClipHeight+=1],
  ['enabled/accordion-motion.json',data=>moving(data).Angle+=.0001],
  ['enabled/accordion-motion.json',data=>moving(data).Fraction+=.001],
  ['enabled/accordion-motion.json',data=>expansion(data).immediate[0].ExpansionDuration=220],
  ['enabled/accordion-motion.json',data=>moving(data).PaintedAt=null],
  ['enabled/accordion-motion.json',data=>hover(data).frames.find(rows=>rows[0].BackgroundOpacity>0&&rows[0].BackgroundOpacity<.3)[0].Background='#FF000000'],
  ['enabled/accordion-motion.json',data=>hover(data).immediate[0].BackgroundDuration=200],
  ['enabled/accordion-motion.json',data=>hover(data).before[0].BackgroundOpacity=.1],
  ['enabled/accordion-motion.json',data=>data.reports.find(row=>row.kind==='hover'&&row.from==='first').before[0].Hovered=false],
  ['enabled/accordion-motion.json',data=>hover(data).frames=[]],
  ['enabled/accordion-motion.json',data=>expansion(data).frames=Array(4).fill(expansion(data).after)],
  ['enabled/accordion-motion.json',data=>data.reports[1]=data.reports[0]],
  ['enabled/accordion-motion.json',data=>expansion(data).frames[0].pop()],
  ['enabled/accordion-motion.json',data=>expansion(data).frames[0][1].Key='first'],
  ['enabled/accordion-motion.json',data=>data.reversals[1]=data.reversals[0]],
  ['enabled/accordion-motion.json',data=>data.reversals[0].steps[0].elapsed=50],
  ['enabled/accordion-motion.json',data=>data.reversals[0].steps[0].immediate[0].ExpansionDuration=200],
  ['enabled/accordion-motion.json',data=>data.reversals[0].steps[1].frames=Array(4).fill(data.reversals[0].steps[1].frames[0])],
  ['reduced/accordion-motion.json',data=>expansion(data).immediate[0].Animating=true],
  ['reduced/result.json',data=>data.environment.animationsEnabled=true],
  ['enabled/result.json',data=>data.environment.displayScale=1.25],
  ['preference-restored.json',data=>data.restored=!data.original]
 ];
 for(const[index,[file,change]]of cases.entries()){await copy();const target=path.join(temporary,file),data=JSON.parse((await fs.readFile(target,'utf8')).replace(/^\uFEFF/,''));change(data);await fs.writeFile(target,JSON.stringify(data));assert.equal(run().status,1,'Corrupt Accordion motion receipt passed: '+index+'/'+file);}
 console.log(JSON.stringify({passed:true,passingControls:1,rejectedMutations:cases.length}));
}finally{assert.equal(path.dirname(temporary),path.resolve(os.tmpdir()));assert(/^orchard-accordion-motion-audit-[A-Za-z0-9]+$/.test(path.basename(temporary)));assert.equal((await fs.lstat(temporary)).isSymbolicLink(),false);await fs.rm(temporary,{recursive:true,force:true});}
