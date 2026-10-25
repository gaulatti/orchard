import React from 'react';

import { cn } from '../utils/cn';

export interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  containerClassName?: string;
  description?: React.ReactNode;
  label?: React.ReactNode;
}

export const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { className, containerClassName, description, id, label, ...props },
  ref
) {
  const checkbox = (
    <div className='relative flex h-5 w-5 shrink-0 items-center justify-center'>
      <input
        ref={ref}
        id={id}
        type='checkbox'
        className={cn(
          'peer absolute inset-0 m-0 h-full w-full appearance-none rounded border border-sand/60 bg-white transition-[background-color,border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium hover:border-sea/50 checked:border-sea checked:bg-sea focus:outline-none focus-visible:ring-2 focus-visible:ring-sea/20 focus-visible:ring-offset-2 focus-visible:ring-offset-white disabled:cursor-not-allowed disabled:opacity-50 dark:border-white/20 dark:bg-deep-sea dark:checked:border-accent-blue dark:checked:bg-accent-blue dark:focus-visible:ring-offset-deep-sea',
          className
        )}
        {...props}
      />
      <svg
        className='pointer-events-none relative z-10 h-3.5 w-3.5 text-white opacity-0 transition-opacity duration-[var(--motion-control)] ease-premium peer-checked:opacity-100'
        viewBox='0 0 14 10'
        fill='none'
        xmlns='http://www.w3.org/2000/svg'
      >
        <path d='M1 5L4.5 8.5L13 1' stroke='currentColor' strokeWidth='2' strokeLinecap='round' strokeLinejoin='round' />
      </svg>
    </div>
  );

  if (!label && !description) {
    return checkbox;
  }

  return (
    <label htmlFor={id} className={cn('flex items-start gap-3', containerClassName)}>
      {checkbox}
      <span className='min-w-0'>
        {label ? <span className='block text-sm font-medium text-text-primary dark:text-text-primary'>{label}</span> : null}
        {description ? <span className='mt-0.5 block text-sm text-text-secondary dark:text-text-secondary'>{description}</span> : null}
      </span>
    </label>
  );
});
