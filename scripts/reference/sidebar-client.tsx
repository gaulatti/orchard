import React from 'react';
import {createRoot} from 'react-dom/client';
import {Sidebar} from '@frozen/sidebar';

const root=createRoot(document.getElementById('root')!);let model:any,items:any[]=[];
const mark=<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 4h16v16H4zM8 8h8v8H8z"/></svg>;
function content(value:any,kind:string):any {
 if(value&&typeof value==='object'){
  if(value.kind==='icon')return value.size?<svg width={value.size} height={value.size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 4h16v16H4z"/></svg>:mark;
  if(value.kind==='badge')return <span data-consumer-badge style={{fontSize:12,lineHeight:'16px',padding:'2px 6px',borderRadius:4,background:'var(--color-sand)'}}>{value.text}</span>;
  if(value.kind==='slot')return <div data-consumer-slot={kind} style={{fontSize:value.size??16,lineHeight:'24px',fontWeight:value.weight??500}}>{value.text}</div>;
  if(value.kind==='button')return <button data-consumer-slot={kind} onClick={()=>window.events.push({kind:'slot',slot:kind})}>{value.text}</button>;
 }
 return value;
}
function records(values:any[]):any[]{return values.map(item=>({...item,icon:content(item.icon,'icon'),badge:content(item.badge,'badge'),items:item.items?records(item.items):item.items}));}
function find(values:any[],id:string):any {for(const item of values){if(item.id===id)return item;const child=item.items&&find(item.items,id);if(child)return child;}return undefined;}
function same(item:any){return find(items,item.id)===item;}
function render(){root.render(<Sidebar items={items} collapsed={model.collapsed} header={content(model.header,'header')} footer={content(model.footer,'footer')} className={model.className} onItemClick={item=>window.events.push({kind:'item',id:item.id,href:item.href,same:same(item)})} renderLink={model.customLink?({item,className,children,onClick})=>{
 const prepared=(children as any).props.children,body=prepared[1]?.props?.children;
 window.renderCalls.push({id:item.id,href:item.href,className,same:same(item),iconSame:!item.icon||prepared[0]?.props?.children===item.icon,badgeSame:!!model.collapsed||!item.badge||body?.[1]?.props?.children===item.badge,labelSame:!!model.collapsed||body?.[0]?.props?.children===item.label,collapsed:!!model.collapsed});
 return <a href={item.href} className={className} data-custom-link={item.id} onClick={e=>{e.preventDefault();window.events.push({kind:'custom',id:item.id,href:item.href,same:same(item)});onClick?.();}}>{children}</a>}:undefined}/>);}
window.events=[];window.renderCalls=[];window.inputEvents=[];
for(const type of ['keydown','keyup','pointermove','pointerdown','pointerup','click','focusin'])document.addEventListener(type,event=>{
 const target=event.target as HTMLElement,action=target.closest('a,button')??target;
 // Document-level key receipts need the target kind, never the inline bundle
 // and page-wide content that BODY.textContent would include.
 const text=action===document.body||action===document.documentElement?null:action.textContent;
 window.inputEvents.push({type,trusted:event.isTrusted,tag:action.tagName,id:action.id,href:action.getAttribute('href'),text,key:(event as KeyboardEvent).key??null,x:(event as PointerEvent).clientX??null,y:(event as PointerEvent).clientY??null,buttons:(event as PointerEvent).buttons??null,time:event.timeStamp});
},true);
window.showSidebar=(value:any)=>{model=value;items=records(value.items);window.items=items;render();};
window.updateSidebar=(change:any)=>{
 if(change.collapsed!==undefined)model.collapsed=change.collapsed;
 if(change.active){for(const [id,active]of Object.entries(change.active)){const item=find(items,id);if(item)item.active=active;}}
 if(change.label){for(const[id,label]of Object.entries(change.label)){const item=find(items,id);if(item)item.label=label;}}
 if(change.replace){items=records(change.replace);window.items=items;}
 if(change.reorder)items=change.reorder.map((id:string)=>find(items,id));
 if(change.remove)items=items.filter(item=>!change.remove.includes(item.id));
 if(change.append)items.push(...records(change.append));
 render();
};
