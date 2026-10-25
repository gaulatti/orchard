'use client';

import { ArrowUpDown, Filter, X } from 'lucide-react';
import React from 'react';

import { Button } from './button';
import { DatePicker } from './date-picker';
import { IconButton } from './icon-button';
import { Select } from './select';
import type { SortState } from './table';
import { cn } from '../utils/cn';

export interface CollectionFilterOption {
  field: string;
  label: string;
  options?: Array<{ label: string; value: string }>;
  type: 'select' | 'boolean' | 'date';
}

export interface CollectionSortOption {
  field: string;
  label: string;
}

export interface CollectionFiltersProps {
  className?: string;
  currentFilters: Record<string, boolean | string>;
  currentSort: SortState;
  defaultExpanded?: boolean;
  filterOptions: CollectionFilterOption[];
  onFilterChange: (filters: Record<string, boolean | string>) => void;
  onSortChange: (sort: SortState) => void;
  sortOptions: CollectionSortOption[];
}

function parseDateValue(value?: string) {
  if (!value) return null;
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) return null;
  return new Date(year, month - 1, day);
}

function serializeDateValue(value: Date | null) {
  if (!value) return '';
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function CollectionFilters({
  className,
  currentFilters,
  currentSort,
  defaultExpanded = false,
  filterOptions,
  onFilterChange,
  onSortChange,
  sortOptions
}: CollectionFiltersProps) {
  const [showFilters, setShowFilters] = React.useState(defaultExpanded);
  const activeFilterCount = Object.keys(currentFilters).length;

  if (filterOptions.length === 0 && sortOptions.length === 0) return null;

  const getFilterDisplayValue = (field: string, value: boolean | string) => {
    const option = filterOptions.find((item) => item.field === field);
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (option?.type === 'date') {
      const date = parseDateValue(value);
      return date?.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }) ?? value;
    }
    return option?.options?.find((item) => item.value === value)?.label ?? value;
  };

  const handleFilterChange = (field: string, value: boolean | string | null | undefined) => {
    const nextFilters = { ...currentFilters };

    if (value === '' || value === null || value === undefined) {
      delete nextFilters[field];
    } else {
      nextFilters[field] = value;
    }

    onFilterChange(nextFilters);
  };

  return (
    <section className={cn('mb-6 overflow-hidden rounded-[var(--radius-card)] border border-sand/30 bg-white shadow-[var(--shadow-surface)] dark:border-white/10 dark:bg-deep-sea', className)}>
      <div className='flex min-h-14 flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-5'>
        <div className='flex flex-wrap items-center gap-2'>
        <Button
          variant={showFilters ? 'subtle' : 'outline'}
          size='sm'
          onClick={() => setShowFilters((open) => !open)}
          aria-expanded={showFilters}
        >
          <Filter size={15} />
          Filters
          {activeFilterCount > 0 ? (
            <span className='ml-0.5 flex h-5 min-w-5 items-center justify-center rounded-[5px] bg-sea px-1.5 text-[11px] font-semibold tabular-nums text-white dark:bg-accent-blue dark:text-deep-sea'>{activeFilterCount}</span>
          ) : null}
        </Button>

        {activeFilterCount > 0 ? (
          <Button variant='link' size='sm' onClick={() => onFilterChange({})} className='ml-1 text-[12px] text-text-secondary hover:text-terracotta'>
            Clear all
          </Button>
        ) : null}
        </div>

        {sortOptions.length > 0 ? <div className='flex items-center gap-2'>
          <span className='hidden text-[11px] font-medium uppercase tracking-[0.08em] text-text-secondary sm:inline'>Sort</span>
          <div className='w-44'>
          <Select
            aria-label='Sort collection by'
            value={currentSort.field}
            onChange={(value) => onSortChange({ field: value, order: currentSort.order })}
            options={sortOptions.map((option) => ({ label: option.label, value: option.field }))}
          />
          </div>
          <IconButton
            onClick={() => onSortChange({ field: currentSort.field, order: currentSort.order === 'asc' ? 'desc' : 'asc' })}
            size='md'
            variant='ghost'
            title={`Sort ${currentSort.order === 'asc' ? 'Descending' : 'Ascending'}`}
            aria-label={`Sort ${currentSort.order === 'asc' ? 'descending' : 'ascending'}`}
          >
            <ArrowUpDown size={16} className={cn('transition-transform duration-[var(--motion-surface)] ease-premium', currentSort.order === 'desc' && 'rotate-180')} />
          </IconButton>
        </div> : null}
      </div>

      {activeFilterCount > 0 ? (
        <div className='flex flex-wrap items-center gap-2 border-t border-sand/20 bg-light-sand/20 px-4 py-2.5 dark:border-white/[0.07] dark:bg-white/[0.025] sm:px-5'>
          {Object.entries(currentFilters).map(([field, value]) => {
            const label = filterOptions.find((option) => option.field === field)?.label ?? field;
            return (
              <button
                key={field}
                type='button'
                onClick={() => handleFilterChange(field, null)}
                className='group inline-flex h-7 scale-100 items-center gap-1.5 rounded-[6px] border border-sand/35 bg-white px-2.5 text-[12px] text-text-secondary transition-[background-color,border-color,color,transform] duration-[var(--motion-control)] ease-premium hover:border-terracotta/35 hover:text-text-primary active:scale-[0.98] dark:border-white/10 dark:bg-deep-sea'
                aria-label={`Remove ${label} filter`}
              >
                <span className='font-medium text-text-primary'>{label}</span>
                <span>{getFilterDisplayValue(field, value)}</span>
                <X size={12} className='ml-0.5 opacity-55 transition-[color,opacity] duration-[var(--motion-control)] ease-premium group-hover:text-terracotta group-hover:opacity-100' />
              </button>
            );
          })}
        </div>
      ) : null}

      {showFilters ? (
        <div className='border-t border-sand/25 bg-light-sand/25 px-4 py-5 dark:border-white/[0.08] dark:bg-white/[0.025] sm:px-5'>
          <div className='mb-4'>
            <p className='text-sm font-medium text-text-primary'>Refine results</p>
            <p className='font-secondary mt-0.5 text-xs text-text-secondary'>Selections apply immediately.</p>
          </div>
          <div className='grid grid-cols-1 gap-x-5 gap-y-5 md:grid-cols-2 lg:grid-cols-3'>
            {filterOptions.map((option) => (
              <div key={option.field} className='space-y-2.5'>
                <label className='block text-[12px] font-medium tracking-ui text-text-primary dark:text-text-primary'>{option.label}</label>

                {option.type === 'select' && option.options ? (
                  <Select
                    aria-label={option.label}
                    value={(currentFilters[option.field] as string) || ''}
                    onChange={(value) => handleFilterChange(option.field, value)}
                    options={[{ label: 'All', value: '' }, ...option.options]}
                  />
                ) : null}

                {option.type === 'boolean' ? (
                  <Select
                    aria-label={option.label}
                    value={currentFilters[option.field] === undefined ? '' : String(currentFilters[option.field])}
                    onChange={(value) => handleFilterChange(option.field, value === '' ? null : value === 'true')}
                    options={[
                      { label: 'All', value: '' },
                      { label: 'Yes', value: 'true' },
                      { label: 'No', value: 'false' }
                    ]}
                  />
                ) : null}

                {option.type === 'date' ? (
                  <DatePicker
                    aria-label={option.label}
                    className='w-full'
                    triggerClassName='w-full'
                    placeholder={`Choose ${option.label.toLowerCase()}`}
                    value={parseDateValue(currentFilters[option.field] as string)}
                    onChange={(date) => handleFilterChange(option.field, serializeDateValue(date))}
                  />
                ) : null}
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </section>
  );
}
