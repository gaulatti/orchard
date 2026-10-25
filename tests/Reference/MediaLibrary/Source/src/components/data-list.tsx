import * as React from 'react';

import { cn } from '../utils/cn';

export interface DataListItem {
  id: string;
  label: React.ReactNode;
  value: React.ReactNode;
  description?: React.ReactNode;
}

export interface DataListProps {
  items: DataListItem[];
  className?: string;
  striped?: boolean;
}

export function DataList({ items, className, striped = false }: DataListProps) {
  return (
    <dl className={cn('divide-y divide-border', className)}>
      {items.map((item, index) => (
        <div
          key={item.id}
          className={cn(
            'flex flex-col gap-1.5 py-3.5 sm:grid sm:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] sm:items-start sm:gap-6',
            striped && index % 2 === 0 && 'rounded-[6px] bg-light-sand/40 px-3 dark:bg-white/[0.03]'
          )}
        >
          <dt className='text-[13px] font-medium text-text-secondary dark:text-text-secondary'>{item.label}</dt>
          <dd className='text-sm font-medium text-text-primary dark:text-text-primary sm:text-right'>
            {item.value}
            {item.description && (
              <p className='font-secondary mt-1 text-xs font-normal leading-5 text-text-secondary dark:text-text-secondary'>{item.description}</p>
            )}
          </dd>
        </div>
      ))}
    </dl>
  );
}
