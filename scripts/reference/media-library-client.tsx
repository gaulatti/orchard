import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {MediaLibrary} from 'frozen/media-library';

const state=window as any,root=createRoot(document.getElementById('root')!);
let serial=0,current:any;
const svg=(width:number,height:number,color:string,accent:string)=>`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}"><rect width="${width}" height="${height}" fill="${color}"/><path d="M0 ${height*.7} L${width*.35} ${height*.25} L${width*.65} ${height*.8} L${width} ${height*.4} V${height} H0Z" fill="${accent}"/><circle cx="${width*.75}" cy="${height*.22}" r="${Math.min(width,height)*.1}" fill="#e8c9a2"/></svg>`;
state.mediaAssets=[
 {url:'https://fixtures.orchard.test/coast-thumb.svg',width:320,height:200,svg:svg(320,200,'#24465a','#69888c')},
 {url:'https://fixtures.orchard.test/coast.svg?label=Original%20value#view',width:640,height:400,svg:svg(640,400,'#24465a','#69888c')},
 {url:'https://fixtures.orchard.test/portrait.svg',width:240,height:480,svg:svg(240,480,'#74545d','#bc8c74')},
 {url:'https://fixtures.orchard.test/thumb-only.svg',width:240,height:240,svg:svg(240,240,'#4d665b','#8da080')},
 {url:'https://fixtures.orchard.test/unicode.svg',width:480,height:320,svg:svg(480,320,'#4d4966','#a19cbd')},
 {url:'https://fixtures.orchard.test/broken-thumb.svg',status:404,width:0,height:0,svg:''},
 {url:'https://fixtures.orchard.test/broken-full.svg',status:404,width:0,height:0,svg:''}
];
const defaults=()=>[
 {id:'coast',filename:'fictional-coast.svg',alt:'Original fictional coastal illustration',thumbnailUrl:state.mediaAssets[0].url,url:state.mediaAssets[1].url,createdAt:'2026-03-10T10:00:00.000Z'},
 {id:'portrait',filename:'fictional-portrait.svg',alt:'Original fictional portrait illustration',url:state.mediaAssets[2].url,createdAt:'2026-03-11T00:30:00.000Z'},
 {id:'thumbnail',filename:'fictional-thumbnail-only.svg',thumbnailUrl:state.mediaAssets[3].url},
 {id:'none',filename:'fictional-no-preview.bin'},
 {id:'broken',filename:'fictional-broken-thumb.svg',thumbnailUrl:state.mediaAssets[5].url,url:state.mediaAssets[1].url,alt:'Original broken thumbnail'},
 {id:'unicode',filename:'Straße 東京 é العربية.svg',alt:' ',url:state.mediaAssets[4].url,createdAt:'2026-10-21T00:30:00.000Z'}
];
state.mediaDefaults=defaults;
function View({initial}:any){
 const [spec,setSpec]=React.useState(initial);current=spec;
 state.mediaSpec=()=>spec;
 state.updateMediaLibrary=(patch:any)=>flushSync(()=>setSpec((old:any)=>({...old,...patch})));
 state.mutateMediaItem=(index:number,patch:any)=>flushSync(()=>setSpec((old:any)=>{Object.assign(old.items[index],patch);return{...old};}));
 const {items,searchTerm='',title='Original fictional media',placeholder='Search media...',emptyMessage='No media found.',actions,filter,acceptSearch=false,filterInHost=false,copyCallback=true,deleteCallback=false,unmounted,wrapperWidth,className}=spec;
 state.currentMediaItems=items;
 const shown=filterInHost?items.filter((item:any)=>(item.filename+' '+(item.alt??'')).toLowerCase().includes(searchTerm.toLowerCase())):items;
 const search=(value:string)=>{state.mediaEvents.push({kind:'search',value});if(acceptSearch)flushSync(()=>setSpec((old:any)=>({...old,searchTerm:value})));};
 const actionNode=actions==='editor'?<input id="caller-action-editor" aria-label="Original action editor" defaultValue="Original action edit"/>:actions==='button'?<button id="caller-action" onClick={()=>state.mediaEvents.push({kind:'action'})}>Original caller action</button>:actions==='text'?<span>Original caller actions</span>:actions===0?0:null;
 const filterNode=filter==='editor'?<input id="caller-filter-editor" aria-label="Original filter editor" defaultValue="Original filter edit"/>:filter==='text'?<span>Original caller filter</span>:filter===0?0:null;
 return <main id="original-media-host" style={{width:wrapperWidth??'100%'}}>{!unmounted&&<MediaLibrary title={title} items={shown} searchTerm={searchTerm} onSearchTermChange={search} searchPlaceholder={placeholder} emptyMessage={emptyMessage} className={className} actions={actionNode} filterSlot={filterNode}
   onCopy={copyCallback?(text:string)=>{state.mediaEvents.push({kind:'copy',text});if(spec.throwCopy)throw new Error('Original caller copy failure');}:undefined}
   onDelete={deleteCallback?(id:string)=>{state.mediaEvents.push({kind:'delete',id});if(spec.throwDelete)throw new Error('Original caller delete failure');}:undefined}/>}</main>;
}
state.showMediaLibrary=(spec:any={})=>{
 state.mediaEvents=[];state.clipboardEvents=[];document.documentElement.style.fontSize=(spec.rootFontSize??16)+'px';document.documentElement.dir=spec.rtl?'rtl':'ltr';
 flushSync(()=>root.render(<View key={++serial} initial={{items:defaults(),...spec}}/>));
};
Object.defineProperty(navigator,'clipboard',{configurable:true,value:{writeText:(text:string)=>{state.clipboardEvents.push({text});return Promise.resolve();}}});
