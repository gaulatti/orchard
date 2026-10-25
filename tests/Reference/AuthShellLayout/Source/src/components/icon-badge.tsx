import React from 'react';

import type { IconBadgeSize, IconBadgeVariant } from '../core';
import { cn } from '../utils/cn';

export type { IconBadgeSize, IconBadgeVariant } from '../core';

export interface IconBadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  size?: IconBadgeSize;
  variant?: IconBadgeVariant;
}

const sizeClassNames: Record<IconBadgeSize, string> = {
  md: 'h-14 w-14',
  lg: 'h-16 w-16'
};

export function IconBadge({ children, className, size = 'lg', variant = 'primary', ...props }: IconBadgeProps) {
  return (
    <div
      className={cn(
        'flex items-center justify-center rounded-[10px] border',
        sizeClassNames[size],
        variant === 'primary' && 'border-deep-sea bg-deep-sea text-white dark:border-accent-blue dark:bg-accent-blue dark:text-deep-sea',
        variant === 'subtle' && 'border-sea/10 bg-sea/[0.06] text-sea dark:border-accent-blue/15 dark:bg-accent-blue/10 dark:text-accent-blue',
        variant === 'outlined' && 'border-sand/40 bg-white text-text-primary dark:border-white/15 dark:bg-deep-sea dark:text-text-primary',
        className
      )}
      {...props}
    >
      {children}
    </div>
  );
}
