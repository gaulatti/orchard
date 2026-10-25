import * as TooltipPrimitive from '@radix-ui/react-tooltip';
import React from 'react';

import { cn } from '../utils/cn';
import { useModalBranch } from '../utils/hooks';

export type TooltipSide = 'top' | 'bottom' | 'left' | 'right';

export interface TooltipProps {
  children: React.ReactNode;
  className?: string;
  content: React.ReactNode;
  delayDuration?: number;
  side?: TooltipSide;
}

export function Tooltip({ children, className, content, delayDuration = 400, side = 'top' }: TooltipProps) {
  const branchRef = useModalBranch<HTMLDivElement>();
  return (
    <TooltipPrimitive.Provider delayDuration={delayDuration} skipDelayDuration={150}>
      <TooltipPrimitive.Root>
        <TooltipPrimitive.Trigger asChild>{children}</TooltipPrimitive.Trigger>
        <TooltipPrimitive.Portal>
          <TooltipPrimitive.Content
            ref={branchRef}
            side={side}
            sideOffset={7}
            collisionPadding={10}
            className={cn(
              'z-[180] max-w-56 rounded-md bg-deep-sea px-3 py-2 text-xs leading-relaxed text-white shadow-[var(--shadow-overlay)] data-[state=delayed-open]:animate-in data-[state=closed]:animate-out data-[state=delayed-open]:fade-in-0 data-[state=closed]:fade-out-0 data-[state=delayed-open]:zoom-in-[0.98] data-[state=closed]:zoom-out-[0.98] data-[state=delayed-open]:duration-[var(--motion-surface)] data-[state=closed]:duration-[var(--motion-exit)] data-[state=delayed-open]:ease-premium data-[state=closed]:ease-in dark:bg-sand dark:text-text-primary',
              className
            )}
          >
            {content}
            <TooltipPrimitive.Arrow className='fill-deep-sea dark:fill-sand' />
          </TooltipPrimitive.Content>
        </TooltipPrimitive.Portal>
      </TooltipPrimitive.Root>
    </TooltipPrimitive.Provider>
  );
}
