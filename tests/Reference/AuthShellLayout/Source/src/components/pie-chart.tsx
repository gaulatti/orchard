import * as React from 'react';
import { Cell, Pie, PieChart as RechartsPieChart, ResponsiveContainer, Tooltip, Legend } from 'recharts';

import { chartTheme } from './chart-theme';

export interface PieChartDatum {
  name: string;
  value: number;
  color?: string;
}

export interface PieChartProps {
  data: PieChartDatum[];
  height?: number;
  nameKey?: string;
  dataKey?: string;
  showLegend?: boolean;
  innerRadius?: number | string;
}

export function PieChart({
  data,
  height = 300,
  nameKey = 'name',
  dataKey = 'value',
  showLegend = true,
  innerRadius = 0
}: PieChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsPieChart>
          <Pie
            data={data}
            dataKey={dataKey}
            nameKey={nameKey}
            cx='50%'
            cy='50%'
            outerRadius='80%'
            innerRadius={innerRadius}
            paddingAngle={2}
            labelLine={false}
            label={({ name, percent }) => `${name}: ${percent !== undefined ? (percent * 100).toFixed(0) : 0}%`}
          >
            {data.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.color ?? chartTheme.palette[index % chartTheme.palette.length]} />
            ))}
          </Pie>
          <Tooltip {...chartTheme.tooltip} formatter={(value) => [value ?? '', '']} />
          {showLegend && <Legend verticalAlign='bottom' height={36} />}
        </RechartsPieChart>
      </ResponsiveContainer>
    </div>
  );
}
