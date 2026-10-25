import React from 'react';
import {createRoot} from 'react-dom/client';
import {Modal} from 'frozen/modal';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({spec}:any){
 const [open,setOpen]=React.useState(true);const [title,setTitle]=React.useState(spec.kind==='title-wrap'?'Fictional component heading with deliberately long wrapping text across several lines':'Fictional component demonstration');const [height,setHeight]=React.useState(spec.kind==='long'?1000:80);
 (window as any).updateModal=(patch:any)=>{if('open'in patch)setOpen(patch.open);if('title'in patch)setTitle(patch.title);if('height'in patch)setHeight(patch.height);};
 const close=()=>{(window as any).modalEvents.push('close');if(spec.accept)setOpen(false);};
 return <><button id="origin">Original fictional trigger</button><Modal isOpen={open} onClose={close} title={title} className={spec.kind==='custom'?'max-w-2xl rounded-none':undefined}>
  {spec.kind==='empty'?null:<div id="consumer" style={{height,width:'100%'}}><input id="editor" defaultValue="Fictional original value"/><div style={{height:Math.max(0,height-100)}}/><button id="consumer-action" onClick={()=> (window as any).modalEvents.push('action')}>Original fictional action</button></div>}
 </Modal></>;
}
(window as any).modalEvents=[];(window as any).showModal=(spec:any)=>{(window as any).modalEvents=[];root.render(<View key={++serial} spec={spec}/>);};
