import * as React from 'react';
import {
  Area,
  AreaChart as RechartsAreaChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';

import { chartTheme, type ChartSeries } from './chart-theme';

export interface AreaChartProps {
  data: Record<string, unknown>[];
  height?: number;
  series: ChartSeries[];
  xAxisKey: string;
  showGrid?: boolean;
  showLegend?: boolean;
  yAxisTickFormatter?: (value: number) => string;
  tooltipFormatter?: (value: number) => string;
  stacked?: boolean;
}

export function AreaChart({
  data,
  height = 300,
  series,
  xAxisKey,
  showGrid = true,
  showLegend = true,
  yAxisTickFormatter,
  tooltipFormatter,
  stacked = false
}: AreaChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsAreaChart data={data} margin={{ top: 10, right: 20, bottom: 5, left: 0 }}>
          {showGrid && <CartesianGrid stroke={chartTheme.grid.stroke} strokeDasharray={chartTheme.grid.strokeDasharray} vertical={false} />}
          <XAxis
            dataKey={xAxisKey}
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
          />
          <YAxis
            stroke={chartTheme.axis.stroke}
            tick={chartTheme.axis.tick}
            tickLine={false}
            axisLine={false}
            tickFormatter={yAxisTickFormatter}
          />
          <Tooltip
            {...chartTheme.tooltip}
            formatter={(value) => [tooltipFormatter && typeof value === 'number' ? tooltipFormatter(value) : value ?? '', '']}
          />
          {showLegend && <Legend wrapperStyle={{ paddingTop: '1rem' }} />}
          {series.map((s, i) => {
            const color = s.color ?? chartTheme.palette[i % chartTheme.palette.length];
            return (
              <Area
                key={s.key}
                type='monotone'
                dataKey={s.key}
                name={s.name}
                stroke={color}
                fill={color}
                fillOpacity={0.2}
                strokeWidth={2}
                stackId={stacked ? 'total' : undefined}
              />
            );
          })}
        </RechartsAreaChart>
      </ResponsiveContainer>
    </div>
  );
}
