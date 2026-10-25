'use client';

import * as React from 'react';
import { ResponsiveContainer, Sankey as RechartsSankey, Tooltip } from 'recharts';

import { cn } from '../utils/cn';
import { chartTheme } from './chart-theme';

export interface SankeyNode {
  name: string;
  fill?: string;
}

export interface SankeyLink {
  source: number;
  target: number;
  value: number;
}

export interface SankeyChartProps {
  data: { nodes: SankeyNode[]; links: SankeyLink[] };
  height?: number;
  nodeWidth?: number;
  linkCurvature?: number;
  /**
   * Optional palette for nodes. Defaults to the Bleecker chart palette.
   * Individual node `fill` values override the palette.
   */
  nodeColors?: string[];
  /** Opacity of flow bands. @default 0.35 */
  linkOpacity?: number;
  /** Radius of node corners. @default 4 */
  nodeRadius?: number;
  className?: string;
}

interface NodeRenderProps {
  x: number;
  y: number;
  width: number;
  height: number;
  index: number;
  payload: SankeyNode;
}

interface LinkRenderProps {
  sourceX: number;
  targetX: number;
  sourceY: number;
  targetY: number;
  sourceControlX: number;
  targetControlX: number;
  sourceRelativeY: number;
  targetRelativeY: number;
  linkWidth: number;
  index: number;
  payload: {
    source: SankeyNode & { index?: number };
    target: SankeyNode & { index?: number };
    value: number;
  };
}

function getNodeColor(node: SankeyNode, index: number, palette: string[]): string {
  return node.fill ?? palette[index % palette.length];
}

export function SankeyChart({
  data,
  height = 400,
  nodeWidth = 12,
  linkCurvature = 0.5,
  nodeColors = chartTheme.palette,
  linkOpacity = 0.35,
  nodeRadius = 4,
  className
}: SankeyChartProps) {
  const nodeRender = React.useCallback(
    (props: NodeRenderProps) => {
      const { x, y, width, height, index, payload } = props;
      const color = getNodeColor(payload, index, nodeColors);

      return (
        <rect
          x={x}
          y={y}
          width={width}
          height={height}
          rx={nodeRadius}
          ry={nodeRadius}
          fill={color}
          stroke='var(--color-card)'
          strokeWidth={2}
        />
      );
    },
    [nodeColors, nodeRadius]
  );

  const linkRender = React.useCallback(
    (props: LinkRenderProps) => {
      const {
        sourceX,
        targetX,
        sourceY,
        targetY,
        sourceControlX,
        targetControlX,
        sourceRelativeY,
        targetRelativeY,
        linkWidth,
        payload
      } = props;

      const sourceIndex = payload.source.index ?? 0;
      const color = getNodeColor(payload.source, sourceIndex, nodeColors);

      const top = `M ${sourceX},${sourceY + sourceRelativeY}
        C ${sourceControlX},${sourceY + sourceRelativeY} ${targetControlX},${targetY + targetRelativeY} ${targetX},${targetY + targetRelativeY}`;
      const bottom = `L ${targetX},${targetY + targetRelativeY + linkWidth}
        C ${targetControlX},${targetY + targetRelativeY + linkWidth} ${sourceControlX},${sourceY + sourceRelativeY + linkWidth} ${sourceX},${sourceY + sourceRelativeY + linkWidth}`;

      return (
        <path
          d={`${top} ${bottom} Z`}
          fill={color}
          fillOpacity={linkOpacity}
          stroke='none'
          className='transition-opacity duration-300 hover:opacity-80'
        />
      );
    },
    [linkOpacity, nodeColors]
  );

  return (
    <div className={cn('w-full', className)} style={{ height }}>
      <ResponsiveContainer width='100%' height='100%'>
        <RechartsSankey
          data={data}
          nodeWidth={nodeWidth}
          linkCurvature={linkCurvature}
          nodePadding={16}
          margin={{ top: 10, right: 20, bottom: 10, left: 20 }}
          node={nodeRender as any}
          link={linkRender as any}
        >
          <Tooltip {...chartTheme.tooltip} />
        </RechartsSankey>
      </ResponsiveContainer>
    </div>
  );
}
