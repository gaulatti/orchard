/**
 * Locale-aware formatting helpers for dashboard numbers, currency and dates.
 */

export interface FormatNumberOptions extends Intl.NumberFormatOptions {
  locale?: string;
}

export function formatNumber(value: number | undefined | null, options: FormatNumberOptions = {}): string {
  if (value === undefined || value === null || Number.isNaN(value)) return '';
  const { locale, ...intlOptions } = options;
  return new Intl.NumberFormat(locale, intlOptions).format(value);
}

export function formatCurrency(value: number | undefined | null, currency = 'USD', options: FormatNumberOptions = {}): string {
  if (value === undefined || value === null || Number.isNaN(value)) return '';
  return new Intl.NumberFormat(options.locale, { style: 'currency', currency, ...options }).format(value);
}

export function formatCompactNumber(value: number | undefined | null, options: FormatNumberOptions = {}): string {
  if (value === undefined || value === null || Number.isNaN(value)) return '';
  return new Intl.NumberFormat(options.locale, { notation: 'compact', ...options }).format(value);
}

export function formatPercent(value: number | undefined | null, options: FormatNumberOptions = {}): string {
  if (value === undefined || value === null || Number.isNaN(value)) return '';
  return new Intl.NumberFormat(options.locale, { style: 'percent', minimumFractionDigits: 1, maximumFractionDigits: 1, ...options }).format(value);
}

export function formatDate(value: string | number | Date | undefined | null, options: FormatNumberOptions & { dateStyle?: 'full' | 'long' | 'medium' | 'short' } = {}): string {
  if (value === undefined || value === null) return '';
  const date = typeof value === 'string' || typeof value === 'number' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return '';
  const { locale, dateStyle = 'medium', ...intlOptions } = options;
  return new Intl.DateTimeFormat(locale, { dateStyle, ...intlOptions }).format(date);
}

export function formatDateTime(value: string | number | Date | undefined | null, options: FormatNumberOptions = {}): string {
  if (value === undefined || value === null) return '';
  const date = typeof value === 'string' || typeof value === 'number' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return '';
  const { locale, ...intlOptions } = options;
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short', ...intlOptions }).format(date);
}

export function formatRelativeTime(value: string | number | Date | undefined | null, locale = 'en-US'): string {
  if (value === undefined || value === null) return '';
  const date = typeof value === 'string' || typeof value === 'number' ? new Date(value) : value;
  if (Number.isNaN(date.getTime())) return '';
  const now = Date.now();
  const diff = date.getTime() - now;
  const seconds = Math.round(diff / 1000);
  const minutes = Math.round(seconds / 60);
  const hours = Math.round(minutes / 60);
  const days = Math.round(hours / 24);

  const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
  if (Math.abs(seconds) < 60) return rtf.format(seconds, 'second');
  if (Math.abs(minutes) < 60) return rtf.format(minutes, 'minute');
  if (Math.abs(hours) < 24) return rtf.format(hours, 'hour');
  return rtf.format(days, 'day');
}
