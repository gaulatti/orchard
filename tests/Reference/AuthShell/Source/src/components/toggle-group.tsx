'use client';

import React from 'react';

import { cn } from '../utils/cn';
import { Toggle, type ToggleProps } from './toggle';

export type ToggleGroupType = 'single' | 'multiple';

interface ToggleGroupSingleProps {
  type: 'single';
  value?: string;
  onValueChange?: (value: string) => void;
}

interface ToggleGroupMultipleProps {
  type: 'multiple';
  value?: string[];
  onValueChange?: (value: string[]) => void;
}

export type ToggleGroupProps = (ToggleGroupSingleProps | ToggleGroupMultipleProps) & {
  children?: React.ReactNode;
  className?: string;
  disabled?: boolean;
  size?: ToggleProps['size'];
  variant?: ToggleProps['variant'];
};

export interface ToggleGroupItemProps extends Omit<ToggleProps, 'pressed' | 'onPressedChange'> {
  value: string;
}

const ToggleGroupContext = React.createContext<{
  type: ToggleGroupType;
  value: string | string[];
  onValueChange: (itemValue: string) => void;
  size?: ToggleProps['size'];
  variant?: ToggleProps['variant'];
  disabled?: boolean;
} | null>(null);

function useToggleGroup() {
  const ctx = React.useContext(ToggleGroupContext);
  if (!ctx) throw new Error('ToggleGroupItem must be used inside ToggleGroup');
  return ctx;
}

export function ToggleGroup({ children, className, disabled, size, type, variant, ...rest }: ToggleGroupProps) {
  const currentValue = type === 'single' ? ((rest as ToggleGroupSingleProps).value ?? '') : ((rest as ToggleGroupMultipleProps).value ?? []);

  const handleChange = (itemValue: string) => {
    if (type === 'single') {
      const { onValueChange } = rest as ToggleGroupSingleProps;
      onValueChange?.(currentValue === itemValue ? '' : itemValue);
    } else {
      const { onValueChange } = rest as ToggleGroupMultipleProps;
      const arr = currentValue as string[];
      onValueChange?.(arr.includes(itemValue) ? arr.filter((v) => v !== itemValue) : [...arr, itemValue]);
    }
  };

  return (
    <ToggleGroupContext.Provider value={{ type, value: currentValue, onValueChange: handleChange, size, variant, disabled }}>
      <div role='group' className={cn('inline-flex items-center gap-0.5 rounded-[9px] border border-sand/20 bg-light-sand/65 p-1 dark:border-white/[0.07] dark:bg-white/[0.05]', className)}>
        {children}
      </div>
    </ToggleGroupContext.Provider>
  );
}

export function ToggleGroupItem({ children, className, disabled, size, value, variant, ...props }: ToggleGroupItemProps) {
  const ctx = useToggleGroup();
  const pressed = ctx.type === 'single' ? ctx.value === value : (ctx.value as string[]).includes(value);
  const isDisabled = disabled ?? ctx.disabled;

  return (
    <Toggle
      pressed={pressed}
      onPressedChange={() => ctx.onValueChange(value)}
      disabled={isDisabled}
      size={size ?? ctx.size}
      variant={variant ?? ctx.variant}
      className={cn('rounded-lg', className)}
      {...props}
    >
      {children}
    </Toggle>
  );
}
