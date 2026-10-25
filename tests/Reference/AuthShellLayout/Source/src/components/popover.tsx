import * as PopoverPrimitive from '@radix-ui/react-popover';
import React from 'react';

import { cn } from '../utils/cn';
import { useModalBranch } from '../utils/hooks';

export type PopoverSide = 'top' | 'bottom' | 'left' | 'right';
export type PopoverAlign = 'start' | 'center' | 'end';

export interface PopoverProps {
  align?: PopoverAlign;
  children: React.ReactNode;
  className?: string;
  content: React.ReactNode;
  defaultOpen?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  side?: PopoverSide;
  sideOffset?: number;
}

export function Popover({ align = 'center', children, className, content, defaultOpen, onOpenChange, open, side = 'bottom', sideOffset = 4 }: PopoverProps) {
  const branchRef = useModalBranch<HTMLDivElement>();

  return (
    <PopoverPrimitive.Root defaultOpen={defaultOpen} onOpenChange={onOpenChange} open={open}>
      <PopoverPrimitive.Trigger asChild>{children}</PopoverPrimitive.Trigger>
      <PopoverPrimitive.Portal>
        <PopoverPrimitive.Content
          ref={branchRef}
          align={align}
          side={side}
          sideOffset={sideOffset}
          collisionPadding={12}
          className={cn(
            'z-50 min-w-56 rounded-[var(--radius-ui)] border border-sand/35 bg-white p-2 text-text-primary shadow-[var(--shadow-overlay)] outline-none data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=open]:fade-in-0 data-[state=closed]:fade-out-0 data-[state=open]:zoom-in-[0.99] data-[state=closed]:zoom-out-[0.99] data-[state=open]:duration-[var(--motion-surface)] data-[state=closed]:duration-[var(--motion-exit)] data-[state=open]:ease-premium data-[state=closed]:ease-in dark:border-white/15 dark:bg-deep-sea dark:text-text-primary',
            className
          )}
        >
          {content}
        </PopoverPrimitive.Content>
      </PopoverPrimitive.Portal>
    </PopoverPrimitive.Root>
  );
}
