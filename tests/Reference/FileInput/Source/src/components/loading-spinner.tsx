import type { LoadingSize } from '../core';
import { cn } from '../utils/cn';

export type { LoadingSize } from '../core';

export interface LoadingSpinnerProps {
  className?: string;
  size?: LoadingSize;
}

const sizeClasses: Record<LoadingSize, string> = {
  sm: 'h-4 w-4 border-2',
  md: 'h-8 w-8 border-2',
  lg: 'h-12 w-12 border-[3px]'
} as const;

export function LoadingSpinner({ className, size = 'md' }: LoadingSpinnerProps) {
  return (
    <div className={cn('flex items-center justify-center', className)}>
      <div className={cn(sizeClasses[size], 'animate-spin rounded-full border-sea border-t-transparent dark:border-accent-blue dark:border-t-transparent')} />
    </div>
  );
}
