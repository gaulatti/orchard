import React from 'react';

import { cn } from '../utils/cn';

export interface SeparatorProps extends React.HTMLAttributes<HTMLDivElement> {
  decorative?: boolean;
  orientation?: 'horizontal' | 'vertical';
}

export const Separator = React.forwardRef<HTMLDivElement, SeparatorProps>(function Separator(
  { className, decorative = true, orientation = 'horizontal', ...props },
  ref
) {
  return (
    <div
      ref={ref}
      role={decorative ? 'presentation' : 'separator'}
      aria-orientation={orientation}
      className={cn('shrink-0 bg-black/5 dark:bg-white/10', orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px', className)}
      {...props}
    />
  );
});
