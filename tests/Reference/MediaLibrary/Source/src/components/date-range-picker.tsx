'use client';

import * as React from 'react';
import { X } from 'lucide-react';

import { Button } from './button';
import { DatePicker } from './date-picker';
import { cn } from '../utils/cn';

export interface DateRange {
  from?: string;
  to?: string;
}

export interface DateRangePickerProps {
  className?: string;
  onChange: (range: DateRange) => void;
  placeholder?: string;
  presets?: { label: string; days: number }[];
  value: DateRange;
}

function parseDate(value?: string) {
  if (!value) return null;
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function serializeDate(value: Date | null) {
  if (!value) return undefined;
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function DateRangePicker({ className, onChange, placeholder = 'Date range', presets, value }: DateRangePickerProps) {
  const fromDate = parseDate(value.from);
  const toDate = parseDate(value.to);
  const hasValue = Boolean(fromDate || toDate);

  const applyPreset = (days: number) => {
    const to = new Date();
    const from = new Date();
    from.setDate(to.getDate() - days);
    onChange({ from: serializeDate(from), to: serializeDate(to) });
  };

  return (
    <div className={cn('flex flex-wrap items-end gap-3', className)} role='group' aria-label={placeholder}>
      <div className='relative grid min-w-[360px] flex-1 grid-cols-[1fr_auto_1fr] items-end gap-2 rounded-[var(--radius-card)] border border-sand/30 bg-white p-3 shadow-[var(--shadow-surface)] dark:border-white/10 dark:bg-deep-sea'>
        <div className='space-y-1.5'>
          <span className='block text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>From</span>
          <DatePicker
            aria-label='Start date'
            value={fromDate}
            maxDate={toDate ?? undefined}
            onChange={(date) => onChange({ ...value, from: serializeDate(date) })}
            placeholder='Start date'
            triggerClassName='min-w-0 w-full border-transparent bg-light-sand/45 shadow-none hover:border-sand/35'
          />
        </div>
        <span className='mb-5 h-px w-3 bg-sand/55 dark:bg-white/20' aria-hidden='true' />
        <div className='space-y-1.5'>
          <span className='block text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>To</span>
          <DatePicker
            aria-label='End date'
            value={toDate}
            minDate={fromDate ?? undefined}
            onChange={(date) => onChange({ ...value, to: serializeDate(date) })}
            placeholder='End date'
            triggerClassName='min-w-0 w-full border-transparent bg-light-sand/45 shadow-none hover:border-sand/35'
          />
        </div>
      </div>

      {hasValue ? (
        <Button variant='ghost' size='sm' onClick={() => onChange({})} aria-label='Clear date range'>
          <X size={14} aria-hidden='true' />
          Clear
        </Button>
      ) : null}

      {presets?.length ? (
        <div className='flex h-10 items-center gap-1 rounded-[var(--radius-ui)] border border-sand/25 bg-white p-1 dark:border-white/10 dark:bg-deep-sea'>
          {presets.map((preset) => (
            <Button key={preset.label} variant='ghost' size='xs' onClick={() => applyPreset(preset.days)}>
              {preset.label}
            </Button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
