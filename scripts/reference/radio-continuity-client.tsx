import React from 'react';
import {createRoot} from 'react-dom/client';
import {flushSync} from 'react-dom';
import {RadioGroup} from 'frozen/radio-group';
const w=window as any,root=createRoot(document.getElementById('root')!);
w.show=(spec:any)=>flushSync(()=>root.render(<RadioGroup {...spec}/>));
w.remove=()=>flushSync(()=>root.render(null));
