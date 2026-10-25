'use client';

import { Monitor, Moon, Sun } from 'lucide-react';

import { useTheme } from '../theme/theme-provider';
import { IconButton, type IconButtonProps } from './icon-button';

export interface ThemeToggleProps extends Omit<IconButtonProps, 'children'> {}

export function ThemeToggle(props: ThemeToggleProps) {
  const { cycleTheme, theme } = useTheme();

  const label = theme === 'light' ? 'Switch to dark theme' : theme === 'dark' ? 'Switch to system theme' : 'Switch to light theme';

  return (
    <IconButton aria-label={label} title={label} onClick={cycleTheme} {...props}>
      {theme === 'light' ? (
        <Sun size={18} className='text-gray-600 dark:text-gray-300' strokeWidth={1.5} />
      ) : theme === 'dark' ? (
        <Moon size={18} className='text-gray-600 dark:text-gray-300' strokeWidth={1.5} />
      ) : (
        <Monitor size={18} className='text-gray-600 dark:text-gray-300' strokeWidth={1.5} />
      )}
    </IconButton>
  );
}
