import * as React from 'react';
import { Check } from 'lucide-react';

import { cn } from '../utils/cn';

export type StepStatus = 'pending' | 'active' | 'completed' | 'error';

export interface Step {
  id: string;
  label: string;
  description?: string;
  status?: StepStatus;
  disabled?: boolean;
}

export interface StepperProps {
  steps: Step[];
  activeStep?: string;
  className?: string;
  onStepClick?: (step: Step) => void;
  orientation?: 'horizontal' | 'vertical';
}

const statusClasses: Record<StepStatus, { circle: string; line: string }> = {
  completed: {
    circle: 'bg-sea text-white ring-sea dark:bg-accent-blue dark:ring-accent-blue',
    line: 'bg-sea dark:bg-accent-blue'
  },
  active: {
    circle: 'bg-white text-sea ring-2 ring-sea dark:bg-deep-sea dark:text-accent-blue dark:ring-accent-blue',
    line: 'bg-sand/30 dark:bg-sand/30'
  },
  pending: {
    circle: 'bg-white text-text-secondary ring-2 ring-sand/30 dark:bg-deep-sea dark:text-text-secondary dark:ring-sand/30',
    line: 'bg-sand/30 dark:bg-sand/30'
  },
  error: {
    circle: 'bg-terracotta text-white ring-terracotta',
    line: 'bg-sand/30 dark:bg-sand/30'
  }
};

export function Stepper({ steps, activeStep, className, onStepClick, orientation = 'horizontal' }: StepperProps) {
  const activeIndex = activeStep ? steps.findIndex((s) => s.id === activeStep) : -1;

  const isHorizontal = orientation === 'horizontal';

  return (
    <div className={cn(isHorizontal ? 'flex w-full items-start' : 'flex flex-col', className)}>
      {steps.map((step, index) => {
        const status: StepStatus = step.status ?? (index < activeIndex ? 'completed' : index === activeIndex ? 'active' : 'pending');
        const isLast = index === steps.length - 1;
        const styles = statusClasses[status];

        return (
          <div key={step.id} className={cn('relative flex', isHorizontal ? 'flex-1 flex-col items-center' : 'items-start gap-4') }>
            {/* Connector line */}
            {!isLast && (
              <div
                className={cn(
                  'absolute transition-colors duration-[var(--motion-surface)] ease-premium',
                  isHorizontal
                    ? 'left-1/2 top-4 h-0.5 w-[calc(100%-2rem)]'
                    : 'left-4 top-9 h-[calc(100%-2rem)] w-0.5'
                )}
              >
                <div className={cn('h-full w-full', status === 'completed' ? styles.line : 'bg-sand/20 dark:bg-sand/20')} />
              </div>
            )}

            {/* Step circle */}
            <button
              type='button'
              disabled={step.disabled || !onStepClick}
              onClick={() => onStepClick?.(step)}
              className={cn(
                'relative z-10 flex h-8 w-8 scale-100 items-center justify-center rounded-full text-sm font-semibold transition-[background-color,border-color,color,transform] duration-[var(--motion-control)] ease-premium active:scale-[0.94] focus:outline-none focus-visible:ring-2 focus-visible:ring-sea focus-visible:ring-offset-2',
                styles.circle,
                onStepClick && !step.disabled && 'cursor-pointer',
                step.disabled && 'cursor-not-allowed opacity-50'
              )}
            >
              {status === 'completed' ? <Check size={16} /> : index + 1}
            </button>

            {/* Labels */}
            <div className={cn('mt-2 text-center', isHorizontal ? 'max-w-[8rem]' : 'text-left')}>
              <p
                className={cn(
                  'text-sm font-medium',
                  status === 'active' ? 'text-text-primary dark:text-text-primary' : 'text-text-secondary dark:text-text-secondary'
                )}
              >
                {step.label}
              </p>
              {step.description && <p className='mt-0.5 text-xs text-text-secondary dark:text-text-secondary'>{step.description}</p>}
            </div>
          </div>
        );
      })}
    </div>
  );
}
