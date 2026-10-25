import {flushSync} from 'react-dom';
import './alert-dialog-client';

// Freeze short reversing transitions immediately after the original commit.
(window as any).commitAlertDialog=(callback:()=>void)=>flushSync(callback);
