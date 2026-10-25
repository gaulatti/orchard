import React from 'react';
import {createRoot} from 'react-dom/client';
import {Header} from 'frozen/header';

const w=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
function View({initial}:any){
 const[spec,setSpec]=React.useState(initial);w.headerSpec=spec;w.updateHeader=(patch:any)=>setSpec((previous:any)=>({...previous,...patch}));
 const originals=spec.navigation;
 const renderLink=spec.custom?props=><a href={props.item.href} className={props.className} data-source-marker={props.item.marker} onClick={event=>{event.preventDefault();w.headerRequests.push({href:props.item.href,label:props.item.label,marker:props.item.marker??null,original:originals.includes(props.item)});props.onClick?.();}}>{props.children}</a>:undefined;
 const slot=(mobile:boolean)=>{const width=mobile?spec.mobileActionWidth:spec.actionWidth;if(width===null||width===false)return null;return spec.input?<input data-header-slot={mobile?'mobile-action':'action'} aria-label={mobile?'Header mobile caller input':'Header desktop caller input'} defaultValue="Original" style={{width,height:32,flexShrink:0}}/>:<div data-header-slot={mobile?'mobile-action':'action'} style={{width,height:32,flexShrink:0}}>{mobile?'Mobile action':'Caller action'}</div>;};
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{width:spec.width??spec.viewportWidth,marginLeft:spec.offsetX??0,marginTop:spec.offsetY??0,height:spec.hostHeight??2000,...(spec.headerHeight!==undefined?{'--bleecker-header-height':spec.headerHeight+'px'}:{})}}>
  <Header brand={spec.brand} navigation={spec.navigation} actions={slot(false)} mobileActions={slot(true)} fullWidth={spec.fullWidth} position={spec.position} renderLink={renderLink}/>
  <div id="following" style={{height:1600}}>Fictional local component fixture</div>
 </div>;
}
w.showHeader=spec=>{w.headerRequests=[];root.render(<View key={++serial} initial={spec}/>);};
