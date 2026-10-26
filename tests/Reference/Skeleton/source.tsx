import * as React from 'react';

import { cn } from '../utils/cn';

export interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  circle?: boolean;
  width?: string | number;
  height?: string | number;
}

export function Skeleton({ className, circle, width, height, style, ...props }: SkeletonProps) {
  return (
    <div
      className={cn(
        'animate-pulse bg-sand/20 dark:bg-white/10',
        circle ? 'rounded-full' : 'rounded-[var(--radius-ui)]',
        className
      )}
      style={{ ...style, width, height }}
      {...props}
    />
  );
}

export interface SkeletonCardProps {
  rows?: number;
  className?: string;
}

export function SkeletonCard({ rows = 3, className }: SkeletonCardProps) {
  return (
    <div className={cn('space-y-3 rounded-[var(--radius-card)] bg-card p-6 shadow-sm ring-1 ring-border', className)}>
      <div className='flex items-center gap-3'>
        <Skeleton circle width={40} height={40} />
        <div className='flex-1 space-y-2'>
          <Skeleton height={16} className='w-1/3' />
          <Skeleton height={12} className='w-1/2' />
        </div>
      </div>
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} height={12} className='w-full' />
      ))}
    </div>
  );
}

export interface SkeletonTableProps {
  rows?: number;
  columns?: number;
  className?: string;
}

export function SkeletonTable({ rows = 5, columns = 4, className }: SkeletonTableProps) {
  return (
    <div className={cn('w-full space-y-2', className)}>
      <div className='flex gap-2'>
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} height={32} className='flex-1' />
        ))}
      </div>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className='flex gap-2'>
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} height={40} className='flex-1' />
          ))}
        </div>
      ))}
    </div>
  );
}
