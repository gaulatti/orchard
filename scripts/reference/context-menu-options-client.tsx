import React from 'react';import {createRoot}from 'react-dom/client';import {Modal}from 'frozen/modal';import * as C from 'frozen/context-menu';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({spec}:any){
 const [modal,setModal]=React.useState(spec.modal);const w=window as any;w.setContextModality=setModal;
 const body=<div id="owner-content"><C.ContextMenu modal={modal} onOpenChange={open=>w.optionEvents.push(open?'open':'closed')}>
  <C.ContextMenuTrigger asChild><button id="option-target" style={{width:220,height:70}}>Original fictional context target</button></C.ContextMenuTrigger>
  <C.ContextMenuContent id="option-menu" onCloseAutoFocus={()=>w.optionEvents.push('close-auto-focus')}><C.ContextMenuItem id="option-action" onSelect={()=>w.optionEvents.push('select')}>Original fictional action</C.ContextMenuItem><C.ContextMenuItem disabled>Unavailable action</C.ContextMenuItem></C.ContextMenuContent>
 </C.ContextMenu><div style={{marginTop:160}}><button id="option-outside" onClick={()=>w.optionEvents.push('outside-click')}>Original fictional outside action</button><input id="option-editor" defaultValue="Original fictional text"/></div></div>;
 return spec.owned?<Modal isOpen title="Fictional owner" onClose={()=>w.optionEvents.push('owner-close-request')}>{body}</Modal>:<div style={{padding:20}}>{body}</div>;
}
(window as any).optionEvents=[];(window as any).showContextOptions=(spec:any)=>{(window as any).optionEvents=[];root.render(<View key={++serial} spec={spec}/>);};
