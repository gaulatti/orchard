'use client';

import { AlertCircle, AlertTriangle, CheckCircle, Info, X } from 'lucide-react';
import React from 'react';
import { createPortal } from 'react-dom';

import { Button, type ButtonVariant } from './button';
import { cn } from '../utils/cn';

// ─── Types ──────────────────────────────────────────────────────────────────

export type SonnerVariant = 'default' | 'success' | 'error' | 'warning' | 'info';
export type SonnerPosition = 'top-left' | 'top-center' | 'top-right' | 'bottom-left' | 'bottom-center' | 'bottom-right';
export type SonnerActionVariant = 'link' | ButtonVariant;

export interface SonnerAction {
  label: string;
  onClick: () => void;
  variant?: SonnerActionVariant;
}

export interface SonnerToast {
  id: string;
  title: string;
  description?: string;
  duration?: number;
  variant?: SonnerVariant;
  action?: SonnerAction;
}

// ─── Context / State ────────────────────────────────────────────────────────

type ToastStore = {
  toasts: SonnerToast[];
  add: (toast: Omit<SonnerToast, 'id'>) => string;
  dismiss: (id: string) => void;
};

const ToastContext = React.createContext<ToastStore | null>(null);

let externalAdd: ToastStore['add'] | null = null;
let externalDismiss: ToastStore['dismiss'] | null = null;

/** Call outside React to fire a toast — only works when <Sonner> is mounted. */
export const toast = {
  show: (t: Omit<SonnerToast, 'id'>) => externalAdd?.(t) ?? '',
  success: (title: string, rest?: Partial<Omit<SonnerToast, 'id' | 'title' | 'variant'>>) => externalAdd?.({ title, variant: 'success', ...rest }) ?? '',
  error: (title: string, rest?: Partial<Omit<SonnerToast, 'id' | 'title' | 'variant'>>) => externalAdd?.({ title, variant: 'error', ...rest }) ?? '',
  warning: (title: string, rest?: Partial<Omit<SonnerToast, 'id' | 'title' | 'variant'>>) => externalAdd?.({ title, variant: 'warning', ...rest }) ?? '',
  info: (title: string, rest?: Partial<Omit<SonnerToast, 'id' | 'title' | 'variant'>>) => externalAdd?.({ title, variant: 'info', ...rest }) ?? '',
  dismiss: (id: string) => externalDismiss?.(id)
};

// ─── Variant config ─────────────────────────────────────────────────────────

const variantConfig: Record<SonnerVariant, { icon: React.ComponentType<{ size?: number; className?: string }>; color: string }> = {
  default: { icon: Info, color: 'text-text-secondary' },
  info: { icon: Info, color: 'text-desert dark:text-accent-gold' },
  success: { icon: CheckCircle, color: 'text-sea dark:text-accent-blue' },
  warning: { icon: AlertTriangle, color: 'text-sunset dark:text-accent-gold' },
  error: { icon: AlertCircle, color: 'text-terracotta' }
};

// ─── Single toast item ───────────────────────────────────────────────────────

function ToastItem({ toast, onDismiss }: { toast: SonnerToast; onDismiss: () => void }) {
  const [visible, setVisible] = React.useState(false);
  const { icon: Icon, color } = variantConfig[toast.variant ?? 'default'];

  React.useEffect(() => {
    requestAnimationFrame(() => setVisible(true));
    if (!toast.duration || toast.duration <= 0) return;
    const t = setTimeout(() => {
      setVisible(false);
      setTimeout(onDismiss, 300);
    }, toast.duration);
    return () => clearTimeout(t);
  }, []);

  return (
    <div
      className={cn(
        'flex w-full items-start gap-3 rounded-[var(--radius-card)] border border-sand/25 bg-white p-4 shadow-[var(--shadow-overlay)] transition-[opacity,transform] duration-200 dark:border-white/10 dark:bg-deep-sea',
        visible ? 'translate-y-0 opacity-100' : 'translate-y-2 opacity-0'
      )}
      role='alert'
    >
      <Icon size={18} className={cn('mt-0.5 flex-shrink-0', color)} />
      <div className='min-w-0 flex-1'>
        <p className='text-sm font-medium text-text-primary dark:text-text-primary'>{toast.title}</p>
        {toast.description ? <p className='mt-0.5 text-xs text-text-secondary dark:text-text-secondary'>{toast.description}</p> : null}
        {toast.action?.variant === 'link' ? (
          <button
            type='button'
            onClick={toast.action.onClick}
            className='mt-2 text-xs font-medium text-sea underline underline-offset-2 transition-colors duration-[var(--motion-control)] ease-premium hover:text-deep-sea dark:text-accent-blue dark:hover:text-text-primary'
          >
            {toast.action.label}
          </button>
        ) : toast.action ? (
          <Button variant={toast.action.variant as ButtonVariant | undefined} size='sm' className='mt-2 text-xs' onClick={toast.action.onClick}>
            {toast.action.label}
          </Button>
        ) : null}
      </div>
      <button
        type='button'
        onClick={() => {
          setVisible(false);
          setTimeout(onDismiss, 300);
        }}
        className='flex-shrink-0 scale-100 rounded-md p-0.5 text-text-secondary transition-[background-color,color,transform] duration-[var(--motion-control)] ease-premium hover:bg-sand/10 hover:text-text-primary active:scale-[0.9] dark:hover:bg-sand/20'
        aria-label='Dismiss'
      >
        <X size={14} />
      </button>
    </div>
  );
}

// ─── Position classes ────────────────────────────────────────────────────────

const positionClasses: Record<SonnerPosition, string> = {
  'top-left': 'top-4 left-4 items-start',
  'top-center': 'top-4 left-1/2 -translate-x-1/2 items-center',
  'top-right': 'top-4 right-4 items-end',
  'bottom-left': 'bottom-4 left-4 items-start',
  'bottom-center': 'bottom-4 left-1/2 -translate-x-1/2 items-center',
  'bottom-right': 'bottom-4 right-4 items-end'
};

// ─── Sonner container ────────────────────────────────────────────────────────

export interface SonnerProps {
  position?: SonnerPosition;
  /** Default duration in ms (0 = persistent) */
  duration?: number;
}

export function Sonner({ duration = 4000, position = 'bottom-right' }: SonnerProps) {
  const [toasts, setToasts] = React.useState<SonnerToast[]>([]);

  const add = React.useCallback<ToastStore['add']>(
    (t) => {
      const id = Math.random().toString(36).slice(2);
      setToasts((prev) => [...prev, { duration, ...t, id }]);
      return id;
    },
    [duration]
  );

  const dismiss = React.useCallback<ToastStore['dismiss']>((id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  // Wire up external API
  React.useEffect(() => {
    externalAdd = add;
    externalDismiss = dismiss;
    return () => {
      externalAdd = null;
      externalDismiss = null;
    };
  }, [add, dismiss]);

  if (typeof document === 'undefined') return null;

  return createPortal(
    <div
      aria-live='polite'
      aria-label='Notifications'
      className={cn('pointer-events-none fixed z-[9999] flex flex-col gap-2 p-4', positionClasses[position])}
      style={{ maxWidth: '22rem', width: '100%' }}
    >
      {toasts.map((t) => (
        <div key={t.id} className='pointer-events-auto w-full'>
          <ToastItem toast={t} onDismiss={() => dismiss(t.id)} />
        </div>
      ))}
    </div>,
    document.body
  );
}
