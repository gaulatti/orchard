import * as React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';

import { cn } from '../utils/cn';
import { Button } from './button';

export interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
  icon?: React.ReactNode;
}

export function ErrorState({
  title = 'Something went wrong',
  description = 'We couldn\'t load this data. Please try again.',
  onRetry,
  retryLabel = 'Try again',
  className,
  icon
}: ErrorStateProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-5 rounded-[var(--radius-card)] border border-terracotta/25 bg-terracotta/[0.035] px-8 py-14 text-center dark:bg-terracotta/[0.07]',
        className
      )}
    >
      <div className='flex h-12 w-12 items-center justify-center rounded-[10px] border border-terracotta/15 bg-terracotta/[0.07] text-terracotta dark:bg-terracotta/15'>
        {icon ?? <AlertCircle size={28} />}
      </div>
      <div className='space-y-1'>
        <h3 className='text-base font-semibold text-text-primary dark:text-text-primary'>{title}</h3>
        {description && <p className='max-w-xs text-sm text-text-secondary dark:text-text-secondary'>{description}</p>}
      </div>
      {onRetry && (
        <Button variant='secondary' size='sm' onClick={onRetry}>
          <RefreshCw size={14} />
          {retryLabel}
        </Button>
      )}
    </div>
  );
}
