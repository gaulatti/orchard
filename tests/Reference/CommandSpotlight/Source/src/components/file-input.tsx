import React from 'react';

import { cn } from '../utils/cn';

type FileInputSize = 'sm' | 'md' | 'lg';

export interface FileInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  error?: boolean;
  inputSize?: FileInputSize;
}

const sizeClasses: Record<FileInputSize, string> = {
  sm: 'text-sm file:px-3 file:py-2',
  md: 'text-sm file:px-4 file:py-2.5',
  lg: 'text-base file:px-4 file:py-3'
};

export const FileInput = React.forwardRef<HTMLInputElement, FileInputProps>(function FileInput({ className, error = false, inputSize = 'md', ...props }, ref) {
  return (
    <input
      ref={ref}
      type='file'
      className={cn(
        'block w-full rounded-[var(--radius-ui)] border bg-white p-2 text-text-primary outline-none transition-[border-color,box-shadow,opacity] duration-[var(--motion-control)] ease-premium file:mr-4 file:rounded-md file:border-0 file:font-medium file:tracking-refined file:transition-colors file:duration-[var(--motion-control)] file:ease-premium',
        'file:bg-sea/10 file:text-sea hover:file:bg-sea/20 dark:bg-dark-sand dark:text-text-primary dark:file:bg-accent-blue/10 dark:file:text-accent-blue dark:hover:file:bg-accent-blue/20',
        error ? 'border-terracotta dark:border-terracotta' : 'border-sand/30 dark:border-sand/50',
        'focus:ring-2 focus:ring-sea disabled:cursor-not-allowed disabled:opacity-50 dark:focus:ring-accent-blue',
        sizeClasses[inputSize],
        className
      )}
      {...props}
    />
  );
});
