import * as React from 'react';
import { CartesianGrid, Line, LineChart as RechartsLineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

import { chartTheme } from './chart-theme';

interface ChartProps {
  data: any[];
  height?: number;
}

export function Chart({ data, height = 300 }: ChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsLineChart data={data} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid stroke={chartTheme.grid.stroke} strokeDasharray={chartTheme.grid.strokeDasharray} vertical={false} />
          <XAxis
            dataKey='name'
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
            tickFormatter={(value) => `$${value}`}
          />
          <Tooltip
            {...chartTheme.tooltip}
            formatter={(value) => [value !== undefined ? `$${value}` : '', 'Total']}
          />
          <Line
            type='monotone'
            dataKey='total'
            stroke={chartTheme.colors.desert}
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 6, fill: chartTheme.colors.desert, stroke: 'var(--color-card)', strokeWidth: 2 }}
          />
        </RechartsLineChart>
      </ResponsiveContainer>
    </div>
  );
}
