import { LoaderCircle } from 'lucide-react';
import React from 'react';

import type { ButtonContract, ButtonSize, ButtonVariant } from '../core';
import { cn } from '../utils/cn';

export type { ButtonSize, ButtonVariant } from '../core';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement>, ButtonContract {
  as?: 'button' | 'a';
  href?: string;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    'border-sea bg-sea text-white shadow-[0_1px_2px_rgba(21,48,66,0.16)] hover:border-deep-sea hover:bg-deep-sea hover:shadow-[0_4px_12px_-7px_rgba(26,55,77,0.42)] dark:border-accent-blue dark:bg-accent-blue dark:text-deep-sea dark:hover:border-accent-blue dark:hover:bg-accent-blue/90',
  secondary:
    'border-sand/45 bg-white text-text-primary shadow-[0_1px_2px_rgba(26,55,77,0.035)] hover:border-sand/75 hover:bg-light-sand/35 hover:shadow-[0_3px_10px_-7px_rgba(26,55,77,0.26)] dark:border-white/18 dark:bg-deep-sea dark:text-text-primary dark:hover:bg-white/[0.06]',
  outline:
    'border-sea/45 bg-transparent text-sea hover:border-sea/70 hover:bg-sea/[0.045] dark:border-accent-blue/45 dark:text-accent-blue dark:hover:border-accent-blue/70 dark:hover:bg-accent-blue/[0.07]',
  subtle:
    'border-transparent bg-light-sand/75 text-deep-sea hover:bg-sand/25 dark:bg-white/[0.07] dark:text-text-primary dark:hover:bg-white/[0.11]',
  ghost:
    'border-transparent bg-transparent text-text-primary hover:bg-sand/12 dark:text-text-primary dark:hover:bg-white/[0.06]',
  link:
    'h-auto border-transparent bg-transparent px-0 text-sea underline-offset-4 hover:text-deep-sea hover:underline active:translate-y-0 active:scale-100 dark:text-accent-blue dark:hover:text-light-sand',
  destructive:
    'border-terracotta bg-terracotta text-white shadow-[0_1px_2px_rgba(133,71,47,0.14)] hover:border-terracotta/90 hover:bg-terracotta/90 hover:shadow-[0_4px_12px_-7px_rgba(133,71,47,0.38)]'
};

const sizeClasses: Record<ButtonSize, string> = {
  xs: 'h-7 gap-1.5 px-2.5 text-xs',
  sm: 'h-9 px-3.5 text-[13px]',
  md: 'h-10 px-[18px] text-sm',
  lg: 'h-12 px-6 text-[15px]'
};

export const Button = React.forwardRef<HTMLButtonElement | HTMLAnchorElement, ButtonProps>(function Button(
  { children, className, disabled, fullWidth = false, loading = false, size = 'md', type = 'button', variant = 'primary', as = 'button', href, ...props },
  ref
) {
  const classes = cn(
    'inline-flex shrink-0 translate-y-0 scale-100 items-center justify-center gap-2 whitespace-nowrap rounded-[var(--radius-button)] border font-medium tracking-ui transition-[background-color,border-color,color,box-shadow,transform,opacity] duration-[var(--motion-control)] ease-premium outline-none active:translate-y-px active:scale-[0.99] active:shadow-none focus-visible:ring-2 focus-visible:ring-sea/35 focus-visible:ring-offset-2 focus-visible:ring-offset-white disabled:pointer-events-none disabled:transform-none disabled:opacity-45 dark:focus-visible:ring-accent-blue/45 dark:focus-visible:ring-offset-deep-sea',
    variantClasses[variant],
    variant !== 'link' && sizeClasses[size],
    fullWidth && 'w-full',
    className
  );

  if (as === 'a' || href) {
    return (
      <a
        ref={ref as React.Ref<HTMLAnchorElement>}
        href={href}
        aria-busy={loading || undefined}
        aria-disabled={disabled || loading || undefined}
        className={classes}
        {...(props as React.AnchorHTMLAttributes<HTMLAnchorElement>)}
      >
        {loading ? <LoaderCircle className='size-4 animate-spin' aria-hidden='true' /> : null}
        {children}
      </a>
    );
  }

  return (
    <button
      ref={ref as React.Ref<HTMLButtonElement>}
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={classes}
      {...props}
    >
      {loading ? <LoaderCircle className='size-4 animate-spin' aria-hidden='true' /> : null}
      {children}
    </button>
  );
});
