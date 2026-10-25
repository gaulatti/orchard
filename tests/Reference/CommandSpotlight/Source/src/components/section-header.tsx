import React from 'react';

import { cn } from '../utils/cn';
import { Eyebrow } from './eyebrow';

export interface SectionHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  eyebrow?: string;
  title: string;
  description?: string;
}

export function SectionHeader({ className, description, eyebrow, title, ...props }: SectionHeaderProps) {
  return (
    <div className={cn('space-y-3.5', className)} {...props}>
      {eyebrow ? <Eyebrow>{eyebrow}</Eyebrow> : null}
      <div className='space-y-3'>
        <h2 className='text-balance text-[1.75rem] font-semibold leading-tight tracking-refined text-text-primary dark:text-text-primary'>{title}</h2>
        {description ? <p className='font-secondary max-w-2xl text-pretty text-base leading-7 text-text-secondary dark:text-text-secondary'>{description}</p> : null}
      </div>
    </div>
  );
}
