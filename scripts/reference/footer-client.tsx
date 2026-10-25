import React from 'react';
import {createRoot} from 'react-dom/client';
import {Footer} from 'frozen/footer';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const[spec,setSpec]=React.useState(initial);w.footerSpec=spec;
 w.updateFooter=(patch:any)=>setSpec((previous:any)=>({...previous,...patch}));
 const originalItems=spec.sections.flatMap(section=>section.items);
 const renderLink=spec.custom?props=><a href={props.item.href} className={props.className} data-source-marker={props.item.marker} onClick={event=>{event.preventDefault();w.footerRequests.push({href:props.item.href,label:props.item.label,marker:props.item.marker??null,original:originalItems.includes(props.item)});props.onClick?.();}}>{props.children}</a>:undefined;
 const bottomLeft=spec.input?<input aria-label="Footer caller input" defaultValue="Original"/>:spec.bottomLeft;
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{width:spec.width??spec.viewportWidth}}>
  <Footer brand={spec.brand} sections={spec.sections} bottomLeft={bottomLeft} bottomRight={spec.bottomRight} showBottomAccent={spec.showBottomAccent} renderLink={renderLink}/>
 </div>;
}
w.showFooter=spec=>{w.footerRequests=[];root.render(<View key={++serial} initial={spec}/>);};
