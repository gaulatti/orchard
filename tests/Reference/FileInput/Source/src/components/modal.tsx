'use client';

import React from 'react';
import { createPortal } from 'react-dom';

import { DialogCloseButton } from './dialog-close-button';
import { cn } from '../utils/cn';
import { ModalLayerScope, useModalLayer, usePresence } from '../utils/hooks';

export interface ModalProps {
  children: React.ReactNode;
  className?: string;
  isOpen: boolean;
  onClose: () => void;
  title: string;
}

export function Modal({ children, className, isOpen, onClose, title }: ModalProps) {
  const { present: isPresent, visible } = usePresence(isOpen, 150);
  const dialogRef = useModalLayer<HTMLDivElement>(isOpen, onClose);
  const titleId = React.useId();
  const motionState = visible ? 'open' : 'closing';
  const requestClose = () => { if (isOpen) onClose(); };

  if (typeof document === 'undefined' || !isPresent) {
    return null;
  }

  return createPortal(
    <ModalLayerScope layerRef={dialogRef}>
      <div data-bleecker-modal-overlay className={cn('fixed inset-0 z-50 flex p-4', !isOpen && 'pointer-events-none')} aria-hidden={!isOpen || undefined}>
        <div aria-hidden='true' onClick={requestClose} className={cn('absolute inset-0 bg-deep-sea/38 backdrop-blur-[2px] transition-opacity dark:bg-black/65', visible ? 'opacity-100 duration-[var(--motion-control)]' : 'opacity-0 duration-[var(--motion-exit)]')} />
        <div
          ref={dialogRef}
          role='dialog'
          aria-modal='true'
          aria-labelledby={titleId}
          tabIndex={-1}
          data-state={motionState}
          className={cn(
            'modal-motion relative m-auto max-h-[calc(100dvh-2rem)] w-full max-w-xl overflow-auto rounded-[var(--radius-dialog)] border border-sand/30 bg-white p-0 text-text-primary shadow-[var(--shadow-overlay)] outline-none dark:border-white/12 dark:bg-deep-sea dark:text-text-primary',
            className
          )}
        >
          <div>
            <div className='flex items-center justify-between gap-4 border-b border-sand/20 px-6 py-5 dark:border-white/[0.08] md:px-7'>
              <h2 id={titleId} className='text-xl font-semibold tracking-refined text-text-primary dark:text-text-primary'>{title}</h2>
              <DialogCloseButton onClick={requestClose} aria-label='Close' />
            </div>
            <div className='px-6 py-6 md:px-7 md:py-7'>{children}</div>
          </div>
        </div>
      </div>
    </ModalLayerScope>,
    document.body
  );
}
