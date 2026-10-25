import React from 'react';

import type { ToggleSize, ToggleVariant } from '../core';
import { cn } from '../utils/cn';

export type { ToggleSize, ToggleVariant } from '../core';

export interface ToggleProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  pressed?: boolean;
  onPressedChange?: (pressed: boolean) => void;
  size?: ToggleSize;
  variant?: ToggleVariant;
}

const sizeClasses: Record<ToggleSize, string> = {
  sm: 'h-8 px-2.5 text-sm gap-1.5',
  md: 'h-9 px-3 text-sm gap-2',
  lg: 'h-10 px-4 text-base gap-2'
};

export const Toggle = React.forwardRef<HTMLButtonElement, ToggleProps>(function Toggle(
  { children, className, disabled, onPressedChange, pressed = false, size = 'md', variant = 'default', onClick, ...props },
  ref
) {
  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    onPressedChange?.(!pressed);
    onClick?.(event);
  };

  return (
    <button
      ref={ref}
      type='button'
      aria-pressed={pressed}
      disabled={disabled}
      onClick={handleClick}
      className={cn(
        'inline-flex translate-y-0 scale-100 items-center justify-center rounded-[var(--radius-button)] font-medium transition-[background-color,border-color,color,box-shadow,transform,opacity] duration-[var(--motion-control)] ease-premium active:translate-y-px active:scale-[0.99] focus:outline-none focus-visible:ring-2 focus-visible:ring-sea disabled:pointer-events-none disabled:transform-none disabled:opacity-50 dark:focus-visible:ring-accent-blue',
        sizeClasses[size],
        variant === 'outline'
          ? [
              'border',
              pressed
                ? 'border-sea bg-sea/10 text-sea dark:border-accent-blue dark:bg-accent-blue/15 dark:text-accent-blue'
                : 'border-sand/30 bg-transparent text-text-secondary hover:bg-sand/10 hover:text-text-primary dark:border-sand/40 dark:hover:bg-sand/15'
            ]
          : [
              pressed
                ? 'bg-sea/10 text-sea dark:bg-accent-blue/15 dark:text-accent-blue'
                : 'bg-transparent text-text-secondary hover:bg-sand/10 hover:text-text-primary dark:hover:bg-sand/15'
            ],
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
});
