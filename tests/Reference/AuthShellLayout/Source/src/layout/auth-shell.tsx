import React from 'react';

import { cn } from '../utils/cn';

export type AuthShellLayout = 'centered' | 'split';
export type AuthShellSide = 'start' | 'end';

export interface AuthShellProps extends React.HTMLAttributes<HTMLElement> {
  aside?: React.ReactNode;
  asideLabel?: string;
  brand?: React.ReactNode;
  children: React.ReactNode;
  footer?: React.ReactNode;
  layout?: AuthShellLayout;
  side?: AuthShellSide;
}

const splitColumnClasses: Record<AuthShellSide, string> = {
  start: 'lg:grid-cols-[minmax(24rem,5fr)_minmax(0,7fr)]',
  end: 'lg:grid-cols-[minmax(0,7fr)_minmax(24rem,5fr)]'
};

/** A page-level shell for sign-in, recovery, invitation, and other access flows. */
export function AuthShell({
  aside,
  asideLabel,
  brand,
  children,
  className,
  footer,
  layout = 'centered',
  side = 'end',
  ...props
}: AuthShellProps) {
  const hasAside = layout === 'split' && Boolean(aside);

  return (
    <main
      className={cn(
        'min-h-screen bg-light-sand/35 text-text-primary dark:bg-background dark:text-text-primary',
        hasAside && 'grid bg-background',
        hasAside && splitColumnClasses[side],
        className
      )}
      data-layout={layout}
      data-side={side}
      {...props}
    >
      <section className='flex min-h-screen bg-background px-5 py-6 sm:px-8 sm:py-8 lg:px-12 xl:px-16' data-slot='access'>
        <div className='mx-auto flex w-full max-w-md flex-1 flex-col'>
          {brand ? <div data-slot='brand'>{brand}</div> : null}
          <div className='flex flex-1 items-center py-10 sm:py-14' data-slot='content'>
            <div className='w-full'>{children}</div>
          </div>
          {footer ? <footer className='font-secondary text-xs leading-5 text-text-secondary' data-slot='footer'>{footer}</footer> : null}
        </div>
      </section>
      {hasAside ? (
        <aside
          aria-label={asideLabel}
          className={cn(
            'relative hidden min-h-screen overflow-hidden bg-deep-sea text-white lg:flex',
            side === 'start' && 'lg:order-first'
          )}
          data-slot='aside'
        >
          <div className='relative z-10 flex w-full items-end p-12 xl:p-16'>{aside}</div>
          <div
            className='pointer-events-none absolute -right-24 -top-24 size-80 rounded-full border border-white/10'
            aria-hidden='true'
          />
          <div
            className='pointer-events-none absolute -right-10 -top-10 size-52 rounded-full border border-white/10'
            aria-hidden='true'
          />
        </aside>
      ) : null}
    </main>
  );
}
