import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Empty} from 'frozen/empty';

const root=createRoot(document.getElementById('root')!);
let key=0;
const events:string[]=[];
function scalar(kind:string,value:string){return kind==='boolean'?value==='true':kind==='number'?Number(value):value;}
function icon(spec:any){
 if(spec.icon==='scalar')return scalar(spec.iconKind,spec.iconValue);
 if(spec.icon==='custom')return <span data-custom-icon style={{width:24,height:24,color:'rgb(180, 55, 90)',background:'currentColor'}}/>;
 if(spec.icon==='large')return <span data-custom-icon style={{width:100,height:80,flexShrink:0,background:'currentColor'}}/>;
 return spec.icon==='null'?null:undefined;
}
function action(spec:any){
 if(spec.action==='scalar')return scalar(spec.actionKind,spec.actionValue);
 if(spec.action==='rich')return <span data-action-content><button id="empty-action" aria-label="Create item" onClick={()=>events.push('create')}>Create</button><input id="empty-input" aria-label="Consumer value" defaultValue="Retained" style={{width:80}}/></span>;
 return undefined;
}
(window as any).emptyEvents=events;
(window as any).showEmpty=(spec:any,remount=true)=>{
 if(remount){key++;events.length=0;}
 const host=document.getElementById('root')!;host.style.width=`${spec.parentWidth??280}px`;host.style.direction=spec.rtl?'rtl':'ltr';
 flushSync(()=>root.render(<Empty key={key} title={spec.title??'No items'} description={spec.description} icon={icon(spec)} action={action(spec)} className={spec.custom?'consumer-empty':undefined}/>));
};
