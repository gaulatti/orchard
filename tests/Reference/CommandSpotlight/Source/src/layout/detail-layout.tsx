import React from 'react';

import { cn } from '../utils/cn';

export type DetailLayoutRatio = 'balanced' | 'primary';
export type DetailLayoutSide = 'start' | 'end';

export interface DetailLayoutProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  ratio?: DetailLayoutRatio;
  side?: DetailLayoutSide;
  sideRail?: React.ReactNode;
  sideRailLabel?: string;
  sticky?: boolean;
}

const columnClasses: Record<DetailLayoutSide, Record<DetailLayoutRatio, string>> = {
  start: {
    balanced: 'lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]',
    primary: 'lg:grid-cols-[minmax(17rem,1fr)_minmax(0,2fr)]'
  },
  end: {
    balanced: 'lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]',
    primary: 'lg:grid-cols-[minmax(0,2fr)_minmax(17rem,1fr)]'
  }
};

/** A responsive detail composition with an optional, independently sticky side rail. */
export function DetailLayout({
  children,
  className,
  ratio = 'primary',
  side = 'end',
  sideRail,
  sideRailLabel,
  sticky = true,
  ...props
}: DetailLayoutProps) {
  return (
    <div
      className={cn('grid items-start gap-10 lg:gap-12 xl:gap-16', sideRail && columnClasses[side][ratio], className)}
      data-ratio={ratio}
      data-side={side}
      {...props}
    >
      <div className='min-w-0' data-slot='content'>
        {children}
      </div>
      {sideRail ? (
        <aside
          aria-label={sideRailLabel}
          className={cn('min-w-0 self-start', side === 'start' && 'lg:order-first', sticky && 'lg:sticky lg:top-8')}
          data-slot='side-rail'
        >
          {sideRail}
        </aside>
      ) : null}
    </div>
  );
}
