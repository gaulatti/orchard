import type { ReactNode } from 'react';

import type { ThemeMode } from '../core';

export type { ThemeMode } from '../core';

export interface ThemeProviderProps {
  children: ReactNode;
  defaultTheme?: ThemeMode;
  storageKey?: string;
}
