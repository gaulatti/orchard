'use client';

import React from 'react';

import { resolveTheme } from './theme-script';
import type { ThemeMode, ThemeProviderProps } from './types';

interface ThemeContextValue {
  theme: ThemeMode;
  resolvedTheme: 'light' | 'dark';
  setTheme: (theme: ThemeMode) => void;
  cycleTheme: () => void;
  storageKey: string;
}

const ThemeContext = React.createContext<ThemeContextValue | null>(null);

function nextTheme(theme: ThemeMode): ThemeMode {
  if (theme === 'light') {
    return 'dark';
  }

  if (theme === 'dark') {
    return 'system';
  }

  return 'light';
}

function getSystemPreference() {
  if (typeof window === 'undefined') {
    return false;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function readStoredTheme(storageKey: string, fallback: ThemeMode) {
  if (typeof window === 'undefined') {
    return fallback;
  }

  const saved = window.localStorage.getItem(storageKey);
  return saved === 'light' || saved === 'dark' || saved === 'system' ? saved : fallback;
}

function applyDocumentTheme(theme: ThemeMode) {
  if (typeof document === 'undefined') {
    return resolveTheme(theme, false);
  }

  const resolved = resolveTheme(theme, getSystemPreference());
  document.documentElement.classList.toggle('dark', resolved === 'dark');
  return resolved;
}

export function ThemeProvider({ children, defaultTheme = 'system', storageKey = 'theme' }: ThemeProviderProps) {
  const [theme, setThemeState] = React.useState<ThemeMode>(defaultTheme);
  const [resolvedTheme, setResolvedTheme] = React.useState<'light' | 'dark'>(() => resolveTheme(defaultTheme, false));

  React.useEffect(() => {
    const initialTheme = readStoredTheme(storageKey, defaultTheme);
    setThemeState(initialTheme);
    setResolvedTheme(applyDocumentTheme(initialTheme));
  }, [defaultTheme, storageKey]);

  React.useEffect(() => {
    if (typeof window === 'undefined') {
      return undefined;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const sync = () => setResolvedTheme(applyDocumentTheme(theme));

    sync();
    mediaQuery.addEventListener('change', sync);
    return () => mediaQuery.removeEventListener('change', sync);
  }, [theme]);

  const setTheme = React.useCallback(
    (value: ThemeMode) => {
      setThemeState(value);
      setResolvedTheme(applyDocumentTheme(value));

      if (typeof window !== 'undefined') {
        window.localStorage.setItem(storageKey, value);
      }
    },
    [storageKey]
  );

  const cycleTheme = React.useCallback(() => {
    setTheme(nextTheme(theme));
  }, [setTheme, theme]);

  const value = React.useMemo(
    () => ({
      theme,
      resolvedTheme,
      setTheme,
      cycleTheme,
      storageKey
    }),
    [cycleTheme, resolvedTheme, setTheme, storageKey, theme]
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = React.useContext(ThemeContext);

  if (!context) {
    throw new Error('useTheme must be used inside a ThemeProvider');
  }

  return context;
}
