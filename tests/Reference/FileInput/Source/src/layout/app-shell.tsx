import React from 'react';

import { cn } from '../utils/cn';

export interface AppShellProps {
  children: React.ReactNode;
  className?: string;
  contentClassName?: string;
  footer?: React.ReactNode;
  header?: React.ReactNode;
  offsetHeader?: boolean;
}

export function AppShell({ children, className, contentClassName, footer, header, offsetHeader = true }: AppShellProps) {
  return (
    <div className={cn('flex min-h-screen flex-col bg-background text-foreground', className)}>
      {header}
      <main className={cn('flex min-h-0 flex-1 flex-col bg-background', header && offsetHeader && 'pt-[var(--bleecker-header-height)]', contentClassName)}>
        <div className='flex min-h-0 flex-1 flex-col'>{children}</div>
      </main>
      {footer}
    </div>
  );
}
