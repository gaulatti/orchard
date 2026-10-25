import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {Menubar} from 'frozen/menubar';

const root = createRoot(document.getElementById('root')!);
const state = window as any;
let serial = 0;

function View({initial}: any) {
  const [spec, setSpec] = React.useState(initial);
  state.updateMenubar = (patch: any) => flushSync(() => setSpec((previous: any) => ({...previous, ...patch})));
  const change = (key: string, value: any) => {
    state.menubarEvents.push({key, value});
    if (spec.accept) setSpec((previous: any) => ({...previous, [key]: value}));
  };
  const action = (name: string) => () => {
    state.menubarEvents.push(name);
    if (spec.throwAction) throw new Error('fictional menubar callback failed');
  };
  const items: any[] = spec.emptyMenu ? [] : [
    {type:'item', label:spec.label ?? 'New fictional draft', shortcut:'Ctrl+N', onSelect:action('new'),
      icon:spec.rich ? <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 5v14M5 12h14" fill="none" stroke="currentColor" strokeWidth="2"/></svg> : undefined},
    {type:'item', label:'Unavailable action', disabled:true, onSelect:action('disabled')},
    {type:'separator'},
    {type:'checkbox', label:'Show fictional guides', shortcut:'Ctrl+G', checked:spec.checked ?? false, onCheckedChange:(value:boolean)=>change('checked',value)},
    {type:'radioGroup', value:spec.radio ?? 'a', onValueChange:(value:string)=>change('radio',value), items:[
      {type:'radio', label:'Fictional Alpha mode', value:'a'},
      {type:'radio', label:'Fictional Beta mode', value:'b', shortcut:'Alt+B'},
      {type:'radio', label:'Unavailable mode', value:'c', disabled:true}
    ]},
    {type:'separator'},
    {type:'sub', label:'More fictional choices', items:[
      {type:'item', label:'Nested original action', onSelect:action('nested')},
      {type:'sub', label:'Deeper fictional choices', items:[{type:'item', label:'Deep original action', onSelect:action('deep')}]}
    ]},
    {type:'sub', label:'Unavailable submenu', disabled:true, items:[{type:'item', label:'Disabled parent child', onSelect:action('disabled-parent-child')}]}
  ];
  const menus = spec.emptyBar ? [] : spec.onlyEdit ? [{trigger:'Edit', items:[{type:'item', label:'Edit original action', onSelect:action('edit')}]}] : [
    {trigger:spec.trigger ?? 'File', items},
    {trigger:'Edit', items:[{type:'item', label:'Edit original action', onSelect:action('edit')}]}
  ];
  if (spec.prepend) menus.unshift({trigger:'Before',items:[{type:'item', label:'Prepended original action',onSelect:action('before')}]});
  return <>
    <button id="outside-before" type="button" style={{position:'absolute',left:700,top:20}}>Outside before</button>
    <div id="fixture" dir={spec.rtl ? 'rtl' : 'ltr'} style={{position:'absolute',left:spec.left ?? 20,top:spec.top ?? 20,width:spec.width ?? 360,fontSize:spec.fontSize ?? 16}}>
      <Menubar menus={menus}/>
    </div>
    <button id="outside-after" type="button" style={{position:'absolute',left:700,top:80}}>Outside after</button>
  </>;
}

state.menubarEvents = [];
state.showMenubar = (spec: any) => {
  state.menubarEvents = [];
  flushSync(() => root.render(<View key={++serial} initial={spec}/>));
};
