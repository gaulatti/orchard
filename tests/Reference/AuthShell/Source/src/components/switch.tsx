import React from 'react';

import { cn } from '../utils/cn';

export interface SwitchProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
  checked?: boolean;
  label?: string;
  onCheckedChange?: (checked: boolean) => void;
}

export const Switch = React.forwardRef<HTMLInputElement, SwitchProps>(function Switch(
  { checked = false, className, disabled, id, label, onCheckedChange, ...props },
  ref
) {
  const inputId = id ?? React.useId();

  return (
    <label
      htmlFor={inputId}
      className={cn('inline-flex cursor-pointer select-none items-center gap-3', disabled && 'cursor-not-allowed opacity-50', className)}
    >
      <span className='relative inline-flex'>
        <input
          ref={ref}
          id={inputId}
          type='checkbox'
          role='switch'
          checked={checked}
          disabled={disabled}
          onChange={(e) => onCheckedChange?.(e.target.checked)}
          className='sr-only'
          aria-checked={checked}
          {...props}
        />
        {/* Track */}
        <span
          className={cn(
            'block h-5 w-9 rounded-full border border-transparent transition-colors duration-[var(--motion-control)] ease-premium',
            checked ? 'bg-sea dark:bg-accent-blue' : 'border-sand/35 bg-sand/25 dark:border-white/10 dark:bg-white/15'
          )}
        />
        {/* Thumb */}
        <span
          className={cn(
            'absolute left-0.5 top-0.5 block h-4 w-4 rounded-full bg-white shadow-[0_1px_2px_rgba(26,55,77,0.18)] transition-transform duration-[var(--motion-surface)] ease-premium',
            checked ? 'translate-x-4' : 'translate-x-0'
          )}
        />
      </span>
      {label ? <span className='text-sm font-medium text-text-primary dark:text-text-primary'>{label}</span> : null}
    </label>
  );
});
