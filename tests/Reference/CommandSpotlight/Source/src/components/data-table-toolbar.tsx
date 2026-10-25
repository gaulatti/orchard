import * as React from 'react';
import { Table } from '@tanstack/react-table';
import { X } from 'lucide-react';

import { cn } from '../utils/cn';
import { Button } from './button';
import { Input } from './input';

export interface DataTableToolbarProps<TData> {
  table: Table<TData>;
  searchColumn?: string;
  searchPlaceholder?: string;
  filterRenderers?: React.ReactNode;
  actions?: React.ReactNode;
  className?: string;
}

export function DataTableToolbar<TData>({
  table,
  searchColumn,
  searchPlaceholder = 'Search...',
  filterRenderers,
  actions,
  className
}: DataTableToolbarProps<TData>) {
  const isFiltered = table.getState().columnFilters.length > 0 || Boolean(table.getState().globalFilter);

  return (
    <div className={cn('flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between', className)}>
      <div className='flex flex-1 flex-col gap-3 sm:flex-row sm:items-center'>
        {searchColumn && (
          <Input
            placeholder={searchPlaceholder}
            value={(table.getColumn(searchColumn)?.getFilterValue() as string) ?? ''}
            onChange={(event) => table.getColumn(searchColumn)?.setFilterValue(event.target.value)}
            className='h-9 w-full sm:max-w-xs'
          />
        )}
        {filterRenderers}
      </div>

      <div className='flex items-center gap-2'>
        {isFiltered && (
          <Button variant='ghost' size='sm' onClick={() => table.resetColumnFilters()}>
            <X size={14} />
            Reset
          </Button>
        )}
        {actions}
      </div>
    </div>
  );
}
