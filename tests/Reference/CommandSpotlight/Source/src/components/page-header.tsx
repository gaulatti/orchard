import * as React from 'react';
import { ChevronLeft } from 'lucide-react';

import { cn } from '../utils/cn';
import { Button } from './button';

export interface PageHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: React.ReactNode;
  actions?: React.ReactNode;
  backHref?: string;
  onBack?: () => void;
  className?: string;
}

export function PageHeader({ title, description, breadcrumbs, actions, backHref, onBack, className }: PageHeaderProps) {
  const showBack = Boolean(backHref || onBack);

  return (
    <header className={cn('space-y-5', className)}>
      {breadcrumbs && <div className='text-sm text-text-secondary dark:text-text-secondary'>{breadcrumbs}</div>}
      <div className='flex flex-col gap-7 sm:flex-row sm:items-start sm:justify-between'>
        <div className='flex items-start gap-4'>
          {showBack && (
            <Button
              variant='ghost'
              size='sm'
              className='mt-0.5 h-8 w-8 p-0'
              onClick={onBack}
              as={backHref ? 'a' : 'button'}
              href={backHref}
            >
              <ChevronLeft size={18} />
            </Button>
          )}
          <div>
            <h1 className='text-balance text-[2rem] font-semibold leading-[1.15] tracking-refined text-text-primary dark:text-text-primary'>{title}</h1>
            {description && <p className='font-secondary mt-2.5 max-w-2xl text-pretty text-sm leading-6 text-text-secondary dark:text-text-secondary'>{description}</p>}
          </div>
        </div>
        {actions && <div className='flex flex-wrap items-center gap-3'>{actions}</div>}
      </div>
    </header>
  );
}
