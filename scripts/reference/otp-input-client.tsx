import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {OtpInput} from 'frozen/otp-input';

const root=createRoot(document.getElementById('root')!);let props:any={},epoch=0;
function Harness({config}:{config:any}){
 const [value,setValue]=React.useState(config.value??'');
 (window as any).setCallerValue=(next:string)=>flushSync(()=>setValue(next));
 (window as any).callerValue=()=>value;
 return <OtpInput {...config} value={value} onChange={config.noCallback?undefined:(next:string)=>{(window as any).requests.push(next);if(config.accept!==false)setValue(next);}}/>;
}
(window as any).showOtp=(next:any)=>{props=next;epoch++;document.getElementById('root')!.dir=next.dir??'ltr';(window as any).requests=[];flushSync(()=>root.render(<Harness key={epoch} config={props}/>));};
(window as any).patchOtp=(next:any)=>{props={...props,...next};flushSync(()=>root.render(<Harness key={epoch} config={props}/>));};
(window as any).localPaste=(index:number,text:string)=>{const data=new DataTransfer();data.setData('text',text);document.querySelectorAll('input')[index].dispatchEvent(new ClipboardEvent('paste',{bubbles:true,cancelable:true,clipboardData:data}));};
(window as any).localEdit=(index:number,text:string)=>{const input=document.querySelectorAll('input')[index];Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value')!.set!.call(input,text);input.dispatchEvent(new InputEvent('input',{bubbles:true,inputType:'insertText',data:text}));};
(window as any).inputEvents=[];for(const type of ['input','keydown','paste','focus'])document.addEventListener(type,event=>(window as any).inputEvents.push({type,trusted:event.isTrusted,key:(event as KeyboardEvent).key??null}),true);
