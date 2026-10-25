import * as React from 'react';

import { cn } from '../utils/cn';

export interface AdminShellProps {
  children: React.ReactNode;
  className?: string;
  sidebar?: React.ReactNode;
  header?: React.ReactNode;
  footer?: React.ReactNode;
  contentClassName?: string;
}

export function AdminShell({ children, className, sidebar, header, footer, contentClassName }: AdminShellProps) {
  return (
    <div className={cn('flex min-h-screen flex-col bg-background text-foreground', className)}>
      {header}
      <div className='flex flex-1 overflow-hidden'>
        {sidebar}
        <main className={cn('flex flex-1 flex-col min-h-0 overflow-y-auto scrollbar-thin bg-background', contentClassName)}>
          <div className='flex-1 px-5 py-6 sm:px-8 sm:py-8 lg:px-10 lg:py-10'>{children}</div>
          {footer && <div className='border-t border-border px-5 py-5 sm:px-8 lg:px-10'>{footer}</div>}
        </main>
      </div>
    </div>
  );
}
