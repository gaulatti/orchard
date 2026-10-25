import React from 'react';

import { cn } from '../utils/cn';

export interface FeedColumnProps extends React.HTMLAttributes<HTMLElement> {
  accent?: string;
  bodyClassName?: string;
  children: React.ReactNode;
  count?: number;
  filter?: React.ReactNode;
  title: string;
  viewportRef?: React.Ref<HTMLDivElement>;
  width?: number | string;
}

export function FeedColumn({
  accent = 'var(--color-sea)',
  bodyClassName,
  children,
  className,
  count,
  filter,
  style,
  title,
  viewportRef,
  width = 372,
  ...props
}: FeedColumnProps) {
  const resolvedWidth = typeof width === 'number' ? `${width}px` : width;

  return (
    <section
      {...props}
      className={cn(
        'flex h-full min-h-0 shrink-0 flex-col overflow-hidden rounded-[12px] border',
        'bg-deep-sea/75 shadow-[0_22px_54px_-38px_rgba(0,0,0,0.95)]',
        className
      )}
      style={{
        background: `linear-gradient(45deg, color-mix(in oklab, ${accent} 11%, rgba(24,37,49,.88)) 0, rgba(24,37,49,.88) 13rem, rgba(13,24,33,.96) 32rem)`,
        borderColor: `color-mix(in oklab, ${accent} 15%, rgba(255,255,255,.075))`,
        minWidth: resolvedWidth,
        width: resolvedWidth,
        ...style
      }}
    >
      <header className='shrink-0 border-b border-white/[0.065] px-4 pb-4 pt-4'>
        <div className='mb-4 flex min-h-7 items-center gap-2.5'>
          <span aria-hidden='true' className='h-2 w-2 rounded-full shadow-[0_0_14px_currentColor]' style={{ backgroundColor: accent, color: accent }} />
          <h2 className='flex-1 text-[11px] font-semibold uppercase tracking-[0.12em]' style={{ color: accent }}>{title}</h2>
          {count !== undefined ? (
            <span className='rounded-full border border-white/[0.08] bg-black/15 px-2.5 py-1 text-[11px] font-semibold tabular-nums text-text-primary/85'>
              {count}
            </span>
          ) : null}
        </div>
        {filter}
      </header>
      <div className={cn('h-full min-h-0 overflow-y-auto p-3', bodyClassName)} ref={viewportRef}>
        {children}
      </div>
    </section>
  );
}
