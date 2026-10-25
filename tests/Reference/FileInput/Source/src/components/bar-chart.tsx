import * as React from 'react';
import {
  Bar,
  BarChart as RechartsBarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';

import { chartTheme, type ChartSeries } from './chart-theme';

export interface BarChartProps {
  data: Record<string, unknown>[];
  height?: number;
  series: ChartSeries[];
  xAxisKey: string;
  showGrid?: boolean;
  showLegend?: boolean;
  yAxisTickFormatter?: (value: number) => string;
  tooltipFormatter?: (value: number) => string;
  layout?: 'horizontal' | 'vertical';
}

export function BarChart({
  data,
  height = 300,
  series,
  xAxisKey,
  showGrid = true,
  showLegend = true,
  yAxisTickFormatter,
  tooltipFormatter,
  layout = 'horizontal'
}: BarChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsBarChart data={data} layout={layout} margin={{ top: 10, right: 20, bottom: 5, left: 0 }}>
          {showGrid && <CartesianGrid stroke={chartTheme.grid.stroke} strokeDasharray={chartTheme.grid.strokeDasharray} />}
          <XAxis
            type={layout === 'horizontal' ? 'category' : 'number'}
            dataKey={layout === 'horizontal' ? xAxisKey : undefined}
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
          />
          <YAxis
            type={layout === 'horizontal' ? 'number' : 'category'}
            dataKey={layout === 'vertical' ? xAxisKey : undefined}
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
            tickFormatter={yAxisTickFormatter}
          />
          <Tooltip
            {...chartTheme.tooltip}
            formatter={(value) => [tooltipFormatter && typeof value === 'number' ? tooltipFormatter(value) : value ?? '', '']}
            cursor={{ fill: 'var(--color-muted)' }}
          />
          {showLegend && <Legend wrapperStyle={{ paddingTop: '1rem' }} />}
          {series.map((s, i) => (
            <Bar
              key={s.key}
              dataKey={s.key}
              name={s.name}
              fill={s.color ?? chartTheme.palette[i % chartTheme.palette.length]}
              radius={[4, 4, 0, 0]}
            />
          ))}
        </RechartsBarChart>
      </ResponsiveContainer>
    </div>
  );
}
