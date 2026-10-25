import React from 'react';

import type { IconButtonSize, IconButtonVariant } from '../core';
import { cn } from '../utils/cn';

export type { IconButtonSize, IconButtonVariant } from '../core';

export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  size?: IconButtonSize;
  variant?: IconButtonVariant;
}

const sizes: Record<IconButtonSize, string> = { sm: 'h-8 w-8', md: 'h-10 w-10', lg: 'h-11 w-11' };

export const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(function IconButton({ className, size = 'md', type = 'button', variant = 'default', ...props }, ref) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        'inline-flex shrink-0 translate-y-0 scale-100 items-center justify-center rounded-[var(--radius-button)] border text-text-primary transition-[background-color,border-color,color,box-shadow,transform,opacity] duration-[var(--motion-control)] ease-premium active:translate-y-px active:scale-[0.97] active:shadow-none focus:outline-none focus-visible:ring-2 focus-visible:ring-sea/30 focus-visible:ring-offset-2 focus-visible:ring-offset-white disabled:pointer-events-none disabled:transform-none disabled:opacity-45 dark:text-text-primary dark:focus-visible:ring-accent-blue/40 dark:focus-visible:ring-offset-deep-sea',
        sizes[size],
        variant === 'default' && 'border-sand/35 bg-white shadow-[0_1px_2px_rgba(26,55,77,0.025)] hover:border-sand/65 hover:bg-light-sand/45 hover:shadow-[0_3px_9px_-6px_rgba(26,55,77,0.28)] dark:border-white/15 dark:bg-deep-sea dark:hover:bg-white/[0.06]',
        variant === 'subtle' && 'border-transparent bg-light-sand/75 hover:bg-sand/25 dark:bg-white/[0.07] dark:hover:bg-white/[0.11]',
        variant === 'ghost' && 'border-transparent bg-transparent hover:bg-sand/12 dark:hover:bg-white/[0.06]',
        className
      )}
      {...props}
    />
  );
});
