import React from 'react';
import {createRoot} from 'react-dom/client';
import {Header} from 'frozen/header';
import {AppShell} from 'frozen/app-shell';

const root = createRoot(document.getElementById('root')!);
root.render(
  <AppShell
    header={<Header brand={{href: '#brand', brandName: 'Fictional library'}} navigation={[{href: '#home', label: 'Home'}, {href: '#devices', label: 'Devices'}]} />}
    footer={<div data-composition-footer style={{height: 32, flexShrink: 0}} />}
  >
    <input data-composition-body defaultValue="Original body" style={{width: 160, height: 32, flexShrink: 0, alignSelf: 'flex-start'}} />
  </AppShell>
);
