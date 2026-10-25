import React from 'react';

import { cn } from '../utils/cn';

export interface PanelProps {
  /**
   * A CSS color value used as the left-border accent and LED dot color.
   * Defaults to the theme's `sea` color.
   * @example '#3b82f6'
   */
  accent?: string;
  children: React.ReactNode;
  className?: string;
  /** Item count displayed in the header badge */
  count?: number;
  /** Optional drag handle element rendered before the title (for DnD integration) */
  dragHandle?: React.ReactNode;
  /** Optional filter/search content rendered below the title row */
  filter?: React.ReactNode;
  /**
   * When used inside `PanelLayout`, lets this column grow to consume
   * remaining horizontal space.
   */
  grow?: boolean;
  /** @deprecated No visual effect in monitor-only mode. */
  isDragging?: boolean;
  /** @deprecated Panel is monitor-only; this prop is ignored. */
  variant?: 'monitor';
  title: string;
  /** Optional toolbar rendered below the filter row */
  toolbar?: React.ReactNode;
  /**
   * Fixed width for the panel.
   * Accepts any valid CSS width value, e.g. `'320px'` or `320`.
   */
  width?: number | string;
}

export function Panel({
  accent,
  children,
  className,
  count,
  dragHandle,
  filter,
  grow: _grow,
  isDragging: _isDragging,
  variant: _variant,
  title,
  toolbar,
  width
}: PanelProps) {
  const widthValue = typeof width === 'number' ? `${width}px` : width;
  const accentStyle: React.CSSProperties = {
    ...(accent ? ({ '--panel-accent': accent } as React.CSSProperties) : {}),
    ...(widthValue ? { width: widthValue, minWidth: widthValue } : {}),
    borderLeftColor: accent ?? 'var(--color-sea)'
  };

  return (
    <div
      className={cn(
        'group flex h-full min-h-0 flex-col overflow-hidden transition-[border-color] duration-200',
        'border-l-2 border-r border-r-sand/20 border-y-0 rounded-none bg-transparent shadow-none dark:border-r-white/[0.06]',
        className
      )}
      style={accentStyle}
    >
      {/* Panel header */}
      <div
        className={cn(
          'relative z-10 flex flex-col bg-white dark:bg-deep-sea',
          'select-none border-b border-black/[0.06] dark:border-white/[0.08]'
        )}
      >
        <div className='flex min-h-14 items-center gap-3 px-5 py-3.5'>
          {dragHandle ? (
            <span className='cursor-grab text-black/40 transition-colors duration-[var(--motion-control)] ease-premium hover:text-black/80 active:cursor-grabbing dark:text-white/40 dark:hover:text-white/80'>
              {dragHandle}
            </span>
          ) : null}

          {/* Quiet status marker */}
          <span
            className='h-1.5 w-1.5 flex-shrink-0 rounded-full opacity-80'
            style={{ backgroundColor: accent ?? 'var(--color-sea)', color: accent ?? 'var(--color-sea)' }}
          />

          <span className='flex-1 text-[11px] font-semibold uppercase tracking-[0.1em] text-[color:var(--panel-accent,var(--color-sea))]'>{title}</span>

          {count !== undefined ? (
            <span className='rounded-[5px] border border-sand/30 bg-light-sand/40 px-2 py-0.5 text-[11px] font-semibold tabular-nums text-text-primary dark:border-white/10 dark:bg-white/5'>
              {count}
            </span>
          ) : null}
        </div>

        {filter ? <div className='px-5 pb-4'>{filter}</div> : null}

        {toolbar ? (
          <div className='flex items-center justify-center gap-2 border-t border-sand/20 bg-light-sand/20 px-5 py-3 dark:border-white/[0.06] dark:bg-white/[0.03]'>
            {toolbar}
          </div>
        ) : null}
      </div>

      {/* Scrollable feed */}
      <div className='flex-1 min-h-0 overflow-y-auto bg-light-sand/10 dark:bg-transparent scrollbar-thin scrollbar-track-transparent scrollbar-thumb-black/10 hover:scrollbar-thumb-black/20 dark:scrollbar-thumb-white/10 dark:hover:scrollbar-thumb-white/20'>
        {children}
      </div>
    </div>
  );
}
