import React from 'react';

import { cn } from '../utils/cn';

export type PageFrameElement = 'article' | 'div' | 'main' | 'section';
export type PageFrameGutter = 'none' | 'compact' | 'comfortable' | 'spacious';
export type PageFrameVerticalSpacing = 'none' | 'compact' | 'comfortable' | 'spacious';
export type PageFrameWidth = 'reading' | 'content' | 'wide' | 'full';

export interface PageFrameProps extends React.HTMLAttributes<HTMLElement> {
  as?: PageFrameElement;
  children: React.ReactNode;
  gutter?: PageFrameGutter;
  verticalSpacing?: PageFrameVerticalSpacing;
  width?: PageFrameWidth;
}

const widthClasses: Record<PageFrameWidth, string> = {
  reading: 'max-w-3xl',
  content: 'max-w-6xl',
  wide: 'max-w-[90rem]',
  full: 'max-w-none'
};

const gutterClasses: Record<PageFrameGutter, string> = {
  none: '',
  compact: 'px-4 sm:px-5',
  comfortable: 'px-5 sm:px-8 lg:px-10',
  spacious: 'px-6 sm:px-10 lg:px-16'
};

const verticalSpacingClasses: Record<PageFrameVerticalSpacing, string> = {
  none: '',
  compact: 'py-6 sm:py-8',
  comfortable: 'py-10 sm:py-12 lg:py-16',
  spacious: 'py-14 sm:py-16 lg:py-24'
};

/** A centered page boundary with semantic measures and responsive breathing room. */
export function PageFrame({
  as: Component = 'div',
  children,
  className,
  gutter = 'comfortable',
  verticalSpacing = 'comfortable',
  width = 'content',
  ...props
}: PageFrameProps) {
  return (
    <Component
      className={cn('mx-auto w-full', widthClasses[width], gutterClasses[gutter], verticalSpacingClasses[verticalSpacing], className)}
      {...props}
    >
      {children}
    </Component>
  );
}
