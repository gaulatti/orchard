import * as React from 'react';
import { TrendingDown, TrendingUp, Minus } from 'lucide-react';

import { cn } from '../utils/cn';
import { Card } from './card';
import { Sparkline } from './sparkline';

export type StatTrendDirection = 'up' | 'down' | 'neutral';

export interface StatCardProps {
  title: string;
  value: React.ReactNode;
  description?: string;
  trend?: {
    value: string;
    direction: StatTrendDirection;
    label?: string;
  };
  icon?: React.ReactNode;
  className?: string;
  sparklineData?: Record<string, unknown>[];
  sparklineDataKey?: string;
  sparklineColor?: string;
  footer?: React.ReactNode;
}

const trendIcons: Record<StatTrendDirection, React.ComponentType<{ className?: string; size?: number }>> = {
  up: TrendingUp,
  down: TrendingDown,
  neutral: Minus
};

const trendClasses: Record<StatTrendDirection, string> = {
  up: 'text-emerald-600 dark:text-emerald-400',
  down: 'text-terracotta dark:text-terracotta',
  neutral: 'text-text-secondary dark:text-text-secondary'
};

export function StatCard({
  title,
  value,
  description,
  trend,
  icon,
  className,
  sparklineData,
  sparklineDataKey,
  sparklineColor,
  footer
}: StatCardProps) {
  const TrendIcon = trend ? trendIcons[trend.direction] : null;

  return (
    <Card className={cn('flex flex-col gap-5', className)}>
      <div className='flex items-start justify-between gap-6'>
        <div className='min-w-0 flex-1 space-y-2'>
          <p className='text-[11px] font-semibold uppercase tracking-[0.09em] text-text-secondary dark:text-text-secondary'>{title}</p>
          <p className='text-[1.875rem] font-semibold leading-none tracking-refined text-text-primary dark:text-text-primary'>{value}</p>
          {trend && (
            <div className='flex items-center gap-2 text-sm'>
              {TrendIcon && <TrendIcon size={14} className={cn('flex-shrink-0', trendClasses[trend.direction])} />}
              <span className={cn('font-medium', trendClasses[trend.direction])}>{trend.value}</span>
              {trend.label && <span className='text-text-secondary dark:text-text-secondary'>{trend.label}</span>}
            </div>
          )}
          {description && !trend && <p className='text-sm text-text-secondary dark:text-text-secondary'>{description}</p>}
        </div>
        {icon && (
          <div className='flex h-10 w-10 shrink-0 items-center justify-center rounded-[9px] border border-sea/12 bg-sea/[0.045] text-sea dark:border-accent-blue/18 dark:bg-accent-blue/[0.08] dark:text-accent-blue'>
            {icon}
          </div>
        )}
      </div>

      {sparklineData && sparklineDataKey && (
        <div className='-mx-1'>
          <Sparkline data={sparklineData} dataKey={sparklineDataKey} height={48} color={sparklineColor} showArea />
        </div>
      )}

      {footer && <div className='border-t border-sand/20 pt-4 dark:border-white/10'>{footer}</div>}
    </Card>
  );
}
