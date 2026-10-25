'use client';

import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';
import { IconButton } from './icon-button';
import { Popover } from './popover';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function getDaysInMonth(year: number, month: number) {
  return new Date(year, month + 1, 0).getDate();
}

function getFirstDayOfMonth(year: number, month: number) {
  return new Date(year, month, 1).getDay();
}

function isSameDay(a: Date, b: Date) {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function isToday(date: Date) {
  return isSameDay(date, new Date());
}

const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const DAYS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

function formatDate(date: Date) {
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

// ─── Calendar ────────────────────────────────────────────────────────────────

export interface CalendarProps {
  className?: string;
  disabled?: (date: Date) => boolean;
  maxDate?: Date;
  minDate?: Date;
  onChange?: (date: Date) => void;
  value?: Date | null;
}

export function Calendar({ className, disabled: isDisabledFn, maxDate, minDate, onChange, value }: CalendarProps) {
  const today = new Date();
  const [viewYear, setViewYear] = React.useState(value?.getFullYear() ?? today.getFullYear());
  const [viewMonth, setViewMonth] = React.useState(value?.getMonth() ?? today.getMonth());

  const days = getDaysInMonth(viewYear, viewMonth);
  const firstDay = getFirstDayOfMonth(viewYear, viewMonth);

  const prevMonth = () => {
    if (viewMonth === 0) {
      setViewMonth(11);
      setViewYear((y) => y - 1);
    } else setViewMonth((m) => m - 1);
  };
  const nextMonth = () => {
    if (viewMonth === 11) {
      setViewMonth(0);
      setViewYear((y) => y + 1);
    } else setViewMonth((m) => m + 1);
  };

  const cells = [...Array(firstDay).fill(null), ...Array.from({ length: days }, (_, i) => new Date(viewYear, viewMonth, i + 1))];

  const isOutOfRange = (date: Date) => {
    if (minDate && date < minDate) return true;
    if (maxDate && date > maxDate) return true;
    return false;
  };

  return (
    <div className={cn('w-full select-none', className)}>
      {/* Header */}
      <div className='mb-4 flex items-center justify-between'>
        <IconButton
          onClick={prevMonth}
          size='sm'
          variant='ghost'
          aria-label='Previous month'
        >
          <ChevronLeft size={16} />
        </IconButton>
        <span className='text-[13px] font-semibold tracking-ui text-text-primary dark:text-text-primary'>
          {MONTHS[viewMonth]} {viewYear}
        </span>
        <IconButton
          onClick={nextMonth}
          size='sm'
          variant='ghost'
          aria-label='Next month'
        >
          <ChevronRight size={16} />
        </IconButton>
      </div>

      {/* Day headers */}
      <div className='mb-1 grid grid-cols-7'>
        {DAYS.map((d) => (
          <div
            key={d}
            className='flex h-8 items-center justify-center text-[10px] font-semibold uppercase tracking-[0.08em] text-text-secondary dark:text-text-secondary'
          >
            {d}
          </div>
        ))}
      </div>

      {/* Cells */}
      <div className='grid grid-cols-7 gap-y-0.5'>
        {cells.map((date, index) => {
          if (!date) return <div key={`empty-${index}`} />;

          const selected = value && isSameDay(date, value);
          const todayCell = isToday(date);
          const outOfRange = isOutOfRange(date);
          const disabled = outOfRange || isDisabledFn?.(date);

          return (
            <button
              key={date.toISOString()}
              type='button'
              disabled={!!disabled}
              onClick={() => onChange?.(date)}
              aria-label={date.toDateString()}
              aria-pressed={!!selected}
              className={cn(
                'flex h-8 w-full scale-100 items-center justify-center rounded-[6px] text-[13px] tabular-nums transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium active:scale-[0.94] focus:outline-none focus-visible:ring-2 focus-visible:ring-sea/30 dark:focus-visible:ring-accent-blue/40',
                selected
                  ? 'bg-sea font-semibold text-white shadow-[0_1px_2px_rgba(21,48,66,0.12)] dark:bg-accent-blue dark:text-deep-sea'
                  : todayCell
                    ? 'font-semibold text-sea ring-1 ring-inset ring-sea/45 dark:text-accent-blue dark:ring-accent-blue/50'
                    : 'text-text-primary hover:bg-light-sand dark:text-text-primary dark:hover:bg-white/[0.07]',
                disabled && 'pointer-events-none opacity-30'
              )}
            >
              {date.getDate()}
            </button>
          );
        })}
      </div>
    </div>
  );
}

// ─── DatePicker ───────────────────────────────────────────────────────────────

export interface DatePickerProps {
  'aria-label'?: string;
  className?: string;
  disabled?: boolean;
  disabledDates?: (date: Date) => boolean;
  maxDate?: Date;
  minDate?: Date;
  onChange?: (date: Date | null) => void;
  placeholder?: string;
  triggerClassName?: string;
  value?: Date | null;
}

export function DatePicker({ 'aria-label': ariaLabel, className, disabled = false, disabledDates, maxDate, minDate, onChange, placeholder = 'Pick a date', triggerClassName, value }: DatePickerProps) {
  const [open, setOpen] = React.useState(false);

  return (
    <div className={cn('inline-block', className)}>
      <Popover
        align='start'
        open={open}
        onOpenChange={setOpen}
        className='w-72 p-4'
        content={
          <div role='dialog' aria-label={ariaLabel ?? placeholder}>
            <Calendar
              value={value}
              minDate={minDate}
              maxDate={maxDate}
              disabled={disabledDates}
              onChange={(date) => {
                onChange?.(date);
                setOpen(false);
              }}
            />
            {value && (
              <button
                type='button'
                onClick={() => {
                  onChange?.(null);
                  setOpen(false);
                }}
                className='mt-3 w-full border-t border-sand/20 pt-3 text-[12px] font-medium text-text-secondary transition-colors duration-[var(--motion-control)] ease-premium hover:text-text-primary dark:border-white/[0.08]'
              >
                Clear date
              </button>
            )}
          </div>
        }
      >
        <button
          type='button'
          disabled={disabled}
          aria-label={ariaLabel}
          className={cn(
            'flex h-10 min-w-[200px] items-center gap-2.5 rounded-[var(--radius-ui)] border border-sand/40 bg-white px-3.5 text-sm shadow-[0_1px_2px_rgba(26,55,77,0.025)] outline-none transition-[border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium hover:border-sand/70 hover:shadow-[0_3px_10px_-8px_rgba(26,55,77,0.25)] focus-visible:border-sea/70 focus-visible:ring-2 focus-visible:ring-sea/10 data-[state=open]:border-sea/70 data-[state=open]:ring-2 data-[state=open]:ring-sea/10 dark:border-white/15 dark:bg-deep-sea dark:data-[state=open]:border-accent-blue dark:data-[state=open]:ring-accent-blue/12',
            value ? 'text-text-primary dark:text-text-primary' : 'text-text-secondary dark:text-text-secondary',
            disabled && 'cursor-not-allowed opacity-50',
            triggerClassName
          )}
        >
          <CalendarDays size={15} className='shrink-0 text-sea dark:text-accent-blue' />
          <span className='min-w-0 flex-1 truncate text-left'>{value ? formatDate(value) : placeholder}</span>
        </button>
      </Popover>
    </div>
  );
}
