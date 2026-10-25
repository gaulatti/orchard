import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Sonner, toast} from 'frozen/sonner';

const state=window as any, root=createRoot(document.getElementById('root')!);
let serial=0, random=0;
// Deterministic IDs make independently captured source receipts comparable.
Math.random=()=>((++random)%1000000)/1000000;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);
 state.updateSonner=(patch:any)=>flushSync(()=>setSpec((old:any)=>({...old,...patch})));
 const providers=spec.providers??[{key:'original',duration:spec.duration??4000,position:spec.position??'bottom-right'}];
 return <><button id="outside" style={{position:'absolute',left:20,top:20}}>Outside fictional action</button>
  {!spec.unmounted&&providers.map((p:any)=><Sonner key={p.key} duration={p.duration} position={p.position}/>)}</>;
}
state.showSonner=(spec:any)=>{
 state.sonnerEvents=[];state.sonnerIds=[];random=0;
 document.documentElement.dir=spec.rtl?'rtl':'ltr';
 document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';
 flushSync(()=>root.render(<View key={++serial} initial={spec}/>));
};
state.invokeSonner=(spec:any={})=>{
 const original:any={title:spec.title??'Fictional notification'};
 for(const name of ['description','variant'])if(Object.hasOwn(spec,name))original[name]=spec[name];
 if(Object.hasOwn(spec,'duration'))original.duration=spec.duration;
 if(spec.durationKind==='undefined')original.duration=undefined;
 if(spec.durationKind==='nan')original.duration=NaN;
 if(spec.durationKind==='infinity')original.duration=Infinity;
 let id='';
 if(spec.hasAction){
  const action:any={label:spec.actionLabel??'Continue'};
  if(Object.hasOwn(spec,'actionVariant'))action.variant=spec.actionVariant;
  action.onClick=()=>{
   state.sonnerEvents.push({kind:'action',label:action.label,original:original.action===action,id});
   if(spec.throwAction)throw new Error('fictional sonner action failed');
   if(spec.actionAdds)toast.info('Fictional follow-up',{duration:0});
   if(spec.actionDismisses)toast.dismiss(id);
  };
  original.action=action;
 }
 flushSync(()=>{
  const method=spec.method??'show';
  if(method==='show')id=toast.show(original);
  else {const {title,...rest}=original;id=(toast as any)[method](title,rest);}
 });
 state.sonnerIds.push(id);return id;
};
state.dismissSonner=(id:string)=>flushSync(()=>toast.dismiss(id));
