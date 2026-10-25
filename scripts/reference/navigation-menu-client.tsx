import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {NavigationMenu} from 'frozen/navigation-menu';

const state=window as any,root=createRoot(document.getElementById('root')!);
let serial=0;
function View({initial}:any) {
  const [spec,setSpec]=React.useState(initial);
  const retained=React.useRef<any[]>([]);
  const home={href:spec.href??'#fictional-home',label:'Home',marker:'original-home'};
  const links=[
    {href:'#fictional-buttons',label:spec.label??'Buttons',description:spec.noDescriptions?undefined:spec.description??'Reusable fictional buttons.',marker:'original-buttons'},
    {href:'/fictional/forms?kind=one%2Ftwo#fields',label:'Forms',description:spec.noDescriptions?undefined:'Fictional input and selection controls.',marker:'original-forms'},
    {href:'https://example.invalid/library',label:'External library',description:spec.noDescriptions?undefined:'Fictional external destination.',external:true,marker:'original-external'}
  ];
  const group={trigger:spec.trigger??'Components',href:spec.groupHref??'#ignored-group-destination',items:spec.emptyGroup?[]:links};
  let entries:any[]=spec.empty?[]:spec.linksOnly?[home,{href:'#fictional-docs',label:'Documentation',marker:'original-docs'}]:[
    home,group,{trigger:'Layout',items:[{href:'#fictional-header',label:'Header',description:'Fictional page composition.',marker:'original-header'}]},
    {href:'#fictional-docs',label:'Documentation',marker:'original-docs'}
  ];
  if(spec.onlyLayout)entries=[{trigger:'Layout',items:[{href:'#fictional-header',label:'Header',description:'Fictional page composition.',marker:'original-header'}]}];
  if(spec.prepend)entries.unshift({trigger:'Before',items:[{href:'#fictional-before',label:'Before original',marker:'original-before'}]});
  if(spec.reorder)entries=[...entries].reverse();
  if(spec.retainedEntries)entries=retained.current;
  else retained.current=entries;
  state.navigationEntries=entries;
  state.updateNavigation=(patch:any)=>flushSync(()=>setSpec((previous:any)=>({...previous,...patch})));
  const originals=entries.flatMap(e=>'items' in e?e.items:[e]);
  const renderLink=spec.custom?(props:any)=>{
    state.navigationRenderCalls.push({marker:props.item.marker,original:originals.includes(props.item),hasOnClick:typeof props.onClick==='function',href:props.item.href,description:props.item.description??null});
    return <a href={props.item.href} className={props.className} data-source-marker={props.item.marker} data-custom="true" onClick={event=>{
      event.preventDefault();state.navigationEvents.push({kind:'custom',marker:props.item.marker,original:originals.includes(props.item),hasOnClick:typeof props.onClick==='function'});
      if(spec.throwCustom)throw new Error('fictional navigation callback failed');
      if(!spec.ignoreClose)props.onClick?.();
    }}>{props.children}{spec.customInput&&<input aria-label={'Original '+props.item.marker} defaultValue="retained original text" style={{width:90,fontSize:12}}/>}</a>;
  }:undefined;
  return <>
    <button id="outside-before" style={{position:'absolute',left:740,top:20}}>Outside before</button>
    <div id="navigation-host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:spec.left??20,top:spec.top??20,width:spec.width??680}}><NavigationMenu entries={entries} renderLink={renderLink}/></div>
    <button id="outside-after" style={{position:'absolute',left:740,top:90}}>Outside after</button>
  </>;
}
state.showNavigation=(spec:any)=>{state.navigationEvents=[];state.navigationRenderCalls=[];flushSync(()=>root.render(<View key={++serial} initial={spec}/>));};
// Observe the original anchor attributes and source handlers without leaving
// the fixture or opening an external tab. This runs after React's own handler.
document.addEventListener('click',event=>{const link=(event.target as Element).closest?.('a');if(!link)return;state.navigationEvents.push({kind:'anchor',href:link.getAttribute('href'),target:link.getAttribute('target'),rel:link.getAttribute('rel')});event.preventDefault();});
