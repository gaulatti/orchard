'use client';

import React from 'react';
import { createPortal } from 'react-dom';

import { DialogCloseButton } from './dialog-close-button';
import { cn } from '../utils/cn';
import { ModalLayerScope, useModalLayer, usePresence } from '../utils/hooks';

export type SheetSide = 'left' | 'right' | 'top' | 'bottom';

export interface SheetProps {
  children: React.ReactNode;
  className?: string;
  isOpen: boolean;
  onClose: () => void;
  scrollContent?: boolean;
  side?: SheetSide;
  title?: string;
  description?: string;
}

const sideStyles: Record<SheetSide, { panel: string; enter: string; leave: string }> = {
  right: {
    panel: 'inset-y-0 right-0 h-full w-full max-w-sm',
    enter: 'translate-x-0',
    leave: 'translate-x-full'
  },
  left: {
    panel: 'inset-y-0 left-0 h-full w-full max-w-sm',
    enter: 'translate-x-0',
    leave: '-translate-x-full'
  },
  top: {
    panel: 'inset-x-0 top-0 w-full',
    enter: 'translate-y-0',
    leave: '-translate-y-full'
  },
  bottom: {
    panel: 'inset-x-0 bottom-0 w-full',
    enter: 'translate-y-0',
    leave: 'translate-y-full'
  }
};

export function Sheet({ children, className, description, isOpen, onClose, scrollContent = true, side = 'right', title }: SheetProps) {
  const { present, visible } = usePresence(isOpen, 180);
  const { panel, enter, leave } = sideStyles[side];
  const titleId = React.useId();
  const descriptionId = React.useId();
  const panelRef = useModalLayer<HTMLDivElement>(isOpen, onClose);

  if (typeof document === 'undefined' || !present) return null;

  return createPortal(
    <ModalLayerScope layerRef={panelRef}><div data-bleecker-modal-overlay className={cn('fixed inset-0 z-50 flex', !isOpen && 'pointer-events-none')} aria-hidden={!isOpen || undefined}>
      {/* Backdrop */}
      <div
        className={cn(
          'absolute inset-0 bg-deep-sea/38 backdrop-blur-[2px] transition-opacity dark:bg-black/65',
          visible ? 'opacity-100 duration-[var(--motion-overlay)] ease-premium' : 'opacity-0 duration-[180ms] ease-in'
        )}
        onClick={onClose}
        aria-hidden='true'
      />
      {/* Panel */}
      <div
        ref={panelRef}
        role='dialog'
        aria-modal='true'
        aria-label={title ? undefined : 'Dialog'}
        aria-labelledby={title ? titleId : undefined}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
        className={cn(
          'absolute flex flex-col overflow-hidden border border-sand/30 bg-white text-text-primary shadow-[var(--shadow-overlay)] outline-none transition-transform dark:border-white/12 dark:bg-deep-sea dark:text-text-primary',
          panel,
          visible ? `${enter} duration-[var(--motion-overlay)] ease-premium` : `${leave} duration-[180ms] ease-in`,
          (side === 'left' || side === 'right') && 'rounded-none',
          side === 'top' && 'rounded-b-[var(--radius-dialog)]',
          side === 'bottom' && 'rounded-t-[var(--radius-dialog)]',
          className
        )}
      >
        <div className='flex h-full min-h-0 flex-1 flex-col overflow-hidden p-6 md:p-8'>
          {title || description ? (
            <div className='mb-5 flex shrink-0 items-start justify-between gap-4'>
              <div className='min-w-0 flex-1'>
                {title ? <h2 id={titleId} className='text-2xl font-medium text-text-primary dark:text-text-primary'>{title}</h2> : null}
                {description ? <p id={descriptionId} className='font-secondary mt-2 text-sm leading-relaxed text-text-secondary dark:text-text-secondary'>{description}</p> : null}
              </div>
              <DialogCloseButton onClick={onClose} aria-label='Close' />
            </div>
          ) : (
            <div className='mb-5 flex shrink-0 justify-end'>
              <DialogCloseButton onClick={onClose} aria-label='Close' />
            </div>
          )}
          <div className={cn(scrollContent ? 'min-h-0 flex-1 overflow-y-auto' : 'flex min-h-0 flex-1 flex-col overflow-hidden')}>{children}</div>
        </div>
      </div>
    </div></ModalLayerScope>,
    document.body
  );
}
