'use client';

import * as React from 'react';
import { ChevronRight } from 'lucide-react';
import * as CollapsiblePrimitive from '@radix-ui/react-collapsible';

import { cn } from '../utils/cn';

export interface SidebarItem {
  id: string;
  label: string;
  href?: string;
  icon?: React.ReactNode;
  badge?: React.ReactNode;
  active?: boolean;
  disabled?: boolean;
  items?: SidebarItem[];
}

export interface SidebarProps {
  items: SidebarItem[];
  header?: React.ReactNode;
  footer?: React.ReactNode;
  className?: string;
  collapsed?: boolean;
  onItemClick?: (item: SidebarItem) => void;
  renderLink?: (props: { item: SidebarItem & { href: string }; className?: string; children: React.ReactNode; onClick?: () => void }) => React.ReactNode;
}

function SidebarLink({
  item,
  collapsed,
  renderLink,
  onClick
}: {
  item: SidebarItem & { href: string };
  collapsed?: boolean;
  renderLink?: SidebarProps['renderLink'];
  onClick?: () => void;
}) {
  const content = (
    <>
      {item.icon && <span className='flex h-5 w-5 shrink-0 items-center justify-center'>{item.icon}</span>}
      {!collapsed && (
        <>
          <span className='flex-1 truncate text-sm font-medium'>{item.label}</span>
          {item.badge && <span className='ml-auto shrink-0'>{item.badge}</span>}
        </>
      )}
    </>
  );

  const className = cn(
    'flex min-h-10 items-center gap-3 rounded-[var(--radius-ui)] px-3 text-text-secondary transition-[background-color,color,box-shadow,transform] duration-[var(--motion-control)] ease-premium hover:bg-light-sand/55 hover:text-text-primary active:translate-y-px dark:text-text-secondary dark:hover:bg-white/[0.05] dark:hover:text-text-primary',
    item.active && 'bg-light-sand/80 font-medium text-deep-sea shadow-[inset_2px_0_0_var(--color-sea)] dark:bg-white/[0.07] dark:text-accent-blue dark:shadow-[inset_2px_0_0_var(--color-accent-blue)]',
    item.disabled && 'pointer-events-none opacity-50'
  );

  if (renderLink) {
    return renderLink({ item, className, children: content, onClick });
  }

  return (
    <a href={item.href} className={className} onClick={onClick} aria-current={item.active ? 'page' : undefined}>
      {content}
    </a>
  );
}

function SidebarGroup({
  item,
  collapsed,
  renderLink,
  onItemClick
}: {
  item: SidebarItem;
  collapsed?: boolean;
  renderLink?: SidebarProps['renderLink'];
  onItemClick?: (item: SidebarItem) => void;
}) {
  const [open, setOpen] = React.useState(item.items?.some((child) => child.active) ?? false);

  if (collapsed || !item.items || item.items.length === 0) {
    if (item.href) {
      return <SidebarLink item={item as SidebarItem & { href: string }} collapsed={collapsed} renderLink={renderLink} onClick={() => onItemClick?.(item)} />;
    }
    return null;
  }

  return (
    <CollapsiblePrimitive.Root open={open} onOpenChange={setOpen}>
      <CollapsiblePrimitive.Trigger asChild>
        <button
          type='button'
          className={cn(
            'flex min-h-10 w-full items-center gap-3 rounded-[var(--radius-ui)] px-3 text-text-secondary transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium hover:bg-light-sand/55 hover:text-text-primary active:translate-y-px dark:text-text-secondary dark:hover:bg-white/[0.05] dark:hover:text-text-primary',
            item.active && 'bg-light-sand/80 font-medium text-deep-sea dark:bg-white/[0.07] dark:text-accent-blue'
          )}
        >
          {item.icon && <span className='flex h-5 w-5 shrink-0 items-center justify-center'>{item.icon}</span>}
          <span className='flex-1 truncate text-left text-sm font-medium'>{item.label}</span>
          {item.badge && <span className='ml-auto shrink-0'>{item.badge}</span>}
          <ChevronRight size={14} className={cn('shrink-0 transition-transform duration-[var(--motion-surface)] ease-premium', open && 'rotate-90')} />
        </button>
      </CollapsiblePrimitive.Trigger>
      <CollapsiblePrimitive.Content className='overflow-hidden'>
        <div className='relative ml-5 mt-1 border-l border-sand/20 pl-4 dark:border-sand/20'>
          {item.items.map((child) =>
            child.href ? (
              <SidebarLink
                key={child.id}
                item={child as SidebarItem & { href: string }}
                collapsed={collapsed}
                renderLink={renderLink}
                onClick={() => onItemClick?.(child)}
              />
            ) : null
          )}
        </div>
      </CollapsiblePrimitive.Content>
    </CollapsiblePrimitive.Root>
  );
}

export function Sidebar({ items, header, footer, className, collapsed = false, onItemClick, renderLink }: SidebarProps) {
  return (
    <aside
      className={cn(
        'flex h-full flex-col border-r border-sand/25 bg-white transition-[width,padding] duration-200 dark:border-white/[0.08] dark:bg-deep-sea',
        collapsed ? 'w-16 px-2 py-5' : 'w-64 px-3 py-5',
        className
      )}
    >
      {header && <div className={cn('mb-5 border-b border-sand/20 pb-5 dark:border-white/[0.07]', collapsed && 'flex justify-center')}>{header}</div>}

      <nav className='flex-1 space-y-1 overflow-y-auto scrollbar-thin'>
        {items.map((item) => (
          <SidebarGroup key={item.id} item={item} collapsed={collapsed} renderLink={renderLink} onItemClick={onItemClick} />
        ))}
      </nav>

      {footer && <div className={cn('mt-auto border-t border-sand/20 pt-4 dark:border-white/[0.07]', collapsed && 'flex justify-center')}>{footer}</div>}
    </aside>
  );
}
