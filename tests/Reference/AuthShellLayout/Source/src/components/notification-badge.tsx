import * as React from 'react';
import { Bell } from 'lucide-react';

import { cn } from '../utils/cn';
import { IconButton } from './icon-button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from './dropdown-menu';

export interface NotificationItem {
  id: string;
  title: string;
  description?: string;
  timestamp?: string;
  unread?: boolean;
  onClick?: () => void;
}

export interface NotificationBadgeProps {
  count?: number;
  items?: NotificationItem[];
  onMarkAllRead?: () => void;
  className?: string;
}

export function NotificationBadge({ count = 0, items = [], onMarkAllRead, className }: NotificationBadgeProps) {
  const hasUnread = count > 0 || items.some((item) => item.unread);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <div className={cn('relative', className)}>
          <IconButton aria-label='Notifications'>
            <Bell size={18} />
          </IconButton>
          {count > 0 && (
            <span className='absolute right-0 top-0 flex h-4 min-w-4 items-center justify-center rounded-full bg-terracotta px-1 text-[10px] font-semibold text-white'>
              {count > 99 ? '99+' : count}
            </span>
          )}
        </div>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='end' className='w-80'>
        <div className='flex items-center justify-between px-2 py-1.5'>
          <DropdownMenuLabel className='uppercase'>Notifications</DropdownMenuLabel>
          {hasUnread && onMarkAllRead && (
            <button type='button' onClick={onMarkAllRead} className='text-xs text-sea transition-colors duration-[var(--motion-control)] ease-premium hover:text-deep-sea hover:underline dark:text-accent-blue dark:hover:text-text-primary'>
              Mark all read
            </button>
          )}
        </div>
        <DropdownMenuSeparator />
        {items.length === 0 ? (
          <div className='px-2 py-6 text-center text-sm text-text-secondary dark:text-text-secondary'>No notifications</div>
        ) : (
          items.map((item) => (
            <DropdownMenuItem key={item.id} onClick={item.onClick} className='flex flex-col items-start gap-0.5 py-2'>
              <div className='flex w-full items-start gap-2'>
                {item.unread && <span className='mt-1.5 h-2 w-2 shrink-0 rounded-full bg-sea dark:bg-accent-blue' />}
                <div className='flex-1'>
                  <p className={cn('text-sm', item.unread && 'font-semibold')}>{item.title}</p>
                  {item.description && <p className='text-xs text-text-secondary dark:text-text-secondary'>{item.description}</p>}
                  {item.timestamp && <p className='mt-1 text-xs text-text-secondary dark:text-text-secondary'>{item.timestamp}</p>}
                </div>
              </div>
            </DropdownMenuItem>
          ))
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
