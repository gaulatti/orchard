'use client';

import React from 'react';

import type { AvatarSize } from '../core';
import { cn } from '../utils/cn';

export type { AvatarSize } from '../core';

export interface AvatarProps {
  alt?: string;
  className?: string;
  fallback?: string;
  size?: AvatarSize;
  src?: string;
}

const sizeClasses: Record<AvatarSize, string> = {
  xs: 'h-6 w-6 text-[10px]',
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-12 w-12 text-base',
  xl: 'h-16 w-16 text-lg'
};

function getInitials(fallback: string): string {
  return fallback
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? '')
    .join('');
}

export function Avatar({ alt, className, fallback, size = 'md', src }: AvatarProps) {
  const [imgError, setImgError] = React.useState(false);
  const showFallback = !src || imgError;
  const initials = fallback ? getInitials(fallback) : null;

  return (
    <span
      className={cn(
        'relative inline-flex flex-shrink-0 items-center justify-center overflow-hidden rounded-full ring-1 ring-inset ring-black/10 bg-light-sand dark:ring-white/10 dark:bg-deep-sea',
        sizeClasses[size],
        className
      )}
    >
      {!showFallback && <img src={src} alt={alt ?? fallback ?? ''} className='h-full w-full object-cover' onError={() => setImgError(true)} />}
      {showFallback && initials ? <span className='select-none font-medium text-text-secondary dark:text-text-primary'>{initials}</span> : null}
      {showFallback && !initials ? (
        <svg className='h-3/5 w-3/5 text-text-secondary dark:text-text-primary' viewBox='0 0 24 24' fill='currentColor' aria-hidden='true'>
          <path d='M7.5 6a4.5 4.5 0 1 1 9 0 4.5 4.5 0 0 1-9 0ZM3.751 20.105a8.25 8.25 0 0 1 16.498 0 .75.75 0 0 1-.437.695A18.683 18.683 0 0 1 12 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 0 1-.437-.695Z' />
        </svg>
      ) : null}
    </span>
  );
}

export interface AvatarGroupProps {
  avatars: AvatarProps[];
  className?: string;
  max?: number;
  size?: AvatarSize;
}

export function AvatarGroup({ avatars, className, max = 4, size = 'md' }: AvatarGroupProps) {
  const visible = avatars.slice(0, max);
  const overflow = avatars.length - max;

  const overflowSizeClasses: Record<AvatarSize, string> = {
    xs: 'h-6 w-6 text-[10px]',
    sm: 'h-8 w-8 text-xs',
    md: 'h-10 w-10 text-xs',
    lg: 'h-12 w-12 text-sm',
    xl: 'h-16 w-16 text-base'
  };

  return (
    <div className={cn('flex -space-x-2', className)}>
      {visible.map((avatar, index) => (
        <Avatar key={index} {...avatar} size={size} className='ring-2 ring-white dark:ring-deep-sea' />
      ))}
      {overflow > 0 && (
        <span
          className={cn(
            'inline-flex flex-shrink-0 items-center justify-center rounded-full bg-light-sand font-medium text-text-secondary ring-2 ring-white dark:bg-deep-sea dark:text-text-primary dark:ring-deep-sea shadow-sm',
            overflowSizeClasses[size]
          )}
        >
          +{overflow}
        </span>
      )}
    </div>
  );
}
