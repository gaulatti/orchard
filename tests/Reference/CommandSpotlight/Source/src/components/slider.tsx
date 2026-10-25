import React from 'react';

import { cn } from '../utils/cn';

export interface SliderProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
  label?: string;
  max?: number;
  min?: number;
  onChange?: (value: number) => void;
  showValue?: boolean;
  step?: number;
  value?: number;
}

export const Slider = React.forwardRef<HTMLInputElement, SliderProps>(function Slider(
  { className, disabled, label, max = 100, min = 0, onChange, showValue = false, step = 1, value = 0, ...props },
  ref
) {
  const pct = ((value - min) / (max - min)) * 100;

  return (
    <div className={cn('space-y-2', className)}>
      {(label || showValue) && (
        <div className='flex items-center justify-between'>
          {label ? <span className='text-sm font-medium text-text-primary dark:text-text-primary'>{label}</span> : <span />}
          {showValue && <span className='text-sm tabular-nums text-text-secondary dark:text-text-primary'>{value}</span>}
        </div>
      )}
      <div className='relative flex h-5 items-center'>
        {/* Track background */}
        <div className='pointer-events-none absolute inset-y-0 flex w-full items-center'>
          <div className='h-2 w-full overflow-hidden rounded-full bg-sand/20 dark:bg-sand/30'>
            <div className='h-full rounded-full bg-sea transition-[width] duration-[var(--motion-control)] ease-premium dark:bg-accent-blue' style={{ width: `${pct}%` }} />
          </div>
        </div>
        <input
          ref={ref}
          type='range'
          min={min}
          max={max}
          step={step}
          value={value}
          disabled={disabled}
          onChange={(e) => onChange?.(Number(e.target.value))}
          className={cn(
            'relative h-2 w-full cursor-pointer appearance-none rounded-full bg-transparent',
            // Thumb styling via arbitrary CSS
            '[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-sea [&::-webkit-slider-thumb]:bg-white [&::-webkit-slider-thumb]:shadow-sm [&::-webkit-slider-thumb]:transition-shadow [&::-webkit-slider-thumb]:duration-[var(--motion-control)] [&::-webkit-slider-thumb]:ease-premium [&::-webkit-slider-thumb]:hover:shadow-md dark:[&::-webkit-slider-thumb]:border-accent-blue',
            '[&::-moz-range-thumb]:h-5 [&::-moz-range-thumb]:w-5 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-sea [&::-moz-range-thumb]:bg-white [&::-moz-range-thumb]:shadow-sm dark:[&::-moz-range-thumb]:border-accent-blue',
            'focus:outline-none focus-visible:ring-2 focus-visible:ring-sea focus-visible:ring-offset-2 dark:focus-visible:ring-accent-blue',
            disabled && 'cursor-not-allowed opacity-50'
          )}
          {...props}
        />
      </div>
    </div>
  );
});
