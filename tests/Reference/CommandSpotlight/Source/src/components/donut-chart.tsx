import * as React from 'react';

import { PieChart, type PieChartDatum } from './pie-chart';

export interface DonutChartProps {
  data: PieChartDatum[];
  height?: number;
  showLegend?: boolean;
  centerLabel?: string;
  centerValue?: string;
}

export function DonutChart({ data, height = 300, showLegend = true, centerLabel, centerValue }: DonutChartProps) {
  return (
    <div className='relative w-full' style={{ height }}>
      <PieChart data={data} height={height} showLegend={showLegend} innerRadius='60%' />
      {(centerLabel || centerValue) && (
        <div className='pointer-events-none absolute inset-0 flex flex-col items-center justify-center pb-6'>
          {centerValue && <span className='text-2xl font-semibold tracking-refined text-text-primary dark:text-text-primary'>{centerValue}</span>}
          {centerLabel && <span className='text-xs text-text-secondary dark:text-text-secondary'>{centerLabel}</span>}
        </div>
      )}
    </div>
  );
}
