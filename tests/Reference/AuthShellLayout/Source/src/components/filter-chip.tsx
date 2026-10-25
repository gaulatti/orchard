import * as React from 'react';
import { X } from 'lucide-react';

import { cn } from '../utils/cn';

export interface FilterChipProps {
  label: string;
  value?: string;
  onRemove?: () => void;
  className?: string;
}

export function FilterChip({ label, value, onRemove, className }: FilterChipProps) {
  return (
    <span
      className={cn(
        'inline-flex min-h-7 items-center gap-1.5 rounded-[6px] border border-sand/35 bg-white px-2.5 py-1 text-[12px] font-medium text-text-primary dark:border-white/12 dark:bg-deep-sea dark:text-text-primary',
        className
      )}
    >
      <span className='text-text-secondary dark:text-text-secondary'>{label}</span>
      {value && <span className='font-semibold'>{value}</span>}
      {onRemove && (
        <button
          type='button'
          onClick={onRemove}
          className='scale-100 rounded-[4px] p-0.5 text-text-secondary transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium hover:bg-sand/20 hover:text-terracotta active:scale-[0.9] dark:hover:bg-white/[0.07]'
          aria-label={`Remove ${label} filter`}
        >
          <X size={12} />
        </button>
      )}
    </span>
  );
}

export interface FilterGroupProps {
  filters: FilterChipProps[];
  className?: string;
  onClearAll?: () => void;
}

export function FilterGroup({ filters, className, onClearAll }: FilterGroupProps) {
  if (filters.length === 0) return null;

  return (
    <div className={cn('flex flex-wrap items-center gap-2', className)}>
      {filters.map((filter, index) => (
        <FilterChip key={`${filter.label}-${index}`} {...filter} />
      ))}
      {onClearAll && (
        <button
          type='button'
          onClick={onClearAll}
          className='text-xs font-medium text-sea transition-colors duration-[var(--motion-control)] ease-premium hover:text-deep-sea hover:underline dark:text-accent-blue dark:hover:text-text-primary'
        >
          Clear all
        </button>
      )}
    </div>
  );
}
