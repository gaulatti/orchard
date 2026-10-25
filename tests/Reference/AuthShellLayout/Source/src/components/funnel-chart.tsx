import * as React from 'react';
import { Funnel, FunnelChart as RechartsFunnelChart, LabelList, ResponsiveContainer, Tooltip } from 'recharts';

import { chartTheme } from './chart-theme';

export interface FunnelChartDatum {
  name: string;
  value: number;
  fill?: string;
}

export interface FunnelChartProps {
  data: FunnelChartDatum[];
  height?: number;
  showLabels?: boolean;
  valueFormatter?: (value: number) => string;
}

export function FunnelChart({ data, height = 350, showLabels = true, valueFormatter }: FunnelChartProps) {
  const coloredData = data.map((entry, index) => ({
    ...entry,
    fill: entry.fill ?? chartTheme.palette[index % chartTheme.palette.length]
  }));

  return (
    <div className='w-full' style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsFunnelChart>
          <Tooltip
            {...chartTheme.tooltip}
            formatter={(value) => [valueFormatter && typeof value === 'number' ? valueFormatter(value) : value, '']}
          />
          <Funnel data={coloredData} dataKey='value' nameKey='name' isAnimationActive>
            {showLabels && (
              <LabelList
                position='inside'
                fill='var(--color-text-primary)'
                stroke='none'
                dataKey='name'
                formatter={((value: unknown, entry: { value?: number }) => `${value ?? ''}: ${entry?.value ?? ''}`) as unknown as (value: unknown) => string}
              />
            )}
          </Funnel>
        </RechartsFunnelChart>
      </ResponsiveContainer>
    </div>
  );
}
