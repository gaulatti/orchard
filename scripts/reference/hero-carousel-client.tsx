import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {HeroCarousel} from 'frozen/hero-carousel';

const state=window as any,root=createRoot(document.getElementById('root')!);
let serial=0,originals:any[]=[],rendered:any[]=[],current:any,owned=new WeakSet<object>();
const first={id:'first',title:'Fictional coastal project',description:'An original caller-owned description for a fictional local component example.',href:'/fictional/coast?title=Original%20value&kind=project',kind:'project',tags:['Local','Original']};
const second={id:'second',title:'Fictional article',description:'A second original description stays with its caller.',href:'/fictional/article#original',kind:'post',external:true};
const third={id:'third',title:'Fictional mountain project',description:'A third original item in the same reusable component.',href:'/fictional/mountain',kind:'project',tags:['Retained']};
state.heroDefaultItems=[first,second,third];
state.heroEvents=[];
const customLink=(props:any)=>{
 rendered.push({item:props.item,ariaLabel:props.ariaLabel,className:props.className,children:props.children});
 return <a href={props.item.href} aria-label={props.ariaLabel} className={props.className} data-custom-link="original" onClick={()=>state.heroEvents.push({kind:'custom',original:owned.has(props.item),href:props.item.href,title:props.item.title})}>
  <div>{props.children}{current.customInput&&<input aria-label={'Original editor '+props.item.id} defaultValue="Caller original text"/>}</div>
 </a>;
};
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);current=spec;
 state.updateHero=(patch:any)=>{for(const item of patch.items??[])owned.add(item);flushSync(()=>setSpec((old:any)=>({...old,...patch})));};
 const items=spec.items??originals;
 return <><div style={{position:'absolute',top:700,left:20}}><button id="outside">Outside fictional action</button><input id="outside-input" aria-label="Outside input"/><textarea id="outside-textarea" aria-label="Outside textarea"/><div id="outside-editable" contentEditable suppressContentEditableWarning>Outside editable</div></div>
  {!spec.unmounted&&<HeroCarousel items={items} {...(Object.hasOwn(spec,'autoAdvanceMs')?{autoAdvanceMs:spec.autoAdvanceMs}:{})} showControls={spec.showControls} showIndicators={spec.showIndicators} className={spec.className} defaultBackgroundClassName={spec.backgroundClassName}
   emptyState={spec.customEmpty?<div data-original-empty="true"><span>Original caller empty content</span><input aria-label="Original empty editor" defaultValue="Retained empty editor"/></div>:undefined}
   renderLink={spec.customLink?customLink:undefined}/>}
  {spec.secondHost&&<HeroCarousel items={originals.slice(0,2)} autoAdvanceMs={spec.autoAdvanceMs??5000}/>}
 </>;
}
state.showHero=(spec:any={})=>{
 originals=spec.items??[structuredClone(first),structuredClone(second),structuredClone(third)];
 owned=new WeakSet(originals);state.heroOriginalItems=originals;state.heroEvents=[];rendered=[];
 document.documentElement.dir=spec.rtl?'rtl':'ltr';document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';
 flushSync(()=>root.render(<View key={++serial} initial={{...spec,items:originals}}/>));
};
state.heroRendererCalls=()=>rendered.map(p=>({original:owned.has(p.item),id:p.item.id??null,ariaLabel:p.ariaLabel,className:p.className,hasPreparedChildren:!!p.children}));
state.heroOriginals=()=>originals;
document.addEventListener('click',event=>{
 const link=(event.target as Element).closest?.('section a');if(!link)return;event.preventDefault();
 const slides=[...link.closest('section')!.querySelectorAll('.hero-carousel > div[aria-hidden]')],index=slides.findIndex(s=>s.contains(link));
 state.heroEvents.push({kind:'navigate',href:link.getAttribute('href'),title:link.querySelector('h1')?.textContent,ariaLabel:link.getAttribute('aria-label'),index,hidden:slides[index]?.getAttribute('aria-hidden')==='true',target:link.getAttribute('target'),rel:link.getAttribute('rel')});
});
