'use client';

import React from 'react';

import { Select } from './select';
import { cn } from '../utils/cn';

export interface HeaderSelectOption {
  disabled?: boolean;
  label: string;
  value: string;
}

export interface HeaderSelectProps {
  'aria-describedby'?: string;
  'aria-label'?: string;
  autoFocus?: boolean;
  className?: string;
  disabled?: boolean;
  icon?: React.ReactNode;
  id?: string;
  name?: string;
  onChange: (value: string) => void;
  options: HeaderSelectOption[];
  placeholder?: string;
  required?: boolean;
  value?: string;
  wrapperClassName?: string;
}

/** A compact header treatment backed by the library's accessible Select menu. */
export function HeaderSelect({
  'aria-describedby': ariaDescribedBy,
  'aria-label': ariaLabel,
  autoFocus,
  className,
  disabled,
  icon,
  id,
  name,
  onChange,
  options,
  placeholder = 'Select option',
  required,
  value = '',
  wrapperClassName
}: HeaderSelectProps) {
  const menuOptions = options.some((option) => option.value === '') ? options : [{ label: placeholder, value: '' }, ...options];

  return (
    <div className={cn('inline-block min-w-0', wrapperClassName)}>
      <Select
        aria-describedby={ariaDescribedBy}
        aria-label={ariaLabel ?? placeholder}
        autoFocus={autoFocus}
        className={cn('w-auto min-w-[9rem] max-w-[13rem] border-sand/35', className)}
        contentClassName='min-w-[11rem]'
        disabled={disabled}
        id={id}
        name={name}
        onChange={onChange}
        options={menuOptions}
        placeholder={placeholder}
        required={required}
        size='sm'
        startIcon={icon}
        value={value}
      />
    </div>
  );
}
