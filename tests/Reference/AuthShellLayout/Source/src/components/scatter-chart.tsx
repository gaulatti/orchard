import * as React from 'react';
import { CartesianGrid, Legend, ResponsiveContainer, Scatter as RechartsScatter, ScatterChart as RechartsScatterChart, Tooltip, XAxis, YAxis, ZAxis } from 'recharts';

import { chartTheme } from './chart-theme';

export interface ScatterSeries {
  key: string;
  name: string;
  xKey: string;
  yKey: string;
  zKey?: string;
  color?: string;
  data?: Record<string, unknown>[];
}

export interface ScatterChartProps {
  data: Record<string, unknown>[];
  height?: number;
  series: ScatterSeries[];
  showGrid?: boolean;
  showLegend?: boolean;
  xAxisLabel?: string;
  yAxisLabel?: string;
  xAxisTickFormatter?: (value: number | string) => string;
  yAxisTickFormatter?: (value: number | string) => string;
  tooltipFormatter?: (value: number) => string;
}

export function ScatterChart({
  data,
  height = 350,
  series,
  showGrid = true,
  showLegend = true,
  xAxisLabel,
  yAxisLabel,
  xAxisTickFormatter,
  yAxisTickFormatter,
  tooltipFormatter
}: ScatterChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsScatterChart margin={{ top: 10, right: 20, bottom: xAxisLabel ? 25 : 5, left: yAxisLabel ? 20 : 0 }}>
          {showGrid && <CartesianGrid stroke={chartTheme.grid.stroke} strokeDasharray={chartTheme.grid.strokeDasharray} />}
          <XAxis
            type='number'
            dataKey={series[0].xKey}
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
            tickFormatter={xAxisTickFormatter}
            label={xAxisLabel ? { value: xAxisLabel, position: 'insideBottom', offset: -15, fill: 'var(--color-text-secondary)', fontSize: 12 } : undefined}
          />
          <YAxis
            type='number'
            dataKey={series[0].yKey}
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
            tickFormatter={yAxisTickFormatter}
            label={yAxisLabel ? { value: yAxisLabel, angle: -90, position: 'insideLeft', fill: 'var(--color-text-secondary)', fontSize: 12 } : undefined}
          />
          <ZAxis type='number' dataKey={series[0].zKey} range={[60, 400]} />
          <Tooltip
            {...chartTheme.tooltip}
            cursor={{ strokeDasharray: '3 3' }}
            formatter={(value, name) => [tooltipFormatter && typeof value === 'number' ? tooltipFormatter(value) : value, name]}
          />
          {showLegend && <Legend wrapperStyle={{ paddingTop: '1rem' }} />}
          {series.map((s, i) => (
            <RechartsScatter
              key={s.key}
              name={s.name}
              data={s.data ?? data}
              fill={s.color ?? chartTheme.palette[i % chartTheme.palette.length]}
            />
          ))}
        </RechartsScatterChart>
      </ResponsiveContainer>
    </div>
  );
}
