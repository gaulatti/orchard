import * as React from 'react';
import { Line, LineChart as RechartsLineChart, ResponsiveContainer, Tooltip } from 'recharts';

import { chartTheme } from './chart-theme';

export interface SparklineProps {
  data: Record<string, unknown>[];
  dataKey: string;
  height?: number;
  color?: string;
  showArea?: boolean;
  className?: string;
}

export function Sparkline({ data, dataKey, height = 40, color, showArea = false, className }: SparklineProps) {
  const stroke = color ?? chartTheme.colors.sea;
  return (
    <div className={className} style={{ height, width: '100%' }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsLineChart data={data} margin={{ top: 2, right: 2, bottom: 2, left: 2 }}>
          <defs>
            <linearGradient id='sparklineGradient' x1='0' y1='0' x2='0' y2='1'>
              <stop offset='0%' stopColor={stroke} stopOpacity={0.3} />
              <stop offset='100%' stopColor={stroke} stopOpacity={0} />
            </linearGradient>
          </defs>
          <Tooltip
            {...chartTheme.tooltip}
            formatter={(value) => [value ?? '', '']}
            labelFormatter={() => ''}
          />
          <Line
            type='monotone'
            dataKey={dataKey}
            stroke={stroke}
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 3, strokeWidth: 0 }}
            fill={showArea ? 'url(#sparklineGradient)' : undefined}
          />
        </RechartsLineChart>
      </ResponsiveContainer>
    </div>
  );
}
