import React from 'react';

import { cn } from '../utils/cn';

export interface HoverCardProps {
  children: React.ReactNode;
  className?: string;
  content: React.ReactNode;
  side?: 'top' | 'bottom';
}

/**
 * A card that appears on hover over a trigger element.
 * Pure CSS approach — no portal needed.
 */
export function HoverCard({ children, className, content, side = 'bottom' }: HoverCardProps) {
  return (
    <span className='group relative inline-flex'>
      {children}
      <span
        className={cn(
          'pointer-events-none absolute z-50 w-72 rounded-[var(--radius-card)] border border-sand/30 bg-white p-4 opacity-0 shadow-[var(--shadow-overlay)] transition-[opacity,transform] duration-[var(--motion-surface)] ease-premium group-hover:pointer-events-auto group-hover:opacity-100 dark:border-white/12 dark:bg-deep-sea',
          side === 'bottom' ? 'left-1/2 top-full mt-2 -translate-x-1/2 translate-y-1 group-hover:translate-y-0' : 'bottom-full left-1/2 mb-2 -translate-x-1/2 -translate-y-1 group-hover:translate-y-0',
          className
        )}
        role='tooltip'
      >
        {content}
      </span>
    </span>
  );
}
