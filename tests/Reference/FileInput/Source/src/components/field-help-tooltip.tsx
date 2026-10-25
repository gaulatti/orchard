import { Info } from 'lucide-react';

import { Tooltip } from './tooltip';
import { cn } from '../utils/cn';

export interface FieldHelpTooltipProps {
  className?: string;
  text: string;
}

export function FieldHelpTooltip({ className, text }: FieldHelpTooltipProps) {
  if (!text.trim()) {
    return null;
  }

  return (
    <Tooltip content={text} side='bottom' className={cn('align-middle', className)}>
      <button
        type='button'
        tabIndex={0}
        aria-label={text}
        className='inline-flex h-4 w-4 scale-100 items-center justify-center rounded-full text-text-secondary transition-[color,transform] duration-[var(--motion-control)] ease-premium hover:text-text-primary active:scale-[0.9] focus:outline-none focus:ring-2 focus:ring-sea dark:focus:ring-accent-blue'
      >
        <Info size={14} />
      </button>
    </Tooltip>
  );
}
