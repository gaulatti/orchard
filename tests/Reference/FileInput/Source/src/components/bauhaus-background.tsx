import React from 'react';

import { cn } from '../utils/cn';

export type BauhausBackgroundType = 'nazca' | 'autostrada' | 'pompeii' | 'monitor';

interface BauhausPatternConfig {
  bgClassName: string;
  pattern: 'circles' | 'lines' | 'squares';
}

const patternConfig: Record<BauhausBackgroundType, BauhausPatternConfig> = {
  nazca: {
    bgClassName: 'bg-accent-bronze',
    pattern: 'circles'
  },
  autostrada: {
    bgClassName: 'bg-accent-oxblood',
    pattern: 'lines'
  },
  pompeii: {
    bgClassName: 'bg-accent-bronze',
    pattern: 'squares'
  },
  monitor: {
    bgClassName: 'bg-accent-oxblood',
    pattern: 'circles'
  }
};

export interface BauhausBackgroundProps {
  className?: string;
  defaultImageUrl?: string;
  imageUrl?: string;
  type: BauhausBackgroundType;
}

export function BauhausBackground({ className, defaultImageUrl = '/hero/pompeii.avif', imageUrl, type }: BauhausBackgroundProps) {
  const { bgClassName, pattern } = patternConfig[type];
  const resolvedImageUrl = imageUrl ?? defaultImageUrl;

  return (
    <div className={cn('relative h-full w-full overflow-hidden', className)}>
      <div className='absolute inset-0'>
        <img src={resolvedImageUrl} alt='' className='h-full w-full object-cover' />
        <div className='absolute inset-0 bg-gradient-to-t from-black/30 to-transparent mix-blend-multiply' />
      </div>

      <div className={cn('absolute inset-0 mix-blend-soft-light', bgClassName)}>
        {pattern === 'circles' ? (
          <div className='absolute inset-0'>
            <div className='shape-rotate absolute left-1/4 top-1/4 h-1/2 w-1/2 rounded-full border-8 border-white' />
            <div className='shape-rotate absolute left-1/3 top-1/3 h-1/3 w-1/3 rounded-full border-4 border-white [animation-direction:reverse]' />
          </div>
        ) : null}

        {pattern === 'lines' ? (
          <div className='absolute inset-0'>
            <div className='grid h-full grid-cols-6'>
              {Array.from({ length: 6 }).map((_, index) => (
                <div
                  key={`line-${index}`}
                  className='h-full border-l-2 border-white/85'
                  style={{ transitionDelay: `${index * 100}ms` }}
                />
              ))}
            </div>
          </div>
        ) : null}

        {pattern === 'squares' ? (
          <div className='absolute inset-0'>
            <div className='grid h-full grid-cols-3 grid-rows-3 gap-4 p-4'>
              {Array.from({ length: 9 }).map((_, index) => (
                <div key={`square-${index}`} className='border-2 border-white/85' />
              ))}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}
