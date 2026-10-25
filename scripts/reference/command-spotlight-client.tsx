import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {CommandSpotlight} from 'frozen/command-spotlight';
import {Modal} from 'frozen/modal';

const state=window as any,root=createRoot(document.getElementById('root')!);let serial=0;
const defaults=()=>[
 {id:'alpha',title:'Original alpha command',description:'Original navigation description',group:'Navigation',keywords:['coast','quick route'],shortcut:['Ctrl','1'],icon:'square',callback:'sync',href:'/ignored-original-link'},
 {id:'beta',title:'Original beta command',description:'Original tools description',group:'Tools',keywords:['portrait'],callback:'sync'},
 {id:'gamma',title:'Original gamma command',description:'Original second navigation description',group:' Navigation ',shortcut:['G'],icon:'circle',callback:'sync'},
 {id:'disabled',title:'Original disabled command',description:'Original unavailable description',group:'Tools',keywords:['locked'],disabled:true,callback:'sync'},
 {id:'ungrouped',title:'Original ungrouped command',keywords:['miscellaneous'],callback:'sync'},
 {id:'unicode',title:'Straße 東京 é العربية',description:'Original Unicode action',group:'  ',keywords:['İstanbul','ΣΟΣ'],shortcut:['Alt','U'],callback:'sync'}
];
state.spotlightDefaults=defaults;
let models:any[]=[];
const icon=(kind:string)=>kind==='editor'?<input id="original-action-icon-editor" aria-label="Original icon editor" defaultValue="Original icon edit"/>:kind==='circle'?<svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="7" fill="none" stroke="currentColor" strokeWidth="2"/></svg>:<svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 5H19V19H5Z" fill="none" stroke="currentColor" strokeWidth="2"/></svg>;
function makeAction(spec:any){const {icon:kind,callback,...original}=spec;const action:any={...original};if(kind)action.icon=icon(kind);if(callback)action.onSelect=()=>{
 state.spotlightEvents.push({kind:'own',id:action.id,sameOriginal:state.currentSpotlightActions.includes(action)});
 if(callback==='throw')throw new Error('Original synchronous action failure');
 if(callback==='resolve')return Promise.resolve('Original asynchronous result');
 if(callback==='reject')return Promise.reject(new Error('Original asynchronous action failure'));
 if(callback==='pending')return new Promise((resolve,reject)=>{state.resolveSpotlightAction=resolve;state.rejectSpotlightAction=reject;});
};return action;}
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);state.spotlightSpec=()=>spec;
 state.updateSpotlight=(patch:any)=>flushSync(()=>setSpec((old:any)=>{const next={...old,...patch};if('actions'in patch)next.actions=patch.actions.map(makeAction);return next;}));
 state.mutateSpotlightAction=(index:number,patch:any)=>flushSync(()=>setSpec((old:any)=>{Object.assign(old.actions[index],patch);return{...old};}));
 state.reorderSpotlightActions=(indices:number[])=>flushSync(()=>setSpec((old:any)=>({...old,actions:indices.map(index=>old.actions[index])})));
 state.replaceSpotlightOriginal=(index:number,patch:any)=>flushSync(()=>setSpec((old:any)=>({...old,actions:old.actions.map((a:any,i:number)=>i===index?{...a,...patch}:a)})));
 const {actions,open,acceptOpen=false,openCallback=true,outerCallback=true,throwOuter=false,showTrigger=true,triggerLabel='Search',hotkey='k',placeholder='Search commands...',emptyMessage='No matching commands.',className,dialogClassName,overlayClassName,triggerClassName,unmounted,editor}=spec;
 models=actions;state.currentSpotlightActions=actions;
 const changed=(value:boolean)=>{state.spotlightEvents.push({kind:'open',value});if(acceptOpen&&typeof open==='boolean')flushSync(()=>setSpec((old:any)=>({...old,open:value})));};
 const selected=(action:any)=>{state.spotlightEvents.push({kind:'outer',id:action.id,sameOriginal:models.includes(action),title:action.title,href:action.href??null});if(throwOuter)throw new Error('Original outer callback failure');};
 const component=!unmounted&&<CommandSpotlight actions={actions} open={open} onOpenChange={openCallback?changed:undefined} onActionSelect={outerCallback?selected:undefined} showTrigger={showTrigger} triggerLabel={triggerLabel} hotkey={hotkey} placeholder={placeholder} emptyMessage={emptyMessage} className={className} dialogClassName={dialogClassName} overlayClassName={overlayClassName} triggerClassName={triggerClassName}/>;
 return <main id="original-spotlight-host" style={{padding:32,width:spec.wrapperWidth??'100%'}}>
  {spec.parentModal?<Modal title="Original parent modal" isOpen={spec.parentOpen!==false} onClose={()=>{state.spotlightEvents.push({kind:'parent-close'});flushSync(()=>setSpec((old:any)=>({...old,parentOpen:false})));}}>{component}<button id="original-parent-last">Original parent last action</button></Modal>:component}
  {editor==='input'?<input id="original-background-editor" aria-label="Original background input" defaultValue="Original background edit"/>:editor==='textarea'?<textarea id="original-background-editor" aria-label="Original background textarea" defaultValue="Original background edit"/>:editor==='select'?<select id="original-background-editor" aria-label="Original background select"><option>Original option</option></select>:editor==='editable'?<div id="original-background-editor" aria-label="Original background editable" contentEditable suppressContentEditableWarning>Original background edit</div>:editor==='button'?<button id="original-background-editor">Original background button</button>:null}
 </main>;
}
state.showSpotlight=(spec:any={})=>{state.spotlightEvents=[];state.spotlightErrors=[];state.spotlightExternalOpens=[];document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';document.documentElement.dir=spec.rtl?'rtl':'ltr';flushSync(()=>root.render(<View key={++serial} initial={{...spec,actions:(spec.actions??defaults()).map(makeAction)}}/>));};
window.open=((url:any,target:any,features:any)=>{state.spotlightExternalOpens.push({url,target,features});return null;}) as any;
const originalError=console.error.bind(console);console.error=(...args:any[])=>{state.spotlightErrors.push(args.map(a=>a instanceof Error?a.message:String(a)));originalError(...args);};
