module.exports=function collect(){
 const upper=[],lower=[],effectiveCased=[],ignorable=[];let scalars=0;
 for(let cp=0;cp<=0x10ffff;cp++){
  if(cp>=0xd800&&cp<=0xdfff)continue;
  const text=String.fromCodePoint(cp),u=text.toUpperCase(),l=text.toLowerCase();
  if(u!==text)upper.push([cp,u]);if(l!==text)lower.push([cp,l]);scalars++;
  // Observe the producer runtime's final-sigma context rules directly.
  const cased=(text+'Σ').toLowerCase().endsWith('ς');
  if(cased)effectiveCased.push(cp);
  else if(('A'+text+'Σ').toLowerCase().endsWith('ς'))ignorable.push(cp);
 }
 const ranges=values=>{const result=[];for(const cp of values){const last=result.at(-1);if(last&&last[1]+1===cp)last[1]=cp;else result.push([cp,cp]);}return result;};
 const units=text=>Array.from({length:text.length},(_,i)=>text.charCodeAt(i));
 const contexts=['ΟΣ','ΟΣΑ','Ο\u0301Σ','Ο\u0301Σ\u0301','Ο\u0301Σ\u0301Α','\u0345Σ','A\u0345Σ','\u037AΣ','AΣ\u037A','AΣ\u0345','AΣ\u02B0','\u02B0Σ','AΣ\u0301B','AΣ\u0301','\u2160Σ','\u24B6Σ','AΣ\u24B6','AΣ\u2160','İ','İΣ','ß','ﬃ','ſ','𐐀𐐨','\u1C89\u1C8A','\uA7CB','\uA7CC','\u{10D50}','\u{10D70}','\uD800','\uDC00','A\uD800Σ'].map(text=>({textUnits:units(text),upperUnits:units(text.toUpperCase()),lowerUnits:units(text.toLowerCase())}));
 return{scalars,upper,lower,effectiveCasedRanges:ranges(effectiveCased),caseIgnorableRanges:ranges(ignorable),contextProbeScalars:scalars,contexts};
};
