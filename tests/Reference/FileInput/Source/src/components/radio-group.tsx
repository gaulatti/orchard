import React from 'react';

import type { SelectionOrientation } from '../core';
import { cn } from '../utils/cn';

export type { SelectionOrientation } from '../core';

export interface RadioOption {
  description?: string;
  disabled?: boolean;
  label: string;
  value: string;
}

export interface RadioGroupProps {
  className?: string;
  disabled?: boolean;
  name?: string;
  onChange?: (value: string) => void;
  options: RadioOption[];
  orientation?: SelectionOrientation;
  value?: string;
}

export function RadioGroup({ className, disabled, name, onChange, options, orientation = 'vertical', value }: RadioGroupProps) {
  const groupName = name ?? React.useId();

  return (
    <div role='radiogroup' className={cn('flex', orientation === 'horizontal' ? 'flex-row flex-wrap gap-4' : 'flex-col gap-3', className)}>
      {options.map((option) => {
        const isChecked = option.value === value;
        const isDisabled = disabled || option.disabled;
        const id = `${groupName}-${option.value}`;

        return (
          <label
            key={option.value}
            htmlFor={id}
            className={cn('group flex cursor-pointer select-none items-start gap-3', isDisabled && 'cursor-not-allowed opacity-50')}
          >
            <span className='relative mt-0.5 flex-shrink-0'>
              <input
                id={id}
                type='radio'
                name={groupName}
                value={option.value}
                checked={isChecked}
                disabled={isDisabled}
                onChange={() => onChange?.(option.value)}
                className='sr-only'
              />
              {/* Outer ring */}
              <span
                className={cn(
                  'flex h-4.5 w-4.5 items-center justify-center rounded-full border-2 transition-colors duration-[var(--motion-control)] ease-premium',
                  isChecked
                    ? 'border-sea dark:border-accent-blue'
                    : 'border-sand/40 group-hover:border-sea/60 dark:border-sand/60 dark:group-hover:border-accent-blue/60'
                )}
              >
                {/* Inner dot */}
                <span
                  className={cn('h-2 w-2 rounded-full bg-sea transition-transform duration-[var(--motion-surface)] ease-premium dark:bg-accent-blue', isChecked ? 'scale-100' : 'scale-0')}
                />
              </span>
            </span>
            <span>
              <span className='block text-sm font-medium text-text-primary dark:text-text-primary'>{option.label}</span>
              {option.description ? (
                <span className='block text-xs leading-relaxed text-text-secondary dark:text-text-secondary'>{option.description}</span>
              ) : null}
            </span>
          </label>
        );
      })}
    </div>
  );
}
