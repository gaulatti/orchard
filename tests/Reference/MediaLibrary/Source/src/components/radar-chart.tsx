import * as React from 'react';
import { Legend, PolarAngleAxis, PolarGrid, PolarRadiusAxis, Radar as RechartsRadar, RadarChart as RechartsRadarChart, ResponsiveContainer, Tooltip } from 'recharts';

import { chartTheme } from './chart-theme';

export interface RadarSeries {
  key: string;
  name: string;
  color?: string;
  fill?: boolean;
}

export interface RadarChartProps {
  data: Record<string, unknown>[];
  height?: number;
  subjectKey: string;
  series: RadarSeries[];
  showLegend?: boolean;
  tickFormatter?: (value: number) => string;
}

export function RadarChart({ data, height = 350, subjectKey, series, showLegend = true, tickFormatter }: RadarChartProps) {
  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsRadarChart data={data} margin={{ top: 10, right: 20, bottom: 5, left: 0 }}>
          <PolarGrid stroke={chartTheme.grid.stroke} />
          <PolarAngleAxis dataKey={subjectKey} tick={{ fill: 'var(--color-text-secondary)', fontSize: 12 }} />
          <PolarRadiusAxis
            stroke={chartTheme.axis.stroke}
            tick={{ fill: 'var(--color-text-secondary)', fontSize: 10 }}
            tickFormatter={tickFormatter}
          />
          <Tooltip {...chartTheme.tooltip} formatter={(value) => [tickFormatter && typeof value === 'number' ? tickFormatter(value) : value, '']} />
          {showLegend && <Legend wrapperStyle={{ paddingTop: '1rem' }} />}
          {series.map((s, i) => {
            const color = s.color ?? chartTheme.palette[i % chartTheme.palette.length];
            return (
              <RechartsRadar
                key={s.key}
                name={s.name}
                dataKey={s.key}
                stroke={color}
                fill={color}
                fillOpacity={s.fill ? 0.35 : 0}
              />
            );
          })}
        </RechartsRadarChart>
      </ResponsiveContainer>
    </div>
  );
}
