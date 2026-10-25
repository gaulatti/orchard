import React from 'react';

import { BrandLockup, type BrandLockupProps } from '../components/brand-lockup';
import { type NavItem, type RenderLink, renderDefaultLink } from '../components/nav-menu';
import { cn } from '../utils/cn';

export interface FooterSection {
  items: NavItem[];
  title: string;
}

export interface FooterProps {
  bottomLeft?: React.ReactNode;
  bottomRight?: React.ReactNode;
  brand: BrandLockupProps & {
    description?: string;
  };
  className?: string;
  renderLink?: RenderLink;
  sections: FooterSection[];
  showBottomAccent?: boolean;
}

export function Footer({ bottomLeft, bottomRight, brand, className, renderLink, sections, showBottomAccent = true }: FooterProps) {
  const linkRenderer = renderLink ?? renderDefaultLink;
  const githubItem = sections.flatMap((section) => section.items).find((item) => item.href.includes('github.com'));
  const brandColumnClassName = sections.length <= 2 ? 'lg:col-span-2' : undefined;
  const resolvedBottomRight =
    bottomRight ??
    (githubItem
      ? linkRenderer({
          item: githubItem,
          className: 'underline-offset-4 transition-colors duration-[var(--motion-control)] ease-premium hover:text-sea hover:underline dark:hover:text-accent-blue',
          children: 'View source on GitHub'
        })
      : null);

  return (
    <footer className={cn('bg-light-sand text-text-primary dark:bg-sand dark:text-text-primary', className)}>
      <div className='h-px w-full bg-sand/30 dark:bg-white/10'></div>

      <div className='container mx-auto px-4 py-16'>
        <div className='grid grid-cols-1 gap-12 md:grid-cols-2 lg:grid-cols-4'>
          <div className={cn(brandColumnClassName)}>
            <BrandLockup {...brand} size='lg' renderLink={renderLink} />
            {brand.description ? (
              <p className='mt-8 max-w-md leading-relaxed tracking-refined text-text-secondary dark:text-text-secondary'>{brand.description}</p>
            ) : null}
          </div>

          {sections.map((section) => (
            <div key={section.title}>
              <h3 className='mb-8 text-sm font-medium uppercase tracking-elegant text-desert dark:text-desert'>{section.title}</h3>
              <ul className='space-y-4'>
                {section.items.map((item) => (
                  <li key={`${section.title}:${item.href}:${item.label}`}>
                    {linkRenderer({
                      item,
                      className: 'text-text-secondary transition-colors duration-[var(--motion-surface)] ease-premium hover:text-sunset dark:text-text-secondary dark:hover:text-sunset',
                      children: item.label
                    })}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className='mt-16 flex flex-col items-center justify-between gap-4 border-t border-sand/20 pt-7 md:flex-row'>
          <div className='text-sm tracking-refined text-text-secondary dark:text-text-secondary'>{bottomLeft}</div>
          <div className='text-sm tracking-refined text-text-secondary/70 dark:text-text-secondary/70'>{resolvedBottomRight}</div>
        </div>
      </div>

      {showBottomAccent ? <div className='h-0.5 w-full bg-sea/70 dark:bg-accent-blue/70'></div> : null}
    </footer>
  );
}
