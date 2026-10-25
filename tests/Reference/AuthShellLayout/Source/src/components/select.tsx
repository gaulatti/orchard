'use client';

import * as SelectPrimitive from '@radix-ui/react-select';
import { Check, ChevronDown, ChevronUp } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';
import { useModalBranch } from '../utils/hooks';

export interface SelectOption {
  disabled?: boolean;
  label: string;
  value: string;
}

export interface SelectProps {
  'aria-describedby'?: string;
  'aria-label'?: string;
  autoFocus?: boolean;
  className?: string;
  contentClassName?: string;
  disabled?: boolean;
  error?: boolean;
  id?: string;
  name?: string;
  onChange: (value: string) => void;
  options: SelectOption[];
  placeholder?: string;
  required?: boolean;
  size?: 'sm' | 'md' | 'lg';
  startIcon?: React.ReactNode;
  value: string;
}

const EMPTY_OPTION_VALUE = '__bleecker_empty_option__';
const triggerSizeClasses = {
  sm: 'h-9 gap-2 px-3 text-[13px]',
  md: 'h-10 gap-3 px-3.5 text-sm',
  lg: 'h-11 gap-3 px-4 text-[15px]'
};

export function Select({
  'aria-label': ariaLabel,
  'aria-describedby': ariaDescribedBy,
  autoFocus,
  className,
  contentClassName,
  disabled = false,
  error = false,
  id,
  name,
  onChange,
  options,
  placeholder = 'Select…',
  required,
  size = 'md',
  startIcon,
  value
}: SelectProps) {
  const hasEmptyOption = options.some((option) => option.value === '');
  const internalValue = value === '' && hasEmptyOption ? EMPTY_OPTION_VALUE : value || undefined;

  const branchRef = useModalBranch<HTMLDivElement>();

  return (
    <SelectPrimitive.Root
      disabled={disabled}
      name={name}
      required={required}
      value={internalValue}
      onValueChange={(nextValue) => onChange(nextValue === EMPTY_OPTION_VALUE ? '' : nextValue)}
    >
      <SelectPrimitive.Trigger
        id={id}
        autoFocus={autoFocus}
        aria-label={ariaLabel ?? placeholder}
        aria-describedby={ariaDescribedBy}
        aria-invalid={error || undefined}
        className={cn(
          'flex w-full items-center rounded-[var(--radius-ui)] border border-sand/40 bg-white text-left text-text-primary shadow-[0_1px_2px_rgba(26,55,77,0.025)] outline-none transition-[background-color,border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium hover:border-sand/70 hover:shadow-[0_3px_10px_-8px_rgba(26,55,77,0.25)] focus:border-sea/70 focus:ring-2 focus:ring-sea/10 data-[state=open]:border-sea/70 data-[state=open]:ring-2 data-[state=open]:ring-sea/10 data-[disabled]:cursor-not-allowed data-[disabled]:bg-light-sand/65 data-[disabled]:opacity-60 dark:border-white/15 dark:bg-deep-sea dark:text-text-primary dark:hover:border-white/25 dark:focus:border-accent-blue dark:focus:ring-accent-blue/12 dark:data-[state=open]:border-accent-blue dark:data-[state=open]:ring-accent-blue/12',
          triggerSizeClasses[size],
          error && 'border-terracotta focus:border-terracotta focus:ring-terracotta/15 dark:border-terracotta',
          className
        )}
      >
        {startIcon ? <span className='flex shrink-0 items-center text-sea dark:text-accent-blue' aria-hidden='true'>{startIcon}</span> : null}
        <span className='min-w-0 flex-1 truncate'><SelectPrimitive.Value placeholder={placeholder} /></span>
        <SelectPrimitive.Icon asChild>
          <ChevronDown size={17} className='shrink-0 text-text-secondary' aria-hidden='true' />
        </SelectPrimitive.Icon>
      </SelectPrimitive.Trigger>

      <SelectPrimitive.Portal>
        <SelectPrimitive.Content
          ref={branchRef}
          position='popper'
          side='bottom'
          align='start'
          sideOffset={4}
          collisionPadding={8}
          sticky='always'
          updatePositionStrategy='always'
          className={cn('z-50 w-[var(--radix-select-trigger-width)] overflow-hidden rounded-[var(--radius-ui)] border border-sand/40 bg-white shadow-[var(--shadow-overlay)] outline-none data-[side=bottom]:origin-top data-[side=top]:origin-bottom data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=open]:fade-in-0 data-[state=closed]:fade-out-0 data-[state=open]:zoom-in-[0.99] data-[state=closed]:zoom-out-[0.99] data-[state=open]:duration-[var(--motion-surface)] data-[state=closed]:duration-[var(--motion-exit)] data-[state=open]:ease-premium data-[state=closed]:ease-in dark:border-white/15 dark:bg-deep-sea', contentClassName)}
        >
          <SelectPrimitive.ScrollUpButton className='flex h-7 items-center justify-center border-b border-sand/15 bg-white text-text-secondary dark:border-white/10 dark:bg-deep-sea'>
            <ChevronUp size={15} aria-hidden='true' />
          </SelectPrimitive.ScrollUpButton>
          <SelectPrimitive.Viewport className='max-h-[min(18rem,var(--radix-select-content-available-height))] p-1'>
            {options.map((option) => (
              <SelectPrimitive.Item
                key={option.value}
                value={option.value === '' ? EMPTY_OPTION_VALUE : option.value}
                disabled={option.disabled}
                className={cn(
                  'relative flex cursor-default select-none items-center rounded-[5px] pl-3 pr-9 text-text-primary outline-none transition-[background-color,color,opacity] duration-[var(--motion-control)] ease-premium data-[disabled]:pointer-events-none data-[highlighted]:bg-light-sand data-[disabled]:opacity-40 data-[state=checked]:font-medium data-[state=checked]:text-sea dark:text-text-primary dark:data-[highlighted]:bg-white/[0.07] dark:data-[state=checked]:text-accent-blue',
                  size === 'sm' ? 'min-h-8 py-1.5 text-[13px]' : size === 'lg' ? 'min-h-10 py-2 text-[15px]' : 'min-h-9 py-1.5 text-sm'
                )}
              >
                <SelectPrimitive.ItemText>{option.label}</SelectPrimitive.ItemText>
                <SelectPrimitive.ItemIndicator className='absolute right-3 inline-flex items-center'>
                  <Check size={15} aria-hidden='true' />
                </SelectPrimitive.ItemIndicator>
              </SelectPrimitive.Item>
            ))}
          </SelectPrimitive.Viewport>
          <SelectPrimitive.ScrollDownButton className='flex h-7 items-center justify-center border-t border-sand/15 bg-white text-text-secondary dark:border-white/10 dark:bg-deep-sea'>
            <ChevronDown size={15} aria-hidden='true' />
          </SelectPrimitive.ScrollDownButton>
        </SelectPrimitive.Content>
      </SelectPrimitive.Portal>
    </SelectPrimitive.Root>
  );
}
