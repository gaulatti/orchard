import assert from 'node:assert/strict';
const ids=['default','applied','refreshed','replaced','inherited','scaled-inherited','cleared','bound-first','bound-second','bound-cleared','retemplated','remounted'];
const padding=(l,t,r,b)=>({left:l,top:t,right:r,bottom:b});
export function verifyCommandSpotlightStyles(r){
 const closedExpected=r.format!=='orchard-command-spotlight-native-v1';
 assert.equal(r.styleContracts.length,closedExpected?28:24); let values=0;
 for(const theme of ['Light','Dark']){
  const all=r.styleContracts.filter(c=>c.theme===theme),records=all.filter(c=>!c.closed);assert.deepEqual(all.map(c=>c.id),closedExpected?ids.concat(['closed-first','closed-second']):ids);assert.deepEqual(records.map(c=>c.id),ids);
  for(const c of records){
   const i=ids.indexOf(c.id),styled=![0,6,9].includes(i),second=[3,8].includes(i),scaled=[5,6].includes(i),bound=i>=7,retained=i<10;
   const max=styled?(second?544:480):(scaled?1344:672),height=styled?(second?52:48):(scaled?72:36);
   const inset=styled?([4,5].includes(i)?padding(13,7,17,5):second?padding(6,8,10,12):padding(20,9,24,11)):scaled?padding(24,12,24,12):padding(12,6,12,6);
   assert.equal(c.passed,true);assert.equal(c.originalActionRetained,true);assert.equal(c.sameElements,retained);assert.equal(c.bindingsIntact,bound);assert.equal(c.surfaceLocalUnset,styled);assert.equal(c.triggerLocalsUnset,styled);assert.equal(c.FontSize,scaled?32:16);
   assert.equal(c.surface.MaxWidth,max);assert.equal(c.surface.box.width,Math.min(max,1000-32*(scaled?2:1)));assert.equal(c.surface.box.x,(1000-c.surface.box.width)/2);assert.equal(c.trigger.Height,height);assert.equal(c.trigger.ActualHeight,height);assert.deepEqual(c.trigger.padding,inset);assert.deepEqual(c.trigger.chromePadding,inset);assert.equal(c.file,`command-spotlight-style-${theme}-${c.id}.png`);
   if(styled){const rgb=second?(theme==='Light'?[45,45,45]:[240,244,248]):theme==='Light'?[44,87,132]:[91,163,245];const expected={a:255,r:rgb[0],g:rgb[1],b:rgb[2],opacity:1};assert.deepEqual(c.surface.background,expected);assert.deepEqual(c.trigger.background,expected);assert.deepEqual(c.trigger.chromeBackground,expected);const border=theme==='Light'?{a:255,r:230,g:213,b:184,opacity:Math.fround(.3)}:{a:255,r:255,g:255,b:255,opacity:Math.fround(.12)};assert.deepEqual(c.trigger.border,border);}
   else if(i!==0){const first=records[0];for(const key of ['background','chromeBackground','border'])assert.deepEqual(c.trigger[key],first.trigger[key],'clearing restores theme '+key);assert.deepEqual(c.surface.background,first.surface.background);}
   values+=18;
  }
  for(const c of all.filter(c=>c.closed)){
   assert.equal(c.passed,true);assert.equal(c.closed,true);assert.equal(c.originalActionRetained,true);const second=c.id==='closed-second',height=second?52:48,inset=second?padding(6,8,10,12):padding(20,9,24,11),rgb=second?(theme==='Light'?[45,45,45]:[240,244,248]):theme==='Light'?[44,87,132]:[91,163,245];
   assert.equal(c.trigger.Height,height);assert.equal(c.trigger.ActualHeight,height);assert.deepEqual(c.trigger.padding,inset);assert.deepEqual(c.trigger.chromePadding,inset);assert.deepEqual(c.trigger.background,{a:255,r:rgb[0],g:rgb[1],b:rgb[2],opacity:1});assert.deepEqual(c.trigger.chromeBackground,c.trigger.background);assert.equal(c.file,`command-spotlight-style-${theme}-${c.id}.png`);values+=10;
  }
 }
 return {styleStates:r.styleContracts.length,styleValues:values};
}
export function verifyCommandSpotlightStylePixels(c,png){
 const probe=(box,color,label)=>{const x=Math.floor(box.x+3),y=Math.floor(box.y+box.height/2),at=(y*png.width+x)*4;assert.deepEqual([...png.rgba.subarray(at,at+4)],[color.r,color.g,color.b,255],`${c.theme}/${c.id}/${label} rendered style paint`);};
 if(c.closed){probe(c.trigger.box,c.trigger.background,'closed-trigger');return 1;}
 if(!c.surfaceLocalUnset)return 0;
 probe(c.surface.box,c.surface.background,'surface');return 1;
}
