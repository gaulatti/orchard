import * as React from 'react';

import { cn } from '../utils/cn';

export interface DashboardGridProps {
  children: React.ReactNode;
  className?: string;
  columns?: 1 | 2 | 3 | 4 | 5 | 6;
}

export function DashboardGrid({ children, className, columns }: DashboardGridProps) {
  return (
    <div
      className={cn(
        'grid gap-6',
        columns === 1 && 'grid-cols-1',
        columns === 2 && 'grid-cols-1 sm:grid-cols-2',
        columns === 3 && 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
        columns === 4 && 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
        columns === 5 && 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5',
        columns === 6 && 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6',
        !columns && 'dashboard-grid',
        className
      )}
    >
      {children}
    </div>
  );
}

export interface DashboardSpanProps {
  children: React.ReactNode;
  className?: string;
  span?: 1 | 2 | 3 | 4 | 'full';
}

export function DashboardSpan({ children, className, span = 1 }: DashboardSpanProps) {
  return (
    <div
      className={cn(
        'min-w-0',
        span === 1 && 'col-span-1',
        span === 2 && 'col-span-1 sm:col-span-2',
        span === 3 && 'col-span-1 sm:col-span-2 lg:col-span-3',
        span === 4 && 'col-span-1 sm:col-span-2 lg:col-span-3 xl:col-span-4',
        span === 'full' && 'col-span-full',
        className
      )}
    >
      {children}
    </div>
  );
}
