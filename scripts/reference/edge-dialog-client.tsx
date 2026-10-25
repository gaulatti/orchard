import React from 'react';import{createRoot}from'react-dom/client';import{Drawer}from'frozen/drawer';import{Sheet}from'frozen/sheet';
const root=createRoot(document.getElementById('root')!);let serial=0;
function View({spec}:any){
 const[open,setOpen]=React.useState(true);const[height,setHeight]=React.useState(spec.shape==='long'||spec.shape==='no-scroll'||spec.shape==='fill'?1000:80);
 (window as any).updateEdge=(p:any)=>{if('open'in p)setOpen(p.open);if('height'in p)setHeight(p.height);};
 const close=()=>{(window as any).edgeEvents.push('close');if(spec.accept)setOpen(false);};
 const title=spec.shape==='no-heading'||spec.shape==='description-only'?undefined:spec.shape==='title-wrap'?'Fictional component heading with deliberately long wrapping words across several lines':'Fictional component panel';
 const description=spec.shape==='no-heading'?undefined:'Fictional local component guidance for the original caller content.';
 const content=spec.shape==='empty'?null:spec.shape==='fill'?<div id="consumer" style={{minHeight:0,flex:1,overflow:'auto'}}><div style={{height:1000}}>Fictional scroll-owning content</div></div>:<div id="consumer" style={{height,width:'100%'}}><input id="editor" defaultValue="Fictional original value"/></div>;
 const props={isOpen:open,onClose:close,title,description,className:spec.shape==='custom'?(spec.kind==='drawer'?'max-h-[70dvh] rounded-none':'max-w-lg rounded-none'):undefined};
 return <><button id="origin">Fictional original trigger</button>{spec.kind==='drawer'?<Drawer {...props} snapPoints={spec.shape==='snap'?['25%','80%']:undefined}>{content}</Drawer>:<Sheet {...props} side={spec.side} scrollContent={spec.shape!=='no-scroll'&&spec.shape!=='fill'}>{content}</Sheet>}</>;
}
(window as any).edgeEvents=[];(window as any).showEdge=(spec:any)=>{(window as any).edgeEvents=[];root.render(<View key={++serial} spec={spec}/>);};
