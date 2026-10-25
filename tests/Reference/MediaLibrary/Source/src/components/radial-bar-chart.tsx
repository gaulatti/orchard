import * as React from 'react';
import { Legend, PolarAngleAxis, RadialBar, RadialBarChart as RechartsRadialBarChart, ResponsiveContainer, Tooltip } from 'recharts';

import { chartTheme } from './chart-theme';

export interface RadialBarChartDatum {
  name: string;
  value: number;
  fill?: string;
}

export interface RadialBarChartProps {
  data: RadialBarChartDatum[];
  height?: number;
  innerRadius?: string | number;
  outerRadius?: string | number;
  showLegend?: boolean;
  startAngle?: number;
  endAngle?: number;
}

export function RadialBarChart({
  data,
  height = 350,
  innerRadius = '10%',
  outerRadius = '80%',
  showLegend = true,
  startAngle = 90,
  endAngle = -270
}: RadialBarChartProps) {
  const coloredData = data.map((entry, index) => ({
    ...entry,
    fill: entry.fill ?? chartTheme.palette[index % chartTheme.palette.length]
  }));

  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsRadialBarChart
          data={coloredData}
          innerRadius={innerRadius}
          outerRadius={outerRadius}
          startAngle={startAngle}
          endAngle={endAngle}
        >
          <PolarAngleAxis type='number' domain={[0, 'dataMax']} tick={false} />
          <RadialBar
            background={{ fill: 'var(--color-muted)' }}
            dataKey='value'
            cornerRadius={8}
            label={{ position: 'insideStart', fill: 'var(--color-text-primary)', fontWeight: 600 }}
          />
          <Tooltip {...chartTheme.tooltip} formatter={(value) => [value ?? '', '']} />
          {showLegend && <Legend iconSize={10} layout='vertical' verticalAlign='middle' align='right' />}
        </RechartsRadialBarChart>
      </ResponsiveContainer>
    </div>
  );
}
