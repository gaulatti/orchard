import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Accordion} from 'frozen/accordion';
const root=createRoot(document.getElementById('root')!);
let generation=0;
const body=(text:string)=><div style={{padding:16,fontSize:14,lineHeight:'20px'}}>{text}</div>;
const common=()=>[
 {id:'first',title:'Delivery schedule',subtitle:'Choose when updates arrive.',content:body('Daily updates arrive each morning.')},
 {id:'second',title:'Reader preferences',content:body('Your choices stay with this view.')}
];
const action=(id:string)=><button type="button" style={{width:48,height:24,padding:0,fontSize:12,lineHeight:'20px'}} onClick={()=>{window.actions.push(id)}}>Edit</button>;
window.actions=[];
window.show=(spec:any,reset=true)=>{
 if(reset){generation++;window.actions=[];}
 const items=common();
 if(spec.id==='long')items[0].title='A long delivery schedule title that wraps in a constrained header';
 if(spec.id==='zero-subtitle')items[0].subtitle=0 as any;
 if(spec.id==='actions')items[0].actions=action('first');
 if(spec.id==='rich'){
  items[0].title=<span>Reader <strong>preferences</strong></span> as any;
  items[0].subtitle=<span>Local <em>choices</em></span> as any;
  items[0].actions=action('first');
  items[0].content=<div style={{padding:16}}><button type="button" style={{width:110,height:28,fontSize:14,lineHeight:'20px'}}>Apply choices</button></div>;
 }
 if(spec.items)items.splice(0,items.length,...spec.items.map((item:any)=>({...item,content:body(item.content)})));
 const actual=spec.id==='empty'?[]:items;
 flushSync(()=>root.render(<Accordion key={generation} items={actual} defaultExpandedId={spec.expanded??null}/>));
};
window.measure=()=>{
 const root=document.querySelector('#root > div')!;
 const box=(element:Element)=>{const b=element.getBoundingClientRect();return{x:b.x,y:b.y,width:b.width,height:b.height}};
 const paint=(element:Element)=>{const s=getComputedStyle(element);return{background:s.backgroundColor,border:s.borderTopColor,borderWidth:s.borderTopWidth,radius:s.borderTopLeftRadius,shadow:s.boxShadow,font:s.fontFamily,fontSize:s.fontSize,lineHeight:s.lineHeight,color:s.color,padding:s.padding}};
 return{box:box(root),paint:paint(root),rows:[...root.querySelector('ul')!.children].map(li=>{
  const header=li.firstElementChild!,button=header.firstElementChild!,title=button.lastElementChild!.firstElementChild!,subtitle=title.nextElementSibling,grid=header.nextElementSibling!,content=grid.firstElementChild!.firstElementChild!;
  const arrow=getComputedStyle(button.firstElementChild!);
  return{expanded:button.getAttribute('aria-expanded')==='true',box:box(li),header:box(header),button:box(button),title:{text:title.textContent,box:box(title),paint:paint(title)},subtitle:subtitle?{text:subtitle.textContent,box:box(subtitle),paint:paint(subtitle)}:null,actions:header.children[1]?box(header.children[1]):null,body:box(grid),content:box(content),headerPaint:paint(header),contentPaint:paint(content),arrow:{transform:arrow.transform,rotate:arrow.rotate}};
 })};
};
