import React from 'react';
import {createRoot} from 'react-dom/client';
import {Select} from 'frozen/select';
import {HeaderSelect} from 'frozen/header-select';
const root=createRoot(document.getElementById('root')!);
const defaults=[{value:'a',label:'Library Alpha'},{value:'b',label:'Library Beta'},{value:'disabled',label:'Disabled library',disabled:true}];
function View({initial}) {
 const [spec,setSpec]=React.useState(initial);window.updateSelect=patch=>setSpec(previous=>({...previous,...patch}));
 const change=value=>{window.selectRequests.push(value);if(spec.accept)setSpec(previous=>({...previous,value}));};
 const icon=spec.icon===true?<div data-icon="original" style={{width:16,height:16,background:'#407fa3'}}/>:spec.icon;
 const props={options:spec.options??defaults,value:spec.value??'',onChange:change,placeholder:spec.placeholder??(spec.header?'Select option':'Select…'),disabled:spec.disabled,required:spec.required,autoFocus:spec.autoFocus,'aria-label':spec.name};
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:spec.left??20,top:spec.top??20,width:spec.width}}>
  {spec.header?<HeaderSelect {...props} icon={icon}/>:<Select {...props} size={spec.size??'md'} startIcon={icon} error={spec.error}/>}
 </div>;
}
window.selectRequests=[];
window.showSelect=spec=>{window.selectRequests=[];root.render(<View key={spec.key??spec.id} initial={spec}/>);};
