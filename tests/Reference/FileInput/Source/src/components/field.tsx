import React from 'react';

import { cn } from '../utils/cn';

type FieldControlProps = {
  'aria-describedby'?: string;
  'aria-invalid'?: boolean | 'false' | 'true';
  id?: string;
};

export interface FieldProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'children'> {
  action?: React.ReactNode;
  children: React.ReactElement<FieldControlProps>;
  description?: React.ReactNode;
  error?: React.ReactNode;
  label: React.ReactNode;
  optional?: boolean;
  required?: boolean;
}

/** A consistent, accessible label/control/help/error composition. */
export function Field({ action, children, className, description, error, label, optional, required, ...props }: FieldProps) {
  const generatedId = React.useId();
  const controlId = children.props.id ?? `${generatedId}-control`;
  const descriptionId = description ? `${generatedId}-description` : undefined;
  const errorId = error ? `${generatedId}-error` : undefined;
  const describedBy = [children.props['aria-describedby'], descriptionId, errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={cn('space-y-2.5', className)} {...props}>
      <div className='flex items-baseline justify-between gap-4'>
        <label htmlFor={controlId} className='text-[13px] font-medium tracking-ui text-text-primary dark:text-text-primary'>
          {label}
          {required ? <span className='ml-1 text-terracotta' aria-hidden='true'>*</span> : null}
        </label>
        {action ?? (optional ? <span className='font-secondary text-[11px] text-text-secondary'>Optional</span> : null)}
      </div>
      {React.cloneElement(children, {
        id: controlId,
        'aria-describedby': describedBy,
        'aria-invalid': error ? true : children.props['aria-invalid']
      })}
      {description ? <p id={descriptionId} className='font-secondary text-xs leading-5 text-text-secondary'>{description}</p> : null}
      {error ? <p id={errorId} className='text-xs leading-5 text-terracotta'>{error}</p> : null}
    </div>
  );
}
