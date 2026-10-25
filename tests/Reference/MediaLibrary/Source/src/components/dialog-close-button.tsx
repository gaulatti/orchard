import { X } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';

export interface DialogCloseButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  iconSize?: number;
}

export const DialogCloseButton = React.forwardRef<HTMLButtonElement, DialogCloseButtonProps>(function DialogCloseButton(
  { className, iconSize = 18, type = 'button', ...props },
  ref
) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        'inline-flex h-9 w-9 translate-y-0 scale-100 items-center justify-center rounded-[var(--radius-button)] border border-sand/30 bg-white text-text-secondary transition-[background-color,border-color,color,box-shadow,transform] duration-[var(--motion-control)] ease-premium hover:border-sand/60 hover:bg-light-sand/50 hover:text-text-primary hover:shadow-[0_3px_9px_-6px_rgba(26,55,77,0.28)] active:translate-y-px active:scale-[0.97] active:shadow-none focus:outline-none focus-visible:ring-2 focus-visible:ring-sea focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:border-white/15 dark:bg-deep-sea dark:text-text-secondary dark:hover:bg-sand/10 dark:hover:text-text-primary dark:focus-visible:ring-accent-blue dark:focus-visible:ring-offset-deep-sea',
        className
      )}
      {...props}
    >
      <X size={iconSize} />
    </button>
  );
});
