import * as React from 'react';

import { cn } from '../utils/cn';

export interface DashboardSectionProps {
  title?: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
  headerClassName?: string;
  action?: React.ReactNode;
}

export function DashboardSection({ title, description, children, className, headerClassName, action }: DashboardSectionProps) {
  return (
    <section className={cn('space-y-5', className)}>
      {(title || description || action) && (
        <div className={cn('flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between', headerClassName)}>
          <div>
            {title && <h2 className='text-lg font-medium tracking-refined text-text-primary dark:text-text-primary'>{title}</h2>}
            {description && <p className='font-secondary mt-1 text-sm text-text-secondary dark:text-text-secondary'>{description}</p>}
          </div>
          {action && <div className='flex items-center gap-2'>{action}</div>}
        </div>
      )}
      {children}
    </section>
  );
}
