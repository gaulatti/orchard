import * as React from 'react';

import { cn } from '../utils/cn';

export type EyebrowTone = 'accent' | 'muted' | 'inverse';

export interface EyebrowProps extends React.HTMLAttributes<HTMLElement> {
  as?: 'div' | 'p' | 'span';
  rule?: boolean;
  tone?: EyebrowTone;
}

const toneClasses: Record<EyebrowTone, string> = {
  accent: 'text-accent-text',
  muted: 'text-text-secondary',
  inverse: 'text-white/75'
};

export function Eyebrow({ as: Component = 'p', children, className, rule = false, tone = 'accent', ...props }: EyebrowProps) {
  return (
    <Component
      className={cn(
        'inline-flex items-center gap-3 text-[11px] font-semibold uppercase leading-4 tracking-[0.12em]',
        toneClasses[tone],
        className
      )}
      {...props}
    >
      {rule ? <span aria-hidden='true' className='h-px w-7 shrink-0 bg-current opacity-60' /> : null}
      {children}
    </Component>
  );
}
