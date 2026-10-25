import * as React from 'react';

import { cn } from '../utils/cn';

export interface ProgressStackItem {
  label: string;
  value: number;
  color?: string;
  className?: string;
}

export interface ProgressStackProps {
  items: ProgressStackItem[];
  max?: number;
  className?: string;
  barClassName?: string;
  showLegend?: boolean;
}

export function ProgressStack({ items, max, className, barClassName, showLegend = true }: ProgressStackProps) {
  const total = max ?? items.reduce((sum, item) => sum + item.value, 0);

  return (
    <div className={cn('space-y-3', className)}>
      <div className={cn('flex h-3 w-full overflow-hidden rounded-full bg-sand/20 dark:bg-sand/20', barClassName)}>
        {items.map((item, index) => {
          const width = total > 0 ? (item.value / total) * 100 : 0;
          return (
            <div
              key={index}
              className={cn('h-full first:rounded-l-full last:rounded-r-full', item.className)}
              style={{ width: `${width}%`, backgroundColor: item.color }}
              title={`${item.label}: ${item.value}`}
            />
          );
        })}
      </div>

      {showLegend && (
        <div className='flex flex-wrap items-center gap-3 text-xs'>
          {items.map((item, index) => (
            <div key={index} className='flex items-center gap-1.5'>
              <span className='h-2.5 w-2.5 rounded-full' style={{ backgroundColor: item.color }} />
              <span className='text-text-secondary dark:text-text-secondary'>{item.label}</span>
              <span className='font-semibold text-text-primary dark:text-text-primary'>{item.value}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
