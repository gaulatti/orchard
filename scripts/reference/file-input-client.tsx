import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {FileInput} from 'frozen/file-input';

const state=window as any,root=createRoot(document.getElementById('root')!);
let serial=0,current:any;
state.fileInputEvents=[];
const files=(input:HTMLInputElement)=>Array.from(input.files??[]).map(f=>({name:f.name,size:f.size,type:f.type,lastModified:f.lastModified}));
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);current=spec;
 state.updateFileInput=(patch:any)=>flushSync(()=>setSpec((old:any)=>({...old,...patch})));
 const ref=React.useCallback((input:HTMLInputElement|null)=>{state.originalFileInput=input;},[]);
 const {viewportWidth,rootFontSize,rtl,wrapperWidth,unmounted,...props}=spec;
 return <><button id="before" style={{position:'absolute',left:16,top:220}}>Before</button>
  <form id="original-form" onSubmit={e=>e.preventDefault()} style={{width:wrapperWidth??384,margin:32}}>
   <label htmlFor="original-file">Original fictional attachment</label>
   {!unmounted&&<FileInput id="original-file" name="original-attachment" aria-describedby="original-help" {...props} ref={ref}
    onInput={e=>state.fileInputEvents.push({kind:'input',trusted:e.nativeEvent.isTrusted,sameRef:e.currentTarget===state.originalFileInput,files:files(e.currentTarget)})}
    onChange={e=>state.fileInputEvents.push({kind:'change',trusted:e.nativeEvent.isTrusted,sameRef:e.currentTarget===state.originalFileInput,files:files(e.currentTarget)})}
    onCancel={e=>state.fileInputEvents.push({kind:'cancel',trusted:e.nativeEvent.isTrusted,sameRef:e.currentTarget===state.originalFileInput,files:files(e.currentTarget)})}/>}
   <p id="original-help">Choose fictional local files.</p>
  </form><button id="after" style={{position:'absolute',left:160,top:220}}>After</button></>;
}
state.showFileInput=(spec:any={})=>{
 state.fileInputEvents=[];document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';document.documentElement.dir=spec.rtl?'rtl':'ltr';
 flushSync(()=>root.render(<View key={++serial} initial={spec}/>));
};
state.fileInputSpec=()=>current;
state.showFileInput();
