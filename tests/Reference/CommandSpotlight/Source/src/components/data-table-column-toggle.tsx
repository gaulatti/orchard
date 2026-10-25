'use client';

import * as React from 'react';
import { Table } from '@tanstack/react-table';
import { Settings2 } from 'lucide-react';

import { cn } from '../utils/cn';
import { Button } from './button';
import { Checkbox } from './checkbox';
import { Popover } from './popover';

export interface DataTableColumnToggleProps<TData> {
  table: Table<TData>;
  className?: string;
  label?: string;
}

export function DataTableColumnToggle<TData>({ table, className, label = 'Columns' }: DataTableColumnToggleProps<TData>) {
  const columns = table
    .getAllColumns()
    .filter((column) => typeof column.accessorFn !== 'undefined' && column.getCanHide());

  return (
    <div className={cn('inline-block', className)}>
      <Popover
        align='end'
        className='w-52 p-1.5'
        content={
          <div>
          <p className='px-2 py-2 text-[10px] font-semibold uppercase tracking-[0.09em] text-text-secondary'>Visible columns</p>
          <div className='max-h-60 overflow-y-auto'>
            {columns.map((column) => {
                return (
                  <label
                    key={column.id}
                    className='flex min-h-9 cursor-pointer items-center gap-2.5 rounded-[5px] px-2 py-1.5 text-[13px] text-text-primary transition-colors duration-[var(--motion-control)] ease-premium hover:bg-light-sand dark:text-text-primary dark:hover:bg-white/[0.07]'
                  >
                    <Checkbox
                      checked={column.getIsVisible()}
                      onChange={(event) => column.toggleVisibility(event.target.checked)}
                    />
                    <span className='truncate'>{typeof column.columnDef.header === 'string' ? column.columnDef.header : column.id}</span>
                  </label>
                );
              })}
          </div>
          </div>
        }
      >
        <Button variant='secondary' size='sm'>
          <Settings2 size={14} />
          {label}
        </Button>
      </Popover>
    </div>
  );
}
