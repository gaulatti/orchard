'use client';

import * as React from 'react';
import { ResponsiveContainer, SunburstChart as RechartsSunburstChart } from 'recharts';

import { cn } from '../utils/cn';
import { chartTheme } from './chart-theme';

export interface SunburstNode {
  name: string;
  value?: number;
  children?: SunburstNode[];
  fill?: string;
}

export interface SunburstChartProps {
  data: SunburstNode;
  height?: number;
  dataKey?: string;
  nameKey?: string;
  className?: string;
}

function sumValues(node: SunburstNode, dataKey: string): number {
  if (node.children && node.children.length > 0) {
    return node.children.reduce((sum, child) => sum + sumValues(child, dataKey), 0);
  }
  const record = node as unknown as Record<string, unknown>;
  return typeof record[dataKey] === 'number' ? (record[dataKey] as number) : 0;
}

function applyPalette(node: SunburstNode, index = 0, depth = 0): SunburstNode {
  const color = node.fill ?? chartTheme.palette[index % chartTheme.palette.length];
  const next: SunburstNode = {
    ...node,
    fill: color
  };
  if (next.children) {
    next.children = next.children.map((child, i) => applyPalette(child, i, depth + 1));
  }
  return next;
}

export function SunburstChart({ data, height = 350, dataKey = 'value', nameKey = 'name', className }: SunburstChartProps) {
  const themedData = React.useMemo(() => {
    const withColors = applyPalette(data);
    const total = sumValues(withColors, dataKey);
    const rootRecord = withColors as unknown as Record<string, unknown>;
    if (total > 0 && typeof rootRecord[dataKey] !== 'number') {
      return { ...withColors, [dataKey]: total };
    }
    return withColors;
  }, [data, dataKey]);

  return (
    <div className={cn('w-full', className)} style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsSunburstChart data={themedData} dataKey={dataKey} nameKey={nameKey} />
      </ResponsiveContainer>
    </div>
  );
}
