import * as React from 'react';
import { Table } from '@tanstack/react-table';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

import { cn } from '../utils/cn';
import { Button } from './button';
import { Select, SelectOption } from './select';

export interface DataTablePaginationProps<TData> {
  table: Table<TData>;
  className?: string;
  pageSizeOptions?: number[];
}

export function DataTablePagination<TData>({ table, className, pageSizeOptions = [10, 20, 30, 50, 100] }: DataTablePaginationProps<TData>) {
  const pageSize = table.getState().pagination.pageSize;
  const pageOptions = table.getPageOptions();

  const sizeOptions: SelectOption[] = pageSizeOptions.map((size) => ({ label: `${size}`, value: String(size) }));

  return (
    <div className={cn('flex flex-col items-center justify-between gap-4 px-2 sm:flex-row', className)}>
      <div className='text-sm text-text-secondary dark:text-text-secondary'>
        {table.getFilteredSelectedRowModel().rows.length > 0 && (
          <>
            {table.getFilteredSelectedRowModel().rows.length} of {table.getFilteredRowModel().rows.length} row(s) selected.
          </>
        )}
      </div>

      <div className='flex items-center gap-4'>
        <div className='flex items-center gap-2 text-sm text-text-secondary'>
          <span>Rows per page</span>
          <div className='w-20'>
            <Select
              value={String(pageSize)}
              options={sizeOptions}
              onChange={(value) => table.setPageSize(Number(value))}
            />
          </div>
        </div>

        <div className='text-sm font-medium text-text-primary dark:text-text-primary'>
          Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
        </div>

        <div className='flex items-center gap-1'>
          <Button
            variant='secondary'
            size='sm'
            className='hidden h-8 w-8 p-0 sm:flex'
            onClick={() => table.setPageIndex(0)}
            disabled={!table.getCanPreviousPage()}
          >
            <ChevronsLeft className='h-4 w-4' />
          </Button>
          <Button variant='secondary' size='sm' className='h-8 w-8 p-0' onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()}>
            <ChevronLeft className='h-4 w-4' />
          </Button>
          <Button variant='secondary' size='sm' className='h-8 w-8 p-0' onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
            <ChevronRight className='h-4 w-4' />
          </Button>
          <Button
            variant='secondary'
            size='sm'
            className='hidden h-8 w-8 p-0 sm:flex'
            onClick={() => table.setPageIndex(table.getPageCount() - 1)}
            disabled={!table.getCanNextPage()}
          >
            <ChevronsRight className='h-4 w-4' />
          </Button>
        </div>
      </div>
    </div>
  );
}
