'use client';

import { Menu, X } from 'lucide-react';
import React from 'react';

import { BrandLockup, type BrandLockupProps } from '../components/brand-lockup';
import { IconButton } from '../components/icon-button';
import { NavMenu, type NavItem, type RenderLink } from '../components/nav-menu';
import { cn } from '../utils/cn';

export interface HeaderProps {
  actions?: React.ReactNode;
  brand: BrandLockupProps;
  className?: string;
  fullWidth?: boolean;
  mobileActions?: React.ReactNode;
  navigation: NavItem[];
  position?: 'fixed' | 'sticky' | 'static';
  renderLink?: RenderLink;
}

export function Header({ actions, brand, className, fullWidth = false, mobileActions, navigation, position = 'fixed', renderLink }: HeaderProps) {
  const [mobileMenuOpen, setMobileMenuOpen] = React.useState(false);

  return (
    <header
      className={cn(
        'z-50 w-full border-b border-sand/25 bg-white/95 font-header backdrop-blur-[8px] dark:border-white/[0.09] dark:bg-deep-sea/95',
        position === 'fixed' && 'fixed top-0',
        position === 'sticky' && 'sticky top-0',
        position === 'static' && 'relative',
        className
      )}
    >
      <div className={cn(fullWidth ? 'w-full px-4' : 'container mx-auto px-4')}>
        <div className='flex h-[var(--bleecker-header-height)] items-center justify-between gap-8 lg:grid lg:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)]'>
          <div className='min-w-0 justify-self-start'>
            <BrandLockup {...brand} renderLink={renderLink} />
          </div>

          <div className='hidden lg:flex lg:items-center lg:justify-self-center'>
            <NavMenu items={navigation} renderLink={renderLink} />
          </div>

          <div className='hidden lg:flex lg:items-center lg:justify-self-end lg:gap-3'>{actions}</div>

          <IconButton
            variant='ghost'
            className='lg:hidden'
            aria-label='Toggle navigation menu'
            type='button'
            onClick={() => setMobileMenuOpen((value) => !value)}
          >
            {mobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
          </IconButton>
        </div>
      </div>

      <div
        className={cn(
          'absolute left-0 top-[var(--bleecker-header-height)] w-full origin-top overflow-hidden border-t border-sand/20 bg-white shadow-[var(--shadow-overlay)] transition-[max-height,opacity] duration-200 dark:border-white/10 dark:bg-deep-sea lg:hidden',
          mobileMenuOpen ? 'max-h-96 opacity-100' : 'max-h-0 opacity-0'
        )}
      >
        <div className={cn(fullWidth ? 'w-full' : 'container mx-auto', 'flex flex-col gap-5 px-4 py-6')}>
          {mobileActions ? <div className='flex items-center gap-3'>{mobileActions}</div> : null}
          <NavMenu direction='vertical' items={navigation} onItemClick={() => setMobileMenuOpen(false)} renderLink={renderLink} />
        </div>
      </div>
    </header>
  );
}
