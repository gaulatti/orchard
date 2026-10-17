import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {RadioGroup} from 'frozen/radio-group';
const w=window as any,root=createRoot(document.getElementById('root')!);
const draw=()=>flushSync(()=>root.render(<RadioGroup {...w.spec} onChange={(value:string)=>{w.requests.push(value);if(w.spec.accept){w.spec.value=value;draw();}}}/>));
w.show=(spec:any)=>{w.spec=spec;w.requests=[];draw();};
w.measure=()=>{
 const box=(el:Element)=>{const b=el.getBoundingClientRect();return {x:b.x,y:b.y,width:b.width,height:b.height};};
 const paint=(el:Element)=>{const s=getComputedStyle(el);return Object.fromEntries(['color','borderColor','borderWidth','opacity','fontSize','fontWeight','lineHeight','transform','backgroundColor'].map(k=>[k,(s as any)[k]]));};
 const group=document.querySelector('[role=radiogroup]')!;
 return {box:box(group),options:[...group.querySelectorAll('label')].map(label=>{
  const input=label.querySelector('input')!,ring=label.querySelector('input + span')!,dot=ring.firstElementChild!,text=label.lastElementChild!;
  return {value:input.value,checked:input.checked,disabled:input.disabled,box:box(label),ring:{box:box(ring),paint:paint(ring)},dot:{box:box(dot),paint:paint(dot)},label:{box:box(text.firstElementChild!),paint:paint(text.firstElementChild!)},description:text.children.length>1?{box:box(text.lastElementChild!),paint:paint(text.lastElementChild!)}:null,opacity:paint(label).opacity};
 })};
};
