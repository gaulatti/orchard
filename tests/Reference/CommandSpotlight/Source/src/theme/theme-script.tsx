import React from 'react';

import { resolveTheme } from '../core/index';
import type { ThemeMode } from './types';

export { resolveTheme } from '../core/index';

export function getThemeScript(storageKey = 'theme') {
  return `(function(){try{var key=${JSON.stringify(storageKey)};var saved=localStorage.getItem(key);var theme=(saved==="light"||saved==="dark"||saved==="system")?saved:"system";var dark=theme==="dark"||(theme==="system"&&window.matchMedia("(prefers-color-scheme: dark)").matches);document.documentElement.classList.toggle("dark",dark);}catch(e){}})();`;
}

export function BleeckerThemeScript({ storageKey = 'theme' }: { storageKey?: string }) {
  return <script dangerouslySetInnerHTML={{ __html: getThemeScript(storageKey) }} />;
}
