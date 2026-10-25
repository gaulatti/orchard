import React from 'react';
import {createRoot} from 'react-dom/client';
import {ThemeToggle} from 'frozen/theme-toggle';
import {ThemeProvider,useTheme} from 'frozen/theme-provider';

const root=createRoot(document.getElementById('root')!);
window.overrideClicks=0;
function Stage({spec}:any){
 const context=useTheme();
 window.themeContext=context;
 const props:any={id:'source-toggle',size:spec.size,variant:spec.variant,disabled:spec.disabled};
 if('name' in spec)props['aria-label']=spec.name;
 if('title' in spec)props.title=spec.title;
 if(spec.overrideClick)props.onClick=()=>{window.overrideClicks++;};
 return <div id="host" dir={spec.rtl?'rtl':'ltr'} style={{position:'absolute',left:24,top:24}}><ThemeToggle {...props}/></div>;
}
let key=0;
window.showThemeToggle=(spec:any)=>{
 localStorage.clear();window.overrideClicks=0;
 root.render(<ThemeProvider key={++key} defaultTheme={spec.mode} storageKey="fixture-theme"><Stage spec={spec}/></ThemeProvider>);
};
