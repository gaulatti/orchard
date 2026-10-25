'use client';

import { ChevronDown } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';
import { usePresence } from '../utils/hooks';
import { type NavItem, renderDefaultLink, type RenderLink } from './nav-menu';

export interface NavigationMenuLink extends NavItem {
  description?: string;
}

export interface NavigationMenuGroup {
  trigger: string;
  href?: string;
  items: NavigationMenuLink[];
}

export type NavigationMenuEntry = NavigationMenuLink | NavigationMenuGroup;

function isGroup(entry: NavigationMenuEntry): entry is NavigationMenuGroup {
  return 'items' in entry;
}

export interface NavigationMenuProps<TLink extends NavigationMenuLink = NavigationMenuLink> {
  className?: string;
  entries: NavigationMenuEntry[];
  renderLink?: RenderLink<TLink>;
}

function NavigationMenuSurface({ children, open }: { children: React.ReactNode; open: boolean }) {
  const { present, visible } = usePresence(open, 150);
  if (!present) return null;

  return (
    <div
      className={cn(
        'absolute left-0 top-full z-50 mt-1 min-w-[16rem] origin-top-left rounded-[var(--radius-ui)] border border-sand/35 bg-white p-1.5 shadow-[var(--shadow-overlay)] transition-[opacity,transform] dark:border-white/15 dark:bg-deep-sea',
        visible
          ? 'translate-y-0 scale-100 opacity-100 duration-[var(--motion-surface)] ease-premium'
          : '-translate-y-1 scale-[0.992] opacity-0 duration-[var(--motion-exit)] ease-in'
      )}
    >
      {children}
    </div>
  );
}

export function NavigationMenu<TLink extends NavigationMenuLink>({ className, entries, renderLink }: NavigationMenuProps<TLink>) {
  const [activeIndex, setActiveIndex] = React.useState<number | null>(null);
  const linkRenderer = (renderLink ?? renderDefaultLink) as RenderLink<NavigationMenuLink>;
  const containerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (activeIndex === null) return;
    const handler = (e: PointerEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setActiveIndex(null);
    };
    document.addEventListener('pointerdown', handler);
    return () => document.removeEventListener('pointerdown', handler);
  }, [activeIndex]);

  return (
    <div ref={containerRef} className={cn('relative flex items-center gap-1', className)}>
      {entries.map((entry, i) => {
        if (!isGroup(entry)) {
          return (
            <React.Fragment key={entry.href}>
              {linkRenderer({
                item: entry as NavigationMenuLink,
                className:
                  'rounded-[var(--radius-ui)] px-3 py-2 text-sm font-medium text-text-secondary transition-colors duration-[var(--motion-control)] ease-premium hover:bg-sand/10 hover:text-text-primary dark:text-text-secondary dark:hover:bg-sand/15 dark:hover:text-text-primary',
                children: entry.label
              })}
            </React.Fragment>
          );
        }

        const isOpen = activeIndex === i;

        return (
          <div key={entry.trigger} className='relative'>
            <button
              type='button'
              onClick={() => setActiveIndex(isOpen ? null : i)}
              aria-expanded={isOpen}
              className={cn(
                'flex translate-y-0 items-center gap-1 rounded-[var(--radius-button)] px-3 py-2 text-[13px] font-medium tracking-ui transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium active:translate-y-px focus:outline-none focus-visible:ring-2 focus-visible:ring-sea/30 dark:focus-visible:ring-accent-blue/40',
                isOpen
                  ? 'bg-sand/10 text-text-primary dark:bg-sand/15 dark:text-text-primary'
                  : 'text-text-secondary hover:bg-sand/10 hover:text-text-primary dark:text-text-secondary dark:hover:bg-sand/15 dark:hover:text-text-primary'
              )}
            >
              {entry.trigger}
              <ChevronDown size={14} className={cn('transition-transform duration-[var(--motion-surface)] ease-premium', isOpen ? 'rotate-180' : 'rotate-0')} />
            </button>

            {/* Dropdown */}
            <NavigationMenuSurface open={isOpen}>
                {entry.items.map((item) => (
                  <React.Fragment key={item.href}>
                    {linkRenderer({
                      item,
                      className: 'flex flex-col rounded-[var(--radius-ui)] px-3 py-2.5 transition-colors duration-[var(--motion-control)] ease-premium hover:bg-sand/10 dark:hover:bg-sand/15',
                      children: (
                        <>
                          <span className='text-sm font-medium text-text-primary dark:text-text-primary'>{item.label}</span>
                          {item.description ? (
                            <span className='mt-0.5 text-xs leading-relaxed text-text-secondary dark:text-text-secondary'>{item.description}</span>
                          ) : null}
                        </>
                      ),
                      onClick: () => setActiveIndex(null)
                    })}
                  </React.Fragment>
                ))}
            </NavigationMenuSurface>
          </div>
        );
      })}
    </div>
  );
}
