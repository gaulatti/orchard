'use client';

import { Copy, Trash2 } from 'lucide-react';
import React from 'react';

import { IconButton } from './icon-button';
import { Modal } from './modal';
import { SearchInput } from './search-input';
import { cn } from '../utils/cn';

export interface MediaLibraryItem {
  alt?: string;
  createdAt?: string;
  filename: string;
  id: string;
  thumbnailUrl?: string;
  url?: string;
}

export interface MediaLibraryProps {
  actions?: React.ReactNode;
  className?: string;
  emptyMessage?: string;
  filterSlot?: React.ReactNode;
  items: MediaLibraryItem[];
  onCopy?: (text: string) => void;
  onDelete?: (id: string) => void;
  onSearchTermChange: (value: string) => void;
  searchPlaceholder?: string;
  searchTerm: string;
  title: string;
}

export function MediaLibrary({
  actions,
  className,
  emptyMessage = 'No media found.',
  filterSlot,
  items,
  onCopy,
  onDelete,
  onSearchTermChange,
  searchPlaceholder = 'Search media...',
  searchTerm,
  title
}: MediaLibraryProps) {
  const [selectedMedia, setSelectedMedia] = React.useState<MediaLibraryItem | null>(null);

  const handleCopy = (text: string) => {
    if (onCopy) {
      onCopy(text);
      return;
    }

    if (typeof navigator !== 'undefined') {
      void navigator.clipboard.writeText(text);
    }
  };

  return (
    <div className={cn('px-5 py-6 md:px-8 md:py-9', className)}>
      <div className='mb-8 flex flex-col gap-5 md:flex-row md:items-end md:justify-between'>
        <div>
          <p className='mb-2 text-[10px] font-semibold uppercase tracking-[0.11em] text-desert'>Media collection</p>
          <h1 className='text-[2rem] font-semibold leading-tight tracking-refined text-text-primary dark:text-text-primary'>{title}</h1>
        </div>
        {actions}
      </div>

      <div className='mb-7 flex flex-col gap-3 md:flex-row'>
        <div className='flex-1'>
          <SearchInput
            placeholder={searchPlaceholder}
            value={searchTerm}
            onChange={(event) => onSearchTermChange(event.target.value)}
            onClear={() => onSearchTermChange('')}
          />
        </div>

        {filterSlot ? <div className='md:w-64'>{filterSlot}</div> : null}
      </div>

      <div className='grid grid-cols-2 gap-4 md:grid-cols-4 lg:grid-cols-5'>
        {items.map((media) => (
          <div
            key={media.id}
            className='group relative aspect-square overflow-hidden rounded-[var(--radius-ui)] border border-sand/30 bg-white shadow-[var(--shadow-surface)] transition-[border-color,box-shadow] duration-[var(--motion-surface)] ease-premium hover:border-sand/55 hover:shadow-[var(--shadow-raised)] focus-within:ring-2 focus-within:ring-sea/25 dark:border-white/10 dark:bg-deep-sea dark:hover:border-white/20'
          >
            <button
              type='button'
              className='absolute inset-0 z-10 cursor-pointer outline-none'
              onClick={() => setSelectedMedia(media)}
              aria-label={`Open ${media.filename}`}
            />
            {media.thumbnailUrl || media.url ? (
              <img src={media.thumbnailUrl || media.url} alt={media.alt || media.filename} className='h-full w-full object-cover' />
            ) : (
              <div className='font-secondary flex h-full w-full items-center justify-center bg-light-sand/60 text-xs text-text-secondary dark:bg-white/[0.04]'>No preview</div>
            )}

            <div className='pointer-events-none absolute inset-x-0 bottom-0 flex translate-y-1 flex-col justify-end bg-deep-sea/82 p-3 opacity-0 backdrop-blur-[2px] transition-[opacity,transform] duration-[var(--motion-surface)] ease-premium group-hover:translate-y-0 group-hover:opacity-100 group-focus-within:translate-y-0 group-focus-within:opacity-100'>
              <p className='mb-2 truncate text-[12px] font-medium text-white'>{media.filename}</p>
              {onDelete ? (
                <IconButton
                  onClick={(event) => {
                    event.stopPropagation();
                    onDelete(media.id);
                  }}
                  variant='subtle'
                  className='pointer-events-auto relative z-20 self-end text-terracotta hover:text-terracotta'
                  title='Delete'
                  aria-label='Delete media item'
                >
                  <Trash2 size={16} />
                </IconButton>
              ) : null}
            </div>
          </div>
        ))}
      </div>

      {items.length === 0 ? <div className='font-secondary rounded-[var(--radius-card)] border border-sand/25 bg-light-sand/25 px-8 py-14 text-center text-sm text-text-secondary dark:border-white/10 dark:bg-white/[0.025]'>{emptyMessage}</div> : null}

      <Modal isOpen={!!selectedMedia} onClose={() => setSelectedMedia(null)} title={selectedMedia?.filename || 'Media Preview'} className='max-w-4xl'>
        {selectedMedia ? (
          <div className='space-y-6'>
            <div className='flex items-center justify-center overflow-hidden rounded-[var(--radius-ui)] border border-sand/20 bg-light-sand/35 dark:border-white/[0.07] dark:bg-black/20'>
              {selectedMedia.url ? (
                <img src={selectedMedia.url} alt={selectedMedia.alt || selectedMedia.filename} className='max-h-[60vh] w-auto object-contain' />
              ) : null}
            </div>

            <div className='space-y-5'>
              <div className='flex items-center gap-2'>
                <h3 className='flex-1 text-xl font-semibold tracking-refined text-text-primary dark:text-text-primary'>{selectedMedia.filename}</h3>
                <IconButton
                  onClick={() => handleCopy(selectedMedia.filename)}
                  variant='ghost'
                  title='Copy filename'
                  aria-label='Copy filename'
                >
                  <Copy size={18} />
                </IconButton>
              </div>

              {selectedMedia.alt ? (
                <div>
                  <p className='text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>Alt text</p>
                  <p className='font-secondary mt-1 text-sm leading-6 text-text-primary dark:text-text-primary'>{selectedMedia.alt}</p>
                </div>
              ) : null}

              <div className='grid grid-cols-1 gap-6 border-t border-sand/20 pt-5 md:grid-cols-2 dark:border-white/[0.07]'>
                {selectedMedia.url ? (
                  <div>
                    <p className='mb-1.5 text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>URL</p>
                    <div className='flex items-center gap-2'>
                      <a
                        href={selectedMedia.url}
                        target='_blank'
                        rel='noreferrer noopener'
                        className='flex-1 break-all text-sm text-sea transition-colors duration-[var(--motion-control)] ease-premium hover:text-deep-sea hover:underline dark:text-accent-blue dark:hover:text-text-primary'
                      >
                        {selectedMedia.url}
                      </a>
                      <IconButton
                        onClick={() => handleCopy(selectedMedia.url || '')}
                        variant='ghost'
                        size='sm'
                        title='Copy URL'
                        aria-label='Copy URL'
                      >
                        <Copy size={14} />
                      </IconButton>
                    </div>
                  </div>
                ) : null}

                {selectedMedia.createdAt ? (
                  <div>
                    <p className='mb-1.5 text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>Created</p>
                    <p className='font-secondary text-sm text-text-primary dark:text-text-primary'>{new Date(selectedMedia.createdAt).toLocaleDateString()}</p>
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        ) : null}
      </Modal>
    </div>
  );
}
