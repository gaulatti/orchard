import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {AuthShell} from 'frozen/auth-shell';

const state=window as any,root=createRoot(document.getElementById('root')!);let spec:any={},events:any[]=[],serial=0;
const node=(slot:string,height:number,text:string)=><div data-caller={slot} style={{height,width:'100%',minWidth:0,overflow:'hidden',background:slot==='aside'?'rgba(255,255,255,.08)':'rgba(100,120,140,.08)'}}><span>{text}</span><input aria-label={'Original '+slot+' editor'} defaultValue={'Original '+slot} style={{display:'block',width:'100%',height:24,fontSize:12}}/><button type="button" style={{height:24,fontSize:12}} onClick={()=>events.push({kind:'click',slot})}>Original {slot} action</button></div>;
function slot(name:string,height:number,text:string){const value=spec[name];if(value==='none')return null;if(value==='zero')return 0;if(value==='empty')return '';if(value==='false')return false;if(value==='empty-array')return [];return node(name,height,text);}
function render(){document.documentElement.style.fontSize=(spec.rootFont??16)+'px';document.documentElement.dir='ltr';flushSync(()=>root.render(spec.unmounted?null:<div data-auth-host style={{width:spec.hostWidth??'100%',marginLeft:spec.hostX??0,marginRight:'auto',direction:'ltr'}}><AuthShell dir={spec.rtl?'rtl':'ltr'} key={serial} layout={spec.layout} side={spec.side} asideLabel={spec.asideLabel??'Original membership note'} brand={slot('brand',spec.brandHeight??40,'Original brand')} footer={slot('footer',spec.footerHeight??32,'Original footer')} aside={slot('aside',spec.asideHeight??160,'Original aside')} aria-label="Original access shell" id="original-shell" style={spec.style} {...(spec.overrideData?{'data-layout':'caller-layout','data-side':'caller-side'}:{})}>{spec.children==='zero'?0:spec.children==='none'?null:<form data-original-form onSubmit={e=>{e.preventDefault();events.push({kind:'submit',prevented:true});}}>{node('content',spec.contentHeight??220,'Original fictional access content')}</form>}</AuthShell></div>));}
state.showAuthShell=(next:any)=>{serial++;spec={...next};events=[];render();window.scrollTo(0,0);};
state.updateAuthShell=(patch:any)=>{spec={...spec,...patch};render();};
state.authShellEvents=()=>events.map(e=>({...e}));
