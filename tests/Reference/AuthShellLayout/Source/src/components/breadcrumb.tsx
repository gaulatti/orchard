'use client';

import { ChevronRight, Ellipsis } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';
import { type NavItem, renderDefaultLink, type RenderLink } from './nav-menu';

export interface BreadcrumbItem extends NavItem {
  href: string;
  label: string;
}

export interface BreadcrumbProps {
  className?: string;
  /** Collapse items in the middle when there are more than this many items. 0 = never collapse. */
  collapsedAfter?: number;
  items: BreadcrumbItem[];
  renderLink?: RenderLink<BreadcrumbItem>;
  separator?: React.ReactNode;
}

export function Breadcrumb({ className, collapsedAfter = 0, items, renderLink, separator }: BreadcrumbProps) {
  const [expanded, setExpanded] = React.useState(false);
  const linkRenderer = renderLink ?? renderDefaultLink<BreadcrumbItem>;
  const separatorNode = separator ?? <ChevronRight size={14} className='flex-shrink-0 text-text-secondary' aria-hidden='true' />;

  const shouldCollapse = collapsedAfter > 0 && !expanded && items.length > collapsedAfter + 1;
  const visibleItems = shouldCollapse ? [items[0], null, ...items.slice(-(collapsedAfter > 0 ? 1 : items.length))] : items;

  return (
    <nav aria-label='Breadcrumb' className={cn('flex items-center', className)}>
      <ol className='flex flex-wrap items-center gap-1.5'>
        {visibleItems.map((item, index) => {
          const isLast = index === visibleItems.length - 1;

          if (item === null) {
            // Collapsed placeholder
            return (
              <React.Fragment key='ellipsis'>
                <li className='flex items-center'>
                  <button
                    type='button'
                    onClick={() => setExpanded(true)}
                    aria-label='Show full path'
                    className='inline-flex h-6 w-6 scale-100 items-center justify-center rounded-md text-text-secondary transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium hover:bg-sand/10 hover:text-text-primary active:scale-[0.9] dark:hover:bg-sand/20 dark:hover:text-text-primary'
                  >
                    <Ellipsis size={14} />
                  </button>
                </li>
                <li aria-hidden='true' className='flex items-center'>
                  {separatorNode}
                </li>
              </React.Fragment>
            );
          }

          return (
            <React.Fragment key={`${item.href}:${item.label}`}>
              <li className='flex items-center'>
                {isLast ? (
                  <span aria-current='page' className='text-sm font-medium text-text-primary dark:text-text-primary'>
                    {item.label}
                  </span>
                ) : (
                  linkRenderer({
                    item,
                    className: 'text-sm text-text-secondary transition-colors duration-[var(--motion-control)] ease-premium hover:text-text-primary dark:text-text-secondary dark:hover:text-text-primary',
                    children: item.label
                  })
                )}
              </li>
              {!isLast && (
                <li aria-hidden='true' className='flex items-center'>
                  {separatorNode}
                </li>
              )}
            </React.Fragment>
          );
        })}
      </ol>
    </nav>
  );
}
