import React from 'react';

import { cn } from '../utils/cn';

export interface ScrollAreaProps extends React.HTMLAttributes<HTMLDivElement> {
  /** max-height applied to the viewport only. Use any valid CSS value. */
  maxHeight?: string;
  orientation?: 'vertical' | 'horizontal' | 'both';
}

/**
 * A thin wrapper that applies custom-styled scrollbars while keeping the
 * inner content naturally sized. Use `maxHeight` to constrain height.
 */
export function ScrollArea({ children, className, maxHeight, orientation = 'vertical', style, ...props }: ScrollAreaProps) {
  const overflowClass =
    orientation === 'both' ? 'overflow-auto' : orientation === 'horizontal' ? 'overflow-x-auto overflow-y-hidden' : 'overflow-y-auto overflow-x-hidden';

  return (
    <div
      {...props}
      className={cn(
        overflowClass,
        // Custom scrollbar styling via Tailwind arbitrary variants
        '[&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:rounded-full [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-sand/30 [&::-webkit-scrollbar-thumb:hover]:bg-sand/50 dark:[&::-webkit-scrollbar-thumb]:bg-sand/40 dark:[&::-webkit-scrollbar-thumb:hover]:bg-sand/60',
        className
      )}
      style={{ maxHeight, ...style }}
    >
      {children}
    </div>
  );
}
