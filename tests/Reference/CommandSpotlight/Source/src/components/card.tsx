import React from 'react';

import type { CardPadding, CardVariant } from '../core';
import { cn } from '../utils/cn';

export type { CardPadding, CardVariant } from '../core';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  padding?: CardPadding;
  variant?: CardVariant;
}

const paddingClasses: Record<CardPadding, string> = { none: 'p-0', sm: 'p-4', md: 'p-6', lg: 'p-8' };

export function Card({ className, padding = 'md', variant = 'surface', ...props }: CardProps) {
  return (
    <div
      className={cn(
        'relative overflow-hidden rounded-[var(--radius-card)] transition-[background-color,border-color,box-shadow,transform] duration-[var(--motion-surface)] ease-premium',
        paddingClasses[padding],
        variant === 'surface' && 'border border-sand/30 bg-white shadow-none dark:border-white/[0.09] dark:bg-deep-sea',
        variant === 'outlined' && 'border border-sand/40 bg-white shadow-none dark:border-white/15 dark:bg-deep-sea',
        variant === 'elevated' && 'border border-sand/25 bg-white shadow-[var(--shadow-raised)] dark:border-white/10 dark:bg-deep-sea',
        variant === 'subtle' && 'border border-sand/20 bg-light-sand/45 shadow-none dark:border-white/[0.07] dark:bg-white/[0.035]',
        variant === 'transparent' && 'border border-transparent bg-transparent shadow-none',
        className
      )}
      {...props}
    />
  );
}
