import React from 'react';

import { cn } from '../utils/cn';

export interface NavItem {
  href: string;
  label: string;
  external?: boolean;
}

export interface RenderLinkProps<TItem extends NavItem> {
  item: TItem;
  className?: string;
  children: React.ReactNode;
  onClick?: () => void;
}

export type RenderLink<TItem extends NavItem = NavItem> = (props: RenderLinkProps<TItem>) => React.ReactNode;

export interface NavMenuProps<TItem extends NavItem = NavItem> {
  className?: string;
  direction?: 'horizontal' | 'vertical';
  itemClassName?: string;
  items: TItem[];
  onItemClick?: () => void;
  renderLink?: RenderLink<TItem>;
}

export function renderDefaultLink<TItem extends NavItem>({ children, className, item, onClick }: RenderLinkProps<TItem>) {
  return (
    <a
      href={item.href}
      className={className}
      onClick={onClick}
      rel={item.external ? 'noreferrer noopener' : undefined}
      target={item.external ? '_blank' : undefined}
    >
      {children}
    </a>
  );
}

export function NavMenu<TItem extends NavItem>({
  className,
  direction = 'horizontal',
  itemClassName,
  items,
  onItemClick,
  renderLink
}: NavMenuProps<TItem>) {
  const linkRenderer = renderLink ?? renderDefaultLink<TItem>;
  const wrapperClassName = direction === 'horizontal' ? 'flex items-center space-x-8' : 'flex flex-col space-y-4';
  const baseItemClassName =
    direction === 'horizontal'
      ? 'text-base font-medium tracking-refined text-text-primary transition-colors duration-400 hover:text-sea dark:text-text-primary dark:hover:text-accent-blue'
      : 'text-lg font-medium text-text-primary transition-colors duration-400 hover:text-sea dark:text-text-primary dark:hover:text-accent-blue';

  return (
    <nav className={cn(wrapperClassName, className)}>
      {items.map((item) => (
        <React.Fragment key={`${item.href}:${item.label}`}>
          {linkRenderer({
            item,
            className: cn(baseItemClassName, itemClassName),
            children: item.label,
            onClick: onItemClick
          })}
        </React.Fragment>
      ))}
    </nav>
  );
}
