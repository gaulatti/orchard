import * as React from 'react';

import { cn } from '../utils/cn';

export type TimelineStatus = 'completed' | 'active' | 'pending' | 'error';

export interface TimelineItem {
  id: string;
  title: string;
  description?: string;
  timestamp?: string;
  status?: TimelineStatus;
  icon?: React.ReactNode;
}

export interface TimelineProps {
  items: TimelineItem[];
  className?: string;
}

const statusClasses: Record<TimelineStatus, string> = {
  completed: 'bg-sea text-white ring-sea dark:bg-accent-blue dark:ring-accent-blue',
  active: 'bg-desert text-white ring-desert dark:bg-accent-gold dark:ring-accent-gold',
  pending: 'bg-sand/20 text-text-secondary ring-sand/40 dark:bg-sand/20 dark:text-text-secondary',
  error: 'bg-terracotta text-white ring-terracotta'
};

export function Timeline({ items, className }: TimelineProps) {
  return (
    <div className={cn('relative space-y-0', className)}>
      {items.map((item, index) => {
        const status = item.status ?? (index === 0 ? 'active' : 'pending');
        const isLast = index === items.length - 1;

        return (
          <div key={item.id} className='relative flex gap-4 pb-7 last:pb-0'>
            {!isLast && (
              <div className='absolute left-[17px] top-8 h-full w-px bg-sand/20 dark:bg-sand/20' />
            )}
            <div
              className={cn(
                'relative z-10 flex h-9 w-9 shrink-0 items-center justify-center rounded-[9px] border border-white/50 shadow-[0_1px_2px_rgba(26,55,77,0.08)] dark:border-white/10',
                statusClasses[status]
              )}
            >
              {item.icon ?? <span className='text-xs font-semibold'>{index + 1}</span>}
            </div>
            <div className='flex-1 pt-1'>
              <div className='flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between'>
                <p className='text-sm font-medium text-text-primary dark:text-text-primary'>{item.title}</p>
                {item.timestamp && (
                  <time className='font-secondary text-[11px] text-text-secondary dark:text-text-secondary'>{item.timestamp}</time>
                )}
              </div>
              {item.description && (
                <p className='font-secondary mt-1 text-sm leading-6 text-text-secondary dark:text-text-secondary'>{item.description}</p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
