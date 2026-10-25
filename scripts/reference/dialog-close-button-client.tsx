import React from 'react';
import {createRoot} from 'react-dom/client';
import {DialogCloseButton} from 'frozen/dialog-close-button';

const root = createRoot(document.getElementById('root')!);
window.showCloseButton = spec => {
  root.render(<DialogCloseButton key={spec.id} aria-label={spec.label ?? 'Close preview'} iconSize={spec.iconSize}
    disabled={spec.disabled} className={spec.className} onClick={() => window.closeRequests.push(spec.id)} />);
};
window.closeRequests = [];
