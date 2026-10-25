import * as React from 'react';

/**
 * Shared theming helpers for Recharts-based dashboard charts.
 *
 * Components in this library use CSS custom properties so charts automatically
 * adapt to the active Bleecker theme (light / dark / custom brand overrides).
 */

export const chartTheme = {
  colors: {
    sea: 'var(--color-sea)',
    desert: 'var(--color-desert)',
    terracotta: 'var(--color-terracotta)',
    sunset: 'var(--color-sunset)',
    dusk: 'var(--color-dusk)',
    accentGold: 'var(--color-accent-gold)',
    accentBlue: 'var(--color-accent-blue)',
    textPrimary: 'var(--color-text-primary)',
    textSecondary: 'var(--color-text-secondary)',
    background: 'var(--color-background)',
    card: 'var(--color-card)',
    border: 'var(--color-border)'
  },
  palette: [
    'var(--color-sea)',
    'var(--color-desert)',
    'var(--color-terracotta)',
    'var(--color-sunset)',
    'var(--color-dusk)',
    'var(--color-accent-gold)',
    'var(--color-accent-blue)',
    'var(--color-accent-oxblood)',
    'var(--color-accent-bronze)',
    'var(--color-accent-yellow)'
  ],
  grid: {
    stroke: 'var(--color-border)',
    strokeDasharray: '3 3'
  },
  axis: {
    stroke: 'var(--color-border)',
    tick: { fill: 'var(--color-text-secondary)', fontSize: 12 }
  },
  tooltip: {
    contentStyle: {
      backgroundColor: 'var(--color-card)',
      borderRadius: '12px',
      border: '1px solid var(--color-border)',
      boxShadow: '0 8px 32px rgba(0,0,0,0.08)',
      backdropFilter: 'blur(8px)'
    } as React.CSSProperties,
    itemStyle: { color: 'var(--color-text-primary)' } as React.CSSProperties,
    labelStyle: { color: 'var(--color-text-secondary)', fontWeight: 600 } as React.CSSProperties
  }
};

export function pickColor(index: number): string {
  return chartTheme.palette[index % chartTheme.palette.length];
}

export interface ChartSeries {
  key: string;
  name: string;
  color?: string;
}
