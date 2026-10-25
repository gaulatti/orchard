import React from 'react';

import { cn } from '../utils/cn';

export interface FeedGridProps extends React.HTMLAttributes<HTMLDivElement> {
  minColumnWidth?: number | string;
}

export function FeedGrid({ children, className, minColumnWidth = 340, style, ...props }: FeedGridProps) {
  const resolvedWidth = typeof minColumnWidth === 'number' ? `${minColumnWidth}px` : minColumnWidth;

  return (
    <div
      {...props}
      className={cn('grid gap-3 p-4', className)}
      style={{ gridTemplateColumns: `repeat(auto-fill, minmax(min(100%, ${resolvedWidth}), 1fr))`, ...style }}
    >
      {children}
    </div>
  );
}
