'use client';

import { Minus } from 'lucide-react';
import React from 'react';
import { createPortal } from 'react-dom';

import { cn } from '../utils/cn';
import { ModalLayerScope, useModalLayer, usePresence } from '../utils/hooks';

export interface DrawerProps {
  children: React.ReactNode;
  className?: string;
  description?: string;
  isOpen: boolean;
  onClose: () => void;
  snapPoints?: string[];
  title?: string;
}

export function Drawer({ children, className, description, isOpen, onClose, title }: DrawerProps) {
  const { present, visible } = usePresence(isOpen, 180);
  const startYRef = React.useRef<number | null>(null);
  const panelRef = useModalLayer<HTMLDivElement>(isOpen, onClose);
  const titleId = React.useId();
  const descriptionId = React.useId();

  const handleTouchStart = (e: React.TouchEvent) => {
    startYRef.current = e.touches[0].clientY;
  };
  const handleTouchEnd = (e: React.TouchEvent) => {
    if (startYRef.current === null) return;
    const delta = e.changedTouches[0].clientY - startYRef.current;
    startYRef.current = null;
    if (delta > 60) onClose();
  };

  if (typeof document === 'undefined' || !present) return null;

  return createPortal(
    <ModalLayerScope layerRef={panelRef}><div data-bleecker-modal-overlay className={cn('fixed inset-0 z-50', !isOpen && 'pointer-events-none')} aria-hidden={!isOpen || undefined}>
      {/* Backdrop */}
      <div
        className={cn(
          'absolute inset-0 bg-deep-sea/38 backdrop-blur-[2px] transition-opacity ease-premium dark:bg-black/65',
          visible ? 'duration-[var(--motion-overlay)]' : 'duration-[180ms] ease-in',
          visible ? 'opacity-100' : 'opacity-0'
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
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
        className={cn(
          'absolute inset-x-0 bottom-0 flex max-h-[90dvh] flex-col rounded-t-[var(--radius-dialog)] border-t border-sand/30 bg-white shadow-[var(--shadow-overlay)] transition-transform dark:border-white/12 dark:bg-deep-sea',
          visible ? 'duration-[var(--motion-overlay)] ease-premium' : 'duration-[180ms] ease-in',
          visible ? 'translate-y-0' : 'translate-y-full',
          className
        )}
      >
        {/* Drag handle */}
        <div className='flex items-center justify-center pt-3 pb-1'>
          <Minus size={24} className='text-sand/40 dark:text-sand/50' strokeWidth={3} aria-hidden='true' />
        </div>
        {(title || description) && (
          <div className='px-6 pb-4 pt-1'>
            {title ? <h2 id={titleId} className='text-lg font-medium text-text-primary dark:text-text-primary'>{title}</h2> : null}
            {description ? <p id={descriptionId} className='font-secondary mt-2 text-sm leading-relaxed text-text-secondary dark:text-text-secondary'>{description}</p> : null}
          </div>
        )}
        <div className='flex-1 overflow-y-auto px-6 pb-6 pt-0'>{children}</div>
      </div>
    </div></ModalLayerScope>,
    document.body
  );
}
