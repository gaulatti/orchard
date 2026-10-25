import React from 'react';
import {createRoot} from 'react-dom/client';
import {Tooltip} from 'frozen/tooltip';
import {Modal} from 'frozen/modal';

const root=createRoot(document.getElementById('root')!);
let serial=0;
function View({spec}:any) {
  const [open,setOpen]=React.useState(true);
  const fixed=spec.slot==='fixed';
  const content=fixed?<span style={{display:'block',width:120,height:39}}/>:'Fictional library guidance';
  const trigger=<Tooltip content={content} side={spec.side} delayDuration={0}><button id="tip-trigger" style={{width:120,height:36}}>Fictional help</button></Tooltip>;
  if(spec.scope==='modal') return <Modal isOpen={open} title="Fictional component demonstration" onClose={()=>{(window as any).tooltipEvents.push('close-parent');setOpen(false);}}><div style={{height:80,display:'flex',alignItems:'center',justifyContent:'center'}}>{trigger}</div></Modal>;
  return <div style={{position:'absolute',left:spec.x,top:spec.y}}>{trigger}</div>;
}
(window as any).tooltipEvents=[];
(window as any).showTooltip=(spec:any)=>{(window as any).tooltipEvents=[];root.render(<View key={++serial} spec={spec}/>);};
