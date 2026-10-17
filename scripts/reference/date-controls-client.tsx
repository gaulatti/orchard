import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Calendar, DatePicker} from 'frozen/date-picker';
import {DateRangePicker} from 'frozen/date-range-picker';

// Only the consumer is supplied here. Calendar, DatePicker, DateRangePicker,
// Popover and their dependencies execute unchanged from the pinned repository.
const w=window as any, root=createRoot(document.getElementById('root')!);
const local=(value:Date)=>[String(value.getFullYear()).padStart(4,'0'),String(value.getMonth()+1).padStart(2,'0'),String(value.getDate()).padStart(2,'0')].join('-');
const date=(value:string|null|undefined)=>value?new Date(value.includes('T')?value:value+'T00:00:00'):null;
let serial=0;
w.actions=[];w.inputEvents=[];
for(const type of ['pointerdown','pointerup','keydown','keyup','click'])document.addEventListener(type,event=>w.inputEvents.push({type,trusted:event.isTrusted,key:(event as KeyboardEvent).key??null}),true);
function render(){
 const s=w.spec;
 const changed=(value:any)=>{
  const next=s.kind==='range'?value:value?local(value):null;
  w.actions.push({value:next,iso:value instanceof Date?value.toISOString():null});
  if(s.accept!==false){s.value=next;render();}
 };
 const blocked=s.blockWeekends?(value:Date)=>[0,6].includes(value.getDay()):undefined;
 flushSync(()=>root.render(s.kind==='calendar'
  ? <Calendar key={s.mount} value={date(s.value)} minDate={date(s.minDate)??undefined} maxDate={date(s.maxDate)??undefined} disabled={blocked} onChange={changed}/>
  : s.kind==='picker'
   ? <DatePicker key={s.mount} value={date(s.value)} minDate={date(s.minDate)??undefined} maxDate={date(s.maxDate)??undefined} disabledDates={blocked} disabled={s.disabled} placeholder={s.placeholder} aria-label={s.ariaLabel} onChange={changed}/>
   : <DateRangePicker key={s.mount} value={s.value??{}} onChange={changed} placeholder={s.placeholder} presets={s.presets}/>));
}
w.show=(spec:any)=>{w.actions=[];w.inputEvents=[];w.spec={...spec,mount:++serial};render();};
w.update=(patch:any)=>{Object.assign(w.spec,patch);render();};
w.dateTypography=()=>{
 const entries:any[]=[],canvas=document.createElement('canvas'),context=canvas.getContext('2d')!;
 const add=(role:string,element:HTMLElement|null)=>{
  if(!element)return;
  const style=getComputedStyle(element),nodes=[...element.childNodes].filter(node=>node.nodeType===Node.TEXT_NODE&&node.textContent?.trim());
  if(!nodes.length)return;
  const rectangle=element.getBoundingClientRect(),range=document.createRange();range.setStart(nodes[0],0);range.setEnd(nodes.at(-1)!,nodes.at(-1)!.textContent!.length);
  const textRectangle=range.getBoundingClientRect(),displayText=style.textTransform==='uppercase'?element.textContent!.trim().toUpperCase():element.textContent!.trim();
  context.font=style.fontStyle+' '+style.fontWeight+' '+style.fontSize+' '+style.fontFamily;context.textBaseline='alphabetic';
  const metrics=context.measureText(displayText),ascent=metrics.fontBoundingBoxAscent,descent=metrics.fontBoundingBoxDescent;
  if(!Number.isFinite(ascent)||!Number.isFinite(descent)||ascent+descent<=0)throw Error('Missing browser font metrics: '+role);
  const verticalScale=textRectangle.height/(ascent+descent);
  entries.push({role,text:element.textContent!.trim(),displayText,baseline:textRectangle.y+ascent*verticalScale,ascent,descent,verticalScale,
   box:{x:rectangle.x,y:rectangle.y,width:rectangle.width,height:rectangle.height},textBox:{x:textRectangle.x,y:textRectangle.y,width:textRectangle.width,height:textRectangle.height},
   fontSize:parseFloat(style.fontSize),fontWeight:Number(style.fontWeight),lineHeight:parseFloat(style.lineHeight),letterSpacing:style.letterSpacing,numerals:style.fontVariantNumeric});
 };
 const days=[...document.querySelectorAll<HTMLButtonElement>('button[aria-pressed]')],calendar=days[0]?.parentElement?.parentElement;
 if(calendar){add('heading',calendar.children[0].querySelector('span'));[...calendar.children[1].children].forEach((item,index)=>add('weekday-'+index,item as HTMLElement));for(const day of days)add('day-'+day.textContent,day);}
 [...document.querySelectorAll<HTMLButtonElement>('#root button[data-state]')].forEach((button,index)=>add('trigger-'+index,button.querySelector('span')));
 if(w.spec.kind==='range'){
  const card=document.querySelector('#root>[role=group]')?.firstElementChild;
  [...(card?.children??[])].filter(item=>item.tagName==='DIV').forEach((field,index)=>add('range-label-'+index,field.querySelector(':scope > span')));
  [...document.querySelectorAll<HTMLButtonElement>('#root button:not([data-state])')].forEach((button,index)=>add('action-'+index,button));
 }
 add('clear',([...document.querySelectorAll<HTMLButtonElement>('[data-radix-popper-content-wrapper] button')].find(button=>button.textContent==='Clear date'))??null);
 return entries;
};
w.measure=()=>{
 const box=(e:Element|null)=>{if(!e)return null;const r=e.getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};};
 const paint=(e:Element|null)=>e?Object.fromEntries(['fontFamily','fontSize','fontWeight','lineHeight','letterSpacing','color','backgroundColor','borderColor','borderWidth','borderRadius','boxShadow','opacity','transform','paddingTop','paddingRight','paddingBottom','paddingLeft','rowGap','columnGap','direction','transitionDuration','transitionTimingFunction'].map(key=>[key,(getComputedStyle(e) as any)[key]])):null;
 const dayButtons=[...document.querySelectorAll<HTMLButtonElement>('button[aria-pressed]')];
 const calendar=dayButtons[0]?.parentElement?.parentElement;
 const children=calendar?[...calendar.children]:[];
 const dialog=document.querySelector('[role=dialog]');
 const popup=document.querySelector('[data-radix-popper-content-wrapper]');
 const rangeCard=document.querySelector('#root>[role=group]')?.firstElementChild??null;
 const clearAction=[...(popup?.querySelectorAll<HTMLButtonElement>('button')??[])].find(button=>button.textContent==='Clear date');
 return{
  viewport:{compatMode:document.compatMode,innerWidth,innerHeight,scrollX,scrollY,scrollWidth:Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)},
  kind:w.spec.kind,value:w.spec.value??null,actions:[...w.actions],root:{box:box(document.querySelector('#root>div')),paint:paint(document.querySelector('#root>div'))},
  calendar:calendar?{box:box(calendar),month:children[0].querySelector('span')?.textContent,header:{box:box(children[0]),paint:paint(children[0].querySelector('span'))},weekdays:[...children[1].children].map(e=>({text:e.textContent,box:box(e),paint:paint(e)})),cellCount:children[2].children.length,blankCount:[...children[2].children].filter(e=>e.tagName!=='BUTTON').length,
   days:dayButtons.map(e=>({day:Number(e.textContent),label:e.getAttribute('aria-label'),pressed:e.getAttribute('aria-pressed')==='true',disabled:e.disabled,focused:document.activeElement===e,box:box(e),paint:paint(e)}))}:null,
  triggers:[...document.querySelectorAll<HTMLButtonElement>('#root button[data-state]')].map(e=>({name:e.getAttribute('aria-label'),text:e.textContent,disabled:e.disabled,state:e.dataset.state,box:box(e),paint:paint(e)})),
  popup:popup?{box:box(popup.firstElementChild),paint:paint(popup.firstElementChild),side:popup.firstElementChild?.getAttribute('data-side')}:null,
  dialog:dialog?{label:dialog.getAttribute('aria-label'),box:box(dialog)}:null,
  dialogs:[...document.querySelectorAll('[role=dialog]')].map(e=>({label:e.getAttribute('aria-label'),box:box(e)})),
  rangeCard:rangeCard?{box:box(rangeCard),paint:paint(rangeCard)}:null,
  ...(clearAction?{clearAction:{box:box(clearAction),parent:box(clearAction.parentElement),bottomGap:clearAction.parentElement!.getBoundingClientRect().bottom-clearAction.getBoundingClientRect().bottom}}:{}),
  buttons:[...document.querySelectorAll<HTMLButtonElement>('#root button')].map(e=>({name:e.getAttribute('aria-label'),text:e.textContent,disabled:e.disabled,box:box(e),paint:paint(e)})),
  focused:{tag:document.activeElement?.tagName,label:document.activeElement?.getAttribute('aria-label'),text:document.activeElement instanceof HTMLButtonElement?document.activeElement.textContent:null,id:document.activeElement?.id},
  horizontalOverflow:Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)>innerWidth,
 };
};
