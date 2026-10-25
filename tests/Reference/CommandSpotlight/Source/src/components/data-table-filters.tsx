import * as React from 'react';
import { Table, Column } from '@tanstack/react-table';

import { cn } from '../utils/cn';
import { Select, SelectOption } from './select';

export interface DataTableSelectFilterProps<TData> {
  table: Table<TData>;
  columnId: string;
  placeholder?: string;
  options: SelectOption[];
  className?: string;
}

export function DataTableSelectFilter<TData>({ table, columnId, placeholder = 'All', options, className }: DataTableSelectFilterProps<TData>) {
  const column = table.getColumn(columnId);
  if (!column) return null;

  const value = (column.getFilterValue() as string) ?? '';
  const selectOptions = [{ label: placeholder, value: '' }, ...options];

  return (
    <div className={cn('w-full sm:w-44', className)}>
      <Select
        value={value}
        options={selectOptions}
        placeholder={placeholder}
        onChange={(next) => column.setFilterValue(next || undefined)}
      />
    </div>
  );
}

export interface DataTableFacetedFilterProps<TData, TValue = string> {
  column?: Column<TData, unknown>;
  title?: string;
  options: { label: string; value: TValue; icon?: React.ComponentType<{ className?: string }> }[];
}
