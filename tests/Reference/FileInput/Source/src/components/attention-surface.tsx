import React from 'react';

import { cn } from '../utils/cn';
import { createAttentionColor } from '../utils/attention-color';

export type AttentionSurfaceElement = 'article' | 'button' | 'div' | 'li' | 'section';
export type AttentionSurfaceDensity = 'compact' | 'comfortable';

export interface AttentionSurfaceProps extends Omit<React.ComponentPropsWithoutRef<'button'>, 'color'> {
  as?: AttentionSurfaceElement;
  density?: AttentionSurfaceDensity;
  hue: number;
  intensity: number;
  interactive?: boolean;
}

export const AttentionSurface = React.forwardRef<HTMLElement, AttentionSurfaceProps>(function AttentionSurface(
  {
    as: Component = 'article',
    children,
    className,
    density = 'comfortable',
    hue,
    intensity,
    interactive,
    style,
    type,
    ...props
  },
  ref
) {
  const attention = createAttentionColor({ hue, intensity });
  const isInteractive = interactive ?? Component === 'button';

  return React.createElement(
    Component,
    {
      ...props,
      ...(Component === 'button' ? { type: type ?? 'button' } : {}),
      className: cn(
        'rounded-[11px] border text-left shadow-[0_16px_38px_-30px_rgba(0,0,0,0.92)]',
        'transition-[background-color,border-color,box-shadow,transform] duration-[var(--motion-surface)] ease-premium',
        density === 'compact' ? 'px-4 py-4' : 'px-5 py-5',
        isInteractive && 'hover:-translate-y-px hover:shadow-[0_24px_48px_-28px_rgba(0,0,0,0.9)] focus:outline-none focus:ring-2 focus:ring-sea/30',
        className
      ),
      'data-attention-intensity': attention.intensity,
      'data-attention-red-mix': attention.redMix,
      ref,
      style: { ...attention.style, ...style }
    },
    children
  );
});
