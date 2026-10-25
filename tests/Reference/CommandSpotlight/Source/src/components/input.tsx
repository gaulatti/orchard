import React from 'react';

import type { ControlSize } from '../core';
import { cn } from '../utils/cn';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  endIcon?: React.ReactNode;
  error?: boolean;
  inputSize?: ControlSize;
  startIcon?: React.ReactNode;
}

const sizeClasses: Record<ControlSize, string> = {
  sm: 'h-9 px-3 text-[13px]',
  md: 'h-10 px-3.5 text-sm',
  lg: 'h-11 px-4 text-[15px]'
};

export const Input = React.forwardRef<HTMLInputElement, InputProps>(function Input(
  { 'aria-invalid': ariaInvalid, className, endIcon, error = false, inputSize = 'md', startIcon, type = 'text', ...props },
  ref
) {
  return (
    <div className='relative w-full'>
      {startIcon ? (
        <div className='pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-text-secondary dark:text-text-secondary'>{startIcon}</div>
      ) : null}
      <input
        ref={ref}
        type={type}
        aria-invalid={ariaInvalid ?? (error || undefined)}
        className={cn(
          'w-full rounded-[var(--radius-ui)] border border-sand/40 bg-white text-text-primary shadow-[0_1px_2px_rgba(26,55,77,0.025)] outline-none transition-[background-color,border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium placeholder:text-text-secondary/55 hover:border-sand/70 hover:shadow-[0_3px_10px_-8px_rgba(26,55,77,0.25)] focus:border-sea/70 focus:ring-2 focus:ring-sea/10 disabled:cursor-not-allowed disabled:bg-light-sand/65 disabled:opacity-60 dark:border-white/15 dark:bg-deep-sea dark:text-text-primary dark:placeholder:text-text-secondary dark:hover:border-white/25 dark:focus:border-accent-blue dark:focus:ring-accent-blue/12 [&::-webkit-search-cancel-button]:appearance-none',
          error ? 'border-terracotta focus:border-terracotta focus:ring-terracotta/15 dark:border-terracotta' : '',
          startIcon ? 'pl-10' : sizeClasses[inputSize],
          startIcon && inputSize === 'sm' && 'h-9 pr-3 text-[13px]',
          startIcon && inputSize === 'md' && 'h-10 pr-3.5 text-sm',
          startIcon && inputSize === 'lg' && 'h-11 pr-4 text-[15px]',
          endIcon ? 'pr-10' : undefined,
          className
        )}
        {...props}
      />
      {endIcon ? (
        <div className='pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3 text-text-secondary dark:text-text-secondary'>{endIcon}</div>
      ) : null}
    </div>
  );
});
