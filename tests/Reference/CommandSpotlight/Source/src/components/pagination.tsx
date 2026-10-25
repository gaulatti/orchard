import { ChevronLeft, ChevronRight } from 'lucide-react';

import { cn } from '../utils/cn';

export interface PaginationProps {
  className?: string;
  currentPage: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
  onPageChange: (page: number) => void;
  totalPages: number;
}

export function Pagination({ className, currentPage, hasNextPage, hasPrevPage, onPageChange, totalPages }: PaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  const visiblePages = Array.from({ length: Math.min(totalPages, 7) }, (_, index) => {
    if (totalPages <= 7) {
      return index + 1;
    }

    if (currentPage <= 4) {
      return index + 1;
    }

    if (currentPage >= totalPages - 3) {
      return totalPages - 6 + index;
    }

    return currentPage - 3 + index;
  });

  return (
    <nav aria-label='Pagination' className={cn('mt-8 flex items-center justify-center gap-1.5', className)}>
      <button
        type='button'
        onClick={() => onPageChange(currentPage - 1)}
        disabled={!hasPrevPage}
        className='flex h-9 w-9 scale-100 items-center justify-center rounded-[var(--radius-button)] border border-sand/30 bg-white transition-[background-color,border-color,color,transform,opacity] duration-[var(--motion-control)] ease-premium hover:border-sand/60 hover:bg-light-sand/50 active:scale-[0.96] disabled:cursor-not-allowed disabled:transform-none disabled:opacity-40 dark:border-white/15 dark:bg-deep-sea dark:hover:bg-white/[0.06]'
        title='Previous page'
      >
        <ChevronLeft size={20} />
      </button>

      <div className='flex items-center gap-1'>
        {visiblePages.map((pageNumber) => (
          <button
            key={pageNumber}
            type='button'
            onClick={() => onPageChange(pageNumber)}
            className={cn(
              'flex h-9 min-w-9 scale-100 items-center justify-center rounded-[var(--radius-button)] border px-2.5 text-[13px] font-medium tabular-nums transition-[background-color,border-color,color,transform] duration-[var(--motion-control)] ease-premium active:scale-[0.96]',
              pageNumber === currentPage
                ? 'border-sea bg-sea text-white dark:border-accent-blue dark:bg-accent-blue dark:text-deep-sea'
                : 'border-transparent bg-white text-text-secondary hover:border-sand/30 hover:bg-light-sand/50 hover:text-text-primary dark:bg-deep-sea dark:text-text-secondary dark:hover:border-white/15 dark:hover:bg-white/[0.06]'
            )}
          >
            {pageNumber}
          </button>
        ))}
      </div>

      <button
        type='button'
        onClick={() => onPageChange(currentPage + 1)}
        disabled={!hasNextPage}
        className='flex h-9 w-9 scale-100 items-center justify-center rounded-[var(--radius-button)] border border-sand/30 bg-white transition-[background-color,border-color,color,transform,opacity] duration-[var(--motion-control)] ease-premium hover:border-sand/60 hover:bg-light-sand/50 active:scale-[0.96] disabled:cursor-not-allowed disabled:transform-none disabled:opacity-40 dark:border-white/15 dark:bg-deep-sea dark:hover:bg-white/[0.06]'
        title='Next page'
      >
        <ChevronRight size={20} />
      </button>
    </nav>
  );
}
