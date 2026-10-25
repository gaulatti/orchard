import React from 'react';
import {createRoot} from 'react-dom/client';
import {PageFrame} from 'frozen/page-frame';

const root = createRoot(document.getElementById('root')!);
window.showPageFrame = spec => {
  root.render(<div id="host" style={{width: spec.hostWidth, marginLeft: 20}}>
    <PageFrame as={spec.element ?? 'div'} width={spec.width} gutter={spec.gutter} verticalSpacing={spec.verticalSpacing}
      aria-label={spec.name} style={spec.style}>
      {spec.empty ? null : <div data-body="true" style={{height: 40, background: '#b9c7d1'}}/>}
    </PageFrame>
  </div>);
};
