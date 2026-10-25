import React from 'react';

import { cn } from '../utils/cn';

export interface KbdProps extends React.HTMLAttributes<HTMLElement> {
  /** One or more keys to display. Pass multiple elements for key combos. */
  keys?: string[];
}

/** Renders a keyboard key or key combination as styled <kbd> elements. */
export function Kbd({ className, keys, children, ...props }: KbdProps) {
  if (keys && keys.length > 0) {
    return (
      <span className='inline-flex items-center gap-0.5' aria-label={keys.join('+')} role='group'>
        {keys.map((key, i) => (
          <React.Fragment key={key}>
            {i > 0 && <span className='text-xs text-text-secondary'>+</span>}
            <kbd
              {...props}
              className={cn(
                'inline-flex h-5 min-w-5 items-center justify-center rounded-[4px] border border-black/[0.08] bg-light-sand/50 px-1.5 font-mono text-[11px] font-medium text-text-secondary shadow-[0_1px_0_0_rgba(0,0,0,0.08)] dark:border-white/10 dark:bg-deep-sea/50 dark:text-text-primary dark:shadow-[0_1px_0_0_rgba(255,255,255,0.1)]',
                className
              )}
            >
              {key}
            </kbd>
          </React.Fragment>
        ))}
      </span>
    );
  }

  return (
    <kbd
      {...props}
      className={cn(
        'inline-flex h-5 min-w-5 items-center justify-center rounded-[4px] border border-black/[0.08] bg-light-sand/50 px-1.5 font-mono text-[11px] font-medium text-text-secondary shadow-[0_1px_0_0_rgba(0,0,0,0.08)] dark:border-white/10 dark:bg-deep-sea/50 dark:text-text-primary dark:shadow-[0_1px_0_0_rgba(255,255,255,0.1)]',
        className
      )}
    >
      {children}
    </kbd>
  );
}
