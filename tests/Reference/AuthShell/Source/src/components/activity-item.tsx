import { Avatar } from './avatar';
import { cn } from '../utils/cn';

export interface ActivityItemProps {
  className?: string;
  init: string;
  time: string;
  title: string;
}

export function ActivityItem({ className, init, time, title }: ActivityItemProps) {
  return (
    <div
      className={cn(
        'flex cursor-pointer gap-3.5 border-b border-sand/20 px-4 py-4 transition-colors duration-[var(--motion-surface)] ease-premium hover:bg-light-sand/35 dark:border-white/[0.07] dark:hover:bg-white/[0.03]',
        className
      )}
    >
      <Avatar fallback={init} size='sm' />
      <div className='min-w-0 flex-1 space-y-1.5'>
        <p className='text-[13px] font-medium leading-5 text-text-primary'>{title}</p>
        <p className='font-secondary text-[11px] text-text-secondary'>{time}</p>
      </div>
    </div>
  );
}
