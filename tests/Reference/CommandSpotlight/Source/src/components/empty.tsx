import React from 'react';

import { cn } from '../utils/cn';

export interface EmptyProps {
  action?: React.ReactNode;
  className?: string;
  description?: string;
  icon?: React.ReactNode;
  title: string;
}

const DefaultIcon = () => (
  <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64' fill='none' className='h-full w-full' aria-hidden='true'>
    <rect x='8' y='16' width='48' height='36' rx='6' stroke='currentColor' strokeWidth='2' />
    <path d='M8 28h48' stroke='currentColor' strokeWidth='2' />
    <path d='M24 40h16' stroke='currentColor' strokeWidth='2' strokeLinecap='round' />
  </svg>
);

export function Empty({ action, className, description, icon, title }: EmptyProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-5 rounded-[var(--radius-card)] border border-sand/30 bg-light-sand/25 px-8 py-16 text-center dark:border-white/10 dark:bg-white/[0.025]',
        className
      )}
    >
      <div className='flex h-14 w-14 items-center justify-center rounded-[10px] border border-sand/25 bg-white text-text-secondary shadow-[var(--shadow-surface)] dark:border-white/10 dark:bg-deep-sea dark:text-text-primary'>
        {icon ?? (
          <span className='h-8 w-8 text-sea dark:text-accent-blue'>
            <DefaultIcon />
          </span>
        )}
      </div>
      <div className='space-y-1.5'>
        <h3 className='text-base font-semibold text-text-primary dark:text-text-primary'>{title}</h3>
        {description ? <p className='max-w-xs text-sm leading-relaxed text-text-secondary dark:text-text-secondary'>{description}</p> : null}
      </div>
      {action ? <div className='mt-1'>{action}</div> : null}
    </div>
  );
}
