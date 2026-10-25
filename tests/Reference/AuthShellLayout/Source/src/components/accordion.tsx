'use client';
import { ChevronDown } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';

export interface AccordionItem {
  actions?: React.ReactNode;
  content: React.ReactNode;
  id: string;
  subtitle?: React.ReactNode;
  title: React.ReactNode;
}

export interface AccordionProps {
  className?: string;
  defaultExpandedId?: string | null;
  items: AccordionItem[];
}

export function Accordion({ className, defaultExpandedId = null, items }: AccordionProps) {
  const [expandedId, setExpandedId] = React.useState<string | null>(defaultExpandedId);

  return (
    <div className={cn('overflow-hidden rounded-[var(--radius-card)] border border-sand/30 bg-white shadow-[var(--shadow-surface)] dark:border-white/10 dark:bg-deep-sea', className)}>
      <ul className='divide-y divide-sand/20 dark:divide-white/[0.07]'>
        {items.map((item) => {
          const isExpanded = expandedId === item.id;

          return (
            <li key={item.id}>
              <div className='flex items-center transition-colors duration-[var(--motion-surface)] ease-premium hover:bg-light-sand/35 dark:hover:bg-white/[0.03]'>
                <button
                  type='button'
                  aria-expanded={isExpanded}
                  className='flex min-w-0 flex-1 items-center gap-3 px-4 py-4 text-left outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-sea/25 sm:px-5'
                  onClick={() => setExpandedId(isExpanded ? null : item.id)}
                >
                  <ChevronDown
                    size={16}
                    className={cn('shrink-0 text-text-secondary transition-transform duration-200 ease-out-expo', isExpanded ? 'rotate-0' : '-rotate-90')}
                  />
                  <div className='min-w-0'>
                    <div className='text-sm font-medium text-text-primary dark:text-text-primary'>{item.title}</div>
                    {item.subtitle ? <div className='font-secondary mt-1 text-xs text-text-secondary dark:text-text-secondary'>{item.subtitle}</div> : null}
                  </div>
                </button>
                {item.actions ? (
                  <div className='mr-4 flex shrink-0 items-center gap-2 sm:mr-5'>
                    {item.actions}
                  </div>
                ) : null}
              </div>
              <div className={cn('grid transition-[grid-template-rows] duration-200 ease-out-expo', isExpanded ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]')}>
                <div className='overflow-hidden'>
                  <div className='border-t border-sand/15 bg-light-sand/25 dark:border-white/[0.05] dark:bg-white/[0.02]'>{item.content}</div>
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
