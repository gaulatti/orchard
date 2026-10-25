import React from 'react';

import { cn } from '../utils/cn';

export interface PanelColumnProps {
  children: React.ReactNode;
  className?: string;
  /** When true, the column expands to fill available horizontal space. */
  grow?: boolean;
}

/**
 * Stacks two or more Panels vertically within a single PanelLayout column.
 *  Add `grow` to a Panel child to make it fill all remaining vertical space.
 *  Panels without `grow` size to their content / fixed height.
 */
export function PanelColumn({ children, className }: PanelColumnProps) {
  return (
    <div className={cn('flex flex-col flex-1 h-full min-h-0 overflow-hidden', className)}>
      {React.Children.map(children, (child) => {
        if (!React.isValidElement(child)) return child;
        const shouldFill = Boolean((child.props as { grow?: boolean }).grow);
        return <div className={cn('flex flex-col min-h-0 overflow-hidden', shouldFill ? 'flex-1' : 'shrink-0')}>{child}</div>;
      })}
    </div>
  );
}

export interface PanelLayoutProps {
  children: React.ReactNode;
  className?: string;
  /** Optional padding applied around the panel container. @default 'p-0' */
  padding?: string;
}

export function PanelLayout({ children, className, padding }: PanelLayoutProps) {
  const resolvedPadding = padding ?? 'p-0';

  return (
    <div className={cn('flex flex-1 min-h-0 overflow-x-auto overflow-y-hidden', resolvedPadding, className)}>
      <div className='flex flex-1 h-full min-h-0 gap-0 flex-nowrap'>
        {React.Children.map(children, (child) => {
          if (!React.isValidElement(child)) return child;
          const shouldFill = Boolean((child.props as { grow?: boolean }).grow);

          // PanelColumn already handles its own layout — render it directly.
          if (child.type === PanelColumn) {
            return <div className={cn('flex h-full min-h-0 flex-col', shouldFill ? 'flex-1 min-w-0' : 'shrink-0')}>{child}</div>;
          }

          return <div className={cn('flex h-full min-h-0 flex-col', shouldFill ? 'flex-1 min-w-0' : 'shrink-0')}>{child}</div>;
        })}
      </div>
    </div>
  );
}
