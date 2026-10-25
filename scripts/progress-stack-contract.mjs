import assert from 'node:assert/strict';
import {cssRgba} from './reference/css-paint.mjs';

export function compareProgressStackFrame(reference,frame,unicode=false){
 const context=[reference.id,reference.scheme,reference.motion].join('/'),found=frame.measurement,wanted=reference.measurement,errors=[];let maximumGeometryError=0;
 function near(actual,expected,label){assert(Number.isFinite(actual)&&Number.isFinite(expected),label);const error=Math.abs(actual-expected);maximumGeometryError=Math.max(maximumGeometryError,error);if(error>=.001)errors.push({context:label,actual,expected,error});}
 function same(actual,expected,label){if(actual!==expected)errors.push({context:label,actual,expected});}
 function box(actual,expected,label){assert(actual&&expected,label);for(const field of ['x','y','width','height'])near(actual[field],expected[field],label+'/'+field);}
 function paint(actual,expected,label){const[r,g,b,a]=cssRgba(expected);for(const[field,wanted]of Object.entries({r,g,b}))if(a>0)same(actual[field],wanted,label+'/'+field);if(Math.abs(actual.a/255*actual.opacity-a)>=.000001)errors.push({context:label,actual,expected});}
 const text=value=>value.replace(/[ \t\r\n\f]+/g,' ').replace(/^ +| +$/g,'');
 const paintText=value=>value.replace(/[\u200b\u2060\u00ad]/g,'');
 function lines(field){const groups=new Map();for(const part of field.graphemes){const values=groups.get(part.line)??[];values.push(part.text);groups.set(part.line,values);}const logical=[...groups].sort(([a],[b])=>a-b).map(([,values])=>values.join('').replace(/^ +| +$/g,''));return logical.map((value,index)=>paintText(value)+(index<logical.length-1&&value.endsWith('\u00ad')?'\u2010':'')).join('\n');}
 if(unicode)assert.deepEqual(frame.labels,reference.spec.items.map(item=>item.label),context+'/consumer-labels');
 box(found.root.box,wanted.root.box,context+'/root');box(found.bar.box,wanted.bar.box,context+'/bar');paint(found.bar.background,wanted.bar.background,context+'/track-paint');
 same(found.segments.length,wanted.segments.length,context+'/segments');for(let i=0;i<wanted.segments.length;i++){box(found.segments[i].box,wanted.segments[i].box,context+'/segment-'+i);paint(found.segments[i].background,wanted.segments[i].background,context+'/segment-'+i+'/paint');same(found.segments[i].title,wanted.segments[i].title,context+'/segment-'+i+'/title');}
 same(found.legend===null,wanted.legend===null,context+'/legend-presence');if(wanted.legend&&found.legend)box(found.legend.box,wanted.legend.box,context+'/legend');same(found.items.length,wanted.items.length,context+'/items');
 for(let i=0;i<wanted.items.length;i++){
  const actual=found.items[i],expected=wanted.items[i];box(actual.box,expected.box,context+'/item-'+i);box(actual.swatch.box,expected.swatch.box,context+'/swatch-'+i);paint(actual.swatch.background,expected.swatch.background,context+'/swatch-'+i+'/paint');
  for(const part of ['label','value']){box(actual[part].box,expected[part].box,context+'/'+i+'/'+part);same(actual[part].text,unicode?lines(expected[part]).replace(/\n/g,' '):text(expected[part].text),context+'/'+i+'/'+part+'/text');if(unicode)same(actual[part].renderedText,lines(expected[part]),context+'/'+i+'/'+part+'/rendered-lines');paint(actual[part].color,expected[part].color,context+'/'+i+'/'+part+'/color');near(actual[part].fontSize,parseFloat(expected[part].fontSize),context+'/'+i+'/'+part+'/font-size');near(actual[part].lineHeight,parseFloat(expected[part].lineHeight),context+'/'+i+'/'+part+'/line-height');same(actual[part].weight,Number(expected[part].weight),context+'/'+i+'/'+part+'/weight');}
 }
 same(found.IsTabStop,false,context+'/tab-stop');same(found.name,'',context+'/name');same(found.hasInvoke,false,context+'/invoke');same(found.hasRange,false,context+'/range');assert.equal(wanted.animations,0);
 return{errors,maximumGeometryError};
}
