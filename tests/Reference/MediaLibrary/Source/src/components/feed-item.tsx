import { ExternalLink } from 'lucide-react';

import { Avatar } from './avatar';
import { Tooltip } from './tooltip';
import { cn } from '../utils/cn';

export interface FeedItemProps {
  author: string;
  avatarSrc?: string;
  categories?: string[];
  className?: string;
  content: string;
  init?: string;
  language: string;
  postedAt: Date | string;
  relevance: number;
  sourceUrl: string;
}

function formatPostedAt(postedAt: Date | string): string {
  const dateValue = postedAt instanceof Date ? postedAt : new Date(postedAt);
  if (Number.isNaN(dateValue.getTime())) {
    return 'Unknown time';
  }

  const now = new Date();
  const diffMs = now.getTime() - dateValue.getTime();
  const twelveHoursMs = 12 * 60 * 60 * 1000;

  if (diffMs >= 0 && diffMs <= twelveHoursMs) {
    return new Intl.DateTimeFormat(undefined, {
      hour: 'numeric',
      minute: '2-digit'
    }).format(dateValue);
  }

  return new Intl.DateTimeFormat(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  }).format(dateValue);
}

function formatIsoTimestamp(postedAt: Date | string): string {
  const dateValue = postedAt instanceof Date ? postedAt : new Date(postedAt);
  if (Number.isNaN(dateValue.getTime())) {
    return typeof postedAt === 'string' ? postedAt : 'Unknown timestamp';
  }

  return dateValue.toISOString();
}

function normalizeCategory(category: string): string {
  return category.trim().toLowerCase();
}

function normalizeSourceLabel(sourceUrl: string): string {
  try {
    const parsed = new URL(sourceUrl);
    return parsed.hostname;
  } catch {
    return 'Source article';
  }
}

function formatRelevance(relevance: number): string {
  return String(Math.round(relevance));
}

export function FeedItem({
  author,
  avatarSrc,
  categories = [],
  className,
  content,
  init,
  language,
  postedAt,
  relevance,
  sourceUrl
}: FeedItemProps) {
  const fallback = init || (author.trim().slice(0, 1) || '?').toUpperCase();
  const postedAtLabel = formatPostedAt(postedAt);
  const postedAtIsoLabel = formatIsoTimestamp(postedAt);
  const sourceLabel = normalizeSourceLabel(sourceUrl);
  const safeCategories = categories
    .map(normalizeCategory)
    .filter((value) => value.length > 0);

  return (
    <div
      className={cn(
        'flex gap-3.5 border-b border-sand/20 px-4 py-4 text-left transition-colors duration-[var(--motion-surface)] ease-premium hover:bg-light-sand/35 dark:border-white/[0.07] dark:hover:bg-white/[0.03]',
        className
      )}
    >
      <Avatar fallback={fallback} size='sm' src={avatarSrc} className='h-8 w-8 ring-1 ring-black/5 dark:ring-white/10' />
      <div className='min-w-0 flex-1'>
        <div className='mb-1.5 flex items-center gap-2'>
          <div className='min-w-0 flex items-center gap-2'>
            <span className='truncate text-[13px] font-semibold text-text-primary'>{author}</span>
            <Tooltip content={postedAtIsoLabel}>
              <span className='font-secondary whitespace-nowrap text-[11px] text-text-secondary'>· {postedAtLabel}</span>
            </Tooltip>
          </div>
          <span className='ml-auto rounded-[5px] border border-sea/10 bg-sea/[0.06] px-2 py-0.5 text-[11px] font-semibold tabular-nums text-sea'>{formatRelevance(relevance)}</span>
        </div>
        <p className='break-words text-[13px] leading-5 text-text-primary'>{content}</p>

        <div className='mt-2 flex flex-wrap items-center gap-1.5'>
          <span className='rounded-[5px] bg-sand/25 px-2 py-0.5 text-[11px] font-medium uppercase text-text-secondary dark:bg-white/[0.07]'>
            {language || 'n/a'}
          </span>
          {safeCategories.map((category) => (
            <span
              key={category}
              className='rounded-[5px] border border-sand/20 bg-light-sand/60 px-2 py-0.5 text-[11px] font-medium text-text-secondary dark:border-white/[0.07] dark:bg-white/[0.04]'
            >
              {category}
            </span>
          ))}
        </div>

        <div className='mt-2.5'>
          <a
            className='inline-flex items-center gap-1 text-xs font-medium text-sea transition-colors duration-[var(--motion-control)] ease-premium hover:text-deep-sea hover:underline'
            href={sourceUrl}
            rel='noreferrer noopener'
            target='_blank'
          >
            <span>{sourceLabel}</span>
            <ExternalLink size={12} />
          </a>
        </div>
      </div>
    </div>
  );
}
