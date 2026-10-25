'use client';

import { ChevronLeft, ChevronRight } from 'lucide-react';
import React from 'react';

import { IconButton } from './icon-button';
import { cn } from '../utils/cn';

export type HeroCarouselItemKind = 'post' | 'project';

export interface HeroCarouselItem {
  ctaLabel?: string;
  description: string;
  external?: boolean;
  href: string;
  id?: string;
  imageUrl?: string;
  kind?: HeroCarouselItemKind;
  tags?: string[];
  title: string;
}

export interface HeroCarouselRenderLinkProps {
  ariaLabel: string;
  children: React.ReactNode;
  className?: string;
  item: HeroCarouselItem;
}

export type HeroCarouselRenderLink = (props: HeroCarouselRenderLinkProps) => React.ReactNode;

export interface HeroCarouselProps {
  autoAdvanceMs?: number;
  className?: string;
  defaultBackgroundClassName?: string;
  emptyState?: React.ReactNode;
  items: HeroCarouselItem[];
  renderLink?: HeroCarouselRenderLink;
  showControls?: boolean;
  showIndicators?: boolean;
}

const defaultBackgroundClassName = 'bg-gradient-to-r from-deep-sea via-dusk to-accent-oxblood';

function defaultRenderLink({ ariaLabel, children, className, item }: HeroCarouselRenderLinkProps) {
  return (
    <a
      href={item.href}
      aria-label={ariaLabel}
      className={className}
      rel={item.external ? 'noreferrer noopener' : undefined}
      target={item.external ? '_blank' : undefined}
    >
      {children}
    </a>
  );
}

function getAriaLabel(item: HeroCarouselItem) {
  const kind = item.kind ?? 'project';
  if (kind === 'post') {
    return `Read article: ${item.title}`;
  }

  return `View project: ${item.title}`;
}

function getKicker(item: HeroCarouselItem) {
  if (item.kind === 'post') {
    return 'From the Blog';
  }

  return 'Featured Project';
}

function getCtaLabel(item: HeroCarouselItem) {
  if (item.ctaLabel) {
    return item.ctaLabel;
  }

  if (item.kind === 'post') {
    return 'Read Article';
  }

  return 'View Project';
}

export function HeroCarousel({
  autoAdvanceMs = 5000,
  className,
  defaultBackgroundClassName: fallbackBackgroundClassName = defaultBackgroundClassName,
  emptyState,
  items,
  renderLink,
  showControls = true,
  showIndicators = true
}: HeroCarouselProps) {
  const [currentSlide, setCurrentSlide] = React.useState(0);
  const [isPaused, setIsPaused] = React.useState(false);

  const linkRenderer = renderLink ?? defaultRenderLink;
  const hasMultipleSlides = items.length > 1;

  React.useEffect(() => {
    if (!hasMultipleSlides || isPaused) {
      return undefined;
    }

    const interval = window.setInterval(() => {
      setCurrentSlide((previous) => (previous + 1) % items.length);
    }, autoAdvanceMs);

    return () => window.clearInterval(interval);
  }, [autoAdvanceMs, hasMultipleSlides, isPaused, items.length]);

  React.useEffect(() => {
    if (!hasMultipleSlides) {
      return undefined;
    }

    const handleKeydown = (event: KeyboardEvent) => {
      if (document.activeElement?.tagName === 'INPUT' || document.activeElement?.tagName === 'TEXTAREA') {
        return;
      }

      if (event.key === 'ArrowLeft') {
        setCurrentSlide((previous) => (previous - 1 + items.length) % items.length);
      } else if (event.key === 'ArrowRight') {
        setCurrentSlide((previous) => (previous + 1) % items.length);
      }
    };

    document.addEventListener('keydown', handleKeydown);
    return () => document.removeEventListener('keydown', handleKeydown);
  }, [hasMultipleSlides, items.length]);

  if (items.length === 0) {
    return (
      <section className={cn('relative w-full overflow-hidden', className)}>
        <div className={cn('flex h-150 w-full items-center justify-center pt-16 text-white', fallbackBackgroundClassName)}>
          {emptyState ?? <p className='text-xl'>No featured content available</p>}
        </div>
      </section>
    );
  }

  const showNavigation = hasMultipleSlides && showControls;

  return (
    <section className={cn('relative w-full overflow-hidden', className)}>
      <div className='hero-carousel flex h-150 w-full' onMouseEnter={() => setIsPaused(true)} onMouseLeave={() => setIsPaused(false)}>
        {items.map((item, index) => (
          <div
            key={item.id ?? `${item.title}:${item.href}`}
            className={cn('absolute inset-0 w-full transition-opacity duration-500', index === currentSlide ? 'opacity-100' : 'opacity-0')}
            aria-hidden={index !== currentSlide}
          >
            <div className='absolute inset-0 h-full w-full'>
              {item.imageUrl ? (
                <>
                  <img
                    src={item.imageUrl}
                    alt={item.title}
                    className='h-full w-full object-cover'
                    loading={index === 0 ? 'eager' : 'lazy'}
                    fetchPriority={index === 0 ? 'high' : 'auto'}
                  />
                  <div className='absolute inset-0 bg-black/20' />
                  <div className='absolute inset-0 bg-linear-to-b from-black/70 via-black/25 to-black/85' />
                  <div className='absolute inset-0 bg-linear-to-r from-black/60 via-transparent to-black/60' />
                  <div
                    className='absolute inset-0 mix-blend-soft-light opacity-45'
                    style={{
                      backgroundImage: 'radial-gradient(120% 90% at 50% 55%, rgba(26,55,77,0.9) 0%, rgba(193,129,77,0.45) 45%, rgba(0,0,0,0) 75%)'
                    }}
                  />
                  <div
                    className='absolute inset-0 mix-blend-overlay opacity-[0.07]'
                    style={{
                      backgroundImage:
                        'repeating-linear-gradient(90deg, rgba(255,255,255,0.28) 0px, rgba(255,255,255,0.28) 1px, transparent 1px, transparent 3px)'
                    }}
                  />
                  <div className='absolute inset-0 bg-[radial-gradient(ellipse_at_center,rgba(0,0,0,0.18)_0%,rgba(0,0,0,0.44)_55%,rgba(0,0,0,0.72)_100%)]' />
                </>
              ) : (
                <div className={cn('h-full w-full', fallbackBackgroundClassName)} />
              )}
            </div>

            <div className='relative mx-auto h-full max-w-7xl px-4 sm:px-6 lg:px-8'>
              {linkRenderer({
                item,
                ariaLabel: getAriaLabel(item),
                className: 'group flex h-full items-center pb-8 pt-16',
                children: (
                  <div className='mx-auto max-w-3xl px-4 text-center'>
                    <div className='text-shadow-lg relative z-10 px-2 sm:px-6'>
                      <div className='flex flex-col items-center opacity-75'>
                        <span className='text-sm font-light uppercase tracking-widest light:text-white'>{getKicker(item)}</span>
                        <div className='mb-6 mt-4 h-px w-12 bg-white/30' />
                      </div>

                      {item.kind === 'project' && item.tags?.length ? (
                        <div className='mb-4 space-x-2'>
                          {item.tags.map((tag) => (
                            <span
                              key={`${item.id ?? item.href}:${tag}`}
                              className='inline-block rounded-[6px] border border-white/15 bg-black/15 px-2.5 py-1 text-[12px] font-medium backdrop-blur-[2px] light:text-white'
                            >
                              {tag}
                            </span>
                          ))}
                        </div>
                      ) : null}

                      <h1 className='mb-4 text-3xl font-semibold text-balance transition-colors duration-[var(--motion-surface)] ease-premium drop-shadow-[0_2px_8px_rgba(0,0,0,0.35)] group-hover:text-white/90 light:text-white sm:text-4xl lg:text-5xl'>
                        {item.title}
                      </h1>

                      <p className='mb-6 text-base text-gray-100 transition-colors duration-[var(--motion-surface)] ease-premium drop-shadow-[0_0_10px_rgba(0,0,0,0.45)] group-hover:text-gray-200 sm:text-lg'>
                        {item.description}
                      </p>

                      <div className='inline-block rounded-[var(--radius-button)] border px-5 py-2.5 text-sm font-medium transition-colors duration-[var(--motion-surface)] ease-premium backdrop-blur-[2px] light:border-white/25 light:bg-white/92 light:text-gray-900 light:group-hover:bg-white dark:border-sand/35 dark:bg-sand/90 dark:text-text-primary dark:group-hover:bg-dark-sand/90'>
                        {getCtaLabel(item)}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        ))}

        {showNavigation ? (
          <div className='absolute bottom-8 left-4 right-4 flex items-center justify-between'>
            <IconButton
              className='border-white/15 bg-black/20 text-white/80 shadow-none backdrop-blur-[2px] hover:border-white/25 hover:bg-black/30 hover:text-white dark:bg-black/20 dark:hover:bg-black/30'
              data-carousel-prev
              aria-label='Previous slide'
              onClick={() => setCurrentSlide((previous) => (previous - 1 + items.length) % items.length)}
              type='button'
            >
              <ChevronLeft size={20} />
            </IconButton>

            {showIndicators ? (
              <div className='flex gap-1 rounded-[var(--radius-ui)] border border-white/10 bg-black/20 p-1 backdrop-blur-[2px]'>
                {items.map((item, index) => (
                  <button
                    key={`dot:${item.id ?? `${item.title}:${item.href}`}`}
                    className={cn('relative flex h-10 w-10 touch-manipulation items-center justify-center', index === currentSlide && 'active')}
                    data-dot={index}
                    aria-label={`Go to slide ${index + 1}`}
                    onClick={() => setCurrentSlide(index)}
                    type='button'
                  >
                    <span className='dot-indicator block h-2 w-2 rounded-full bg-white/50 transition-[background-color,transform] duration-[var(--motion-control)] ease-premium' />
                    <span className='sr-only'>Slide {index + 1}</span>
                  </button>
                ))}
              </div>
            ) : null}

            <IconButton
              className='border-white/15 bg-black/20 text-white/80 shadow-none backdrop-blur-[2px] hover:border-white/25 hover:bg-black/30 hover:text-white dark:bg-black/20 dark:hover:bg-black/30'
              data-carousel-next
              aria-label='Next slide'
              onClick={() => setCurrentSlide((previous) => (previous + 1) % items.length)}
              type='button'
            >
              <ChevronRight size={20} />
            </IconButton>
          </div>
        ) : null}
      </div>
    </section>
  );
}
