import React from 'react';

import type { ControlSize } from '../core';
import { cn } from '../utils/cn';

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
  textareaSize?: ControlSize;
}

const sizeClasses: Record<ControlSize, string> = {
  sm: 'min-h-24 px-3 py-2 text-sm',
  md: 'min-h-28 px-4 py-3 text-sm',
  lg: 'min-h-32 px-4 py-3 text-base'
};

export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(function Textarea(
  { 'aria-invalid': ariaInvalid, className, error = false, textareaSize = 'md', rows = 4, ...props },
  ref
) {
  return (
    <textarea
      ref={ref}
      rows={rows}
      aria-invalid={ariaInvalid ?? (error || undefined)}
      className={cn(
        'w-full rounded-[var(--radius-ui)] border border-sand/40 bg-white text-text-primary shadow-[0_1px_2px_rgba(26,55,77,0.025)] outline-none transition-[background-color,border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium placeholder:text-text-secondary/55 hover:border-sand/70 hover:shadow-[0_3px_10px_-8px_rgba(26,55,77,0.25)] focus:border-sea/70 focus:ring-2 focus:ring-sea/10 disabled:cursor-not-allowed disabled:bg-light-sand/65 disabled:opacity-60 dark:border-white/15 dark:bg-deep-sea dark:text-text-primary dark:placeholder:text-text-secondary dark:hover:border-white/25 dark:focus:border-accent-blue dark:focus:ring-accent-blue/12',
        'resize-y',
        error ? 'border-terracotta focus:border-terracotta focus:ring-terracotta/15 dark:border-terracotta' : '',
        sizeClasses[textareaSize],
        className
      )}
      {...props}
    />
  );
});
