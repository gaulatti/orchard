'use client';

import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';
import React from 'react';

import { cn } from '../utils/cn';
import { Checkbox } from './checkbox';

// ─── SortState ────────────────────────────────────────────────────────────────

export interface SortState {
  field: string;
  order: 'asc' | 'desc';
}

// ─── SortableTableHeader ─────────────────────────────────────────────────────

export interface SortableTableHeaderProps {
  align?: 'left' | 'right';
  className?: string;
  currentSort: SortState;
  field: string;
  label: string;
  onSort: (field: string, order: 'asc' | 'desc') => void;
  sortable?: boolean;
}

export function SortableTableHeader({ align = 'left', className, currentSort, field, label, onSort, sortable = true }: SortableTableHeaderProps) {
  const isSorted = currentSort.field === field;

  if (!sortable) {
    return (
      <th
        className={cn(
          align === 'right' ? 'text-right' : 'text-left',
          'h-11 px-4 text-[11px] font-semibold uppercase tracking-[0.08em] text-text-secondary dark:text-text-secondary',
          className
        )}
      >
        {label}
      </th>
    );
  }

  const SortIcon = isSorted ? (currentSort.order === 'asc' ? ArrowUp : ArrowDown) : ArrowUpDown;

  return (
    <th className={cn(align === 'right' ? 'text-right' : 'text-left', 'h-11 px-4', className)}>
      <button
        type='button'
        onClick={() => onSort(field, isSorted && currentSort.order === 'asc' ? 'desc' : 'asc')}
        className={cn(
          'inline-flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-[0.08em] transition-colors duration-[var(--motion-control)] ease-premium focus:outline-none focus-visible:underline',
          isSorted ? 'text-sea dark:text-accent-blue' : 'text-text-secondary hover:text-text-primary dark:text-text-secondary dark:hover:text-text-primary'
        )}
      >
        {label}
        <SortIcon size={12} className={cn('flex-shrink-0 transition-opacity duration-[var(--motion-control)] ease-premium', isSorted ? 'opacity-100' : 'opacity-40')} />
      </button>
    </th>
  );
}

// ─── Table (root wrapper) ─────────────────────────────────────────────────────

export interface TableProps extends React.HTMLAttributes<HTMLTableElement> {
  /** Wraps the table in a horizontally-scrollable container */
  containerClassName?: string;
}

export const Table = React.forwardRef<HTMLTableElement, TableProps>(function Table({ className, containerClassName, ...props }, ref) {
  return (
    <div className={cn('w-full overflow-x-auto', containerClassName)}>
      <table ref={ref} className={cn('w-full caption-bottom text-sm', className)} {...props} />
    </div>
  );
});

// ─── TableHeader ──────────────────────────────────────────────────────────────

export const TableHeader = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(function TableHeader(
  { className, ...props },
  ref
) {
  return (
    <thead
      ref={ref}
      className={cn('border-b border-sand/25 bg-light-sand/40 dark:border-white/10 dark:bg-white/[0.035]', '[&_tr]:hover:bg-transparent', className)}
      {...props}
    />
  );
});

// ─── TableBody ────────────────────────────────────────────────────────────────

export const TableBody = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(function TableBody(
  { className, ...props },
  ref
) {
  return <tbody ref={ref} className={cn('[&_tr:last-child]:border-b-0', className)} {...props} />;
});

// ─── TableFooter ──────────────────────────────────────────────────────────────

export const TableFooter = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(function TableFooter(
  { className, ...props },
  ref
) {
  return <tfoot ref={ref} className={cn('border-t border-sand/10 bg-sand/5 font-medium dark:border-sand/20 dark:bg-sand/10', className)} {...props} />;
});

// ─── TableRow ─────────────────────────────────────────────────────────────────

let hoveredRow: HTMLTableRowElement | null = null;

export const TableRow = React.forwardRef<HTMLTableRowElement, React.HTMLAttributes<HTMLTableRowElement>>(function TableRow(
  { className, onMouseEnter, onMouseLeave, ...props },
  ref
) {
  const rowRef = React.useRef<HTMLTableRowElement>(null);

  const mergedRef = React.useCallback(
    (el: HTMLTableRowElement | null) => {
      rowRef.current = el;
      if (typeof ref === 'function') ref(el);
      else if (ref) (ref as React.MutableRefObject<HTMLTableRowElement | null>).current = el;
    },
    [ref]
  );

  const handleMouseEnter = React.useCallback(
    (e: React.MouseEvent<HTMLTableRowElement>) => {
      onMouseEnter?.(e);
      const el = rowRef.current;
      if (!el) return;

      const prev = hoveredRow;
      hoveredRow = el;

      if (prev && prev !== el) {
        el.style.transition = 'none';
        requestAnimationFrame(() => {
          el.style.transition = '';
        });
      }
    },
    [onMouseEnter]
  );

  const handleMouseLeave = React.useCallback(
    (e: React.MouseEvent<HTMLTableRowElement>) => {
      onMouseLeave?.(e);
    },
    [onMouseLeave]
  );

  return (
    <tr
      ref={mergedRef}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      className={cn(
        'border-b border-sand/20 data-[selected=true]:bg-sea/[0.045] dark:border-white/[0.07] dark:data-[selected=true]:bg-accent-blue/[0.08]',
        'transition-colors duration-[var(--motion-surface)] ease-premium hover:bg-light-sand/45 dark:hover:bg-white/[0.035]',
        className
      )}
      {...props}
    />
  );
});

// ─── TableHead (<th>) ─────────────────────────────────────────────────────────

export const TableHead = React.forwardRef<HTMLTableCellElement, React.ThHTMLAttributes<HTMLTableCellElement>>(function TableHead({ className, ...props }, ref) {
  return (
    <th
      ref={ref}
      className={cn(
        'h-11 px-4 text-left align-middle text-[11px] font-semibold uppercase tracking-[0.08em] text-text-secondary dark:text-text-secondary',
        '[&:has([role=checkbox])]:pr-0 [&>[role=checkbox]]:translate-y-[2px]',
        className
      )}
      {...props}
    />
  );
});

// ─── TableCell (<td>) ─────────────────────────────────────────────────────────

export const TableCell = React.forwardRef<HTMLTableCellElement, React.TdHTMLAttributes<HTMLTableCellElement>>(function TableCell({ className, ...props }, ref) {
  return (
    <td
      ref={ref}
      className={cn(
        'px-4 py-3 align-middle text-sm text-text-primary dark:text-text-primary',
        '[&:has([role=checkbox])]:pr-0 [&>[role=checkbox]]:translate-y-[2px]',
        className
      )}
      {...props}
    />
  );
});

// ─── TableCaption ─────────────────────────────────────────────────────────────

export const TableCaption = React.forwardRef<HTMLTableCaptionElement, React.HTMLAttributes<HTMLTableCaptionElement>>(function TableCaption(
  { className, ...props },
  ref
) {
  return <caption ref={ref} className={cn('mt-4 text-sm text-text-secondary dark:text-text-secondary', className)} {...props} />;
});

// ─── DataTable ───────────────────────────────────────────────────────────────

export interface ColumnDef<TData> {
  /** Unique key matching a property of TData, or an arbitrary id for computed columns */
  key: string;
  header: string;
  /** Render the cell value. Defaults to `String(row[key])`. */
  cell?: (row: TData, index: number) => React.ReactNode;
  sortable?: boolean;
  /** Custom compare when sort is uncontrolled. Defaults to string/number comparison. */
  sortFn?: (a: TData, b: TData) => number;
  align?: 'left' | 'right' | 'center';
  className?: string;
  headerClassName?: string;
}

export interface DataTableProps<TData> {
  caption?: string;
  className?: string;
  columns: ColumnDef<TData>[];
  containerClassName?: string;
  data: TData[];
  emptyMessage?: string;
  /** Required so selection and keying work correctly */
  getRowKey: (row: TData, index: number) => string;
  /** Footer row — receives the current (sorted) data */
  footer?: (data: TData[]) => React.ReactNode;
  /** Controlled sort. If omitted, sort is managed internally. */
  sort?: SortState;
  onSortChange?: (sort: SortState) => void;
  /** Enable checkbox selection */
  selectable?: boolean;
  selectedKeys?: Set<string>;
  onSelectionChange?: (keys: Set<string>) => void;
}

function defaultCompare<TData>(key: string, a: TData, b: TData): number {
  const av = (a as Record<string, unknown>)[key];
  const bv = (b as Record<string, unknown>)[key];
  if (typeof av === 'number' && typeof bv === 'number') return av - bv;
  return String(av ?? '').localeCompare(String(bv ?? ''));
}

export function DataTable<TData>({
  caption,
  className,
  columns,
  containerClassName,
  data,
  emptyMessage = 'No results.',
  getRowKey,
  footer,
  sort: controlledSort,
  onSortChange,
  selectable = false,
  selectedKeys: controlledSelected,
  onSelectionChange
}: DataTableProps<TData>) {
  const [internalSort, setInternalSort] = React.useState<SortState>({ field: '', order: 'asc' });
  const [internalSelected, setInternalSelected] = React.useState<Set<string>>(new Set());

  const sort = controlledSort ?? internalSort;
  const selectedKeys = controlledSelected ?? internalSelected;

  const setSort = (next: SortState) => {
    setInternalSort(next);
    onSortChange?.(next);
  };

  const setSelectedKeys = (next: Set<string>) => {
    setInternalSelected(next);
    onSelectionChange?.(next);
  };

  const handleSort = (field: string, order: 'asc' | 'desc') => setSort({ field, order });

  // Sort data
  const sortedData = React.useMemo(() => {
    if (!sort.field) return data;
    const col = columns.find((c) => c.key === sort.field);
    const compare = col?.sortFn ?? ((a: TData, b: TData) => defaultCompare(sort.field, a, b));
    return [...data].sort((a, b) => (sort.order === 'asc' ? compare(a, b) : -compare(a, b)));
  }, [data, sort, columns]);

  const rowKeys = sortedData.map(getRowKey);
  const allSelected = rowKeys.length > 0 && rowKeys.every((k) => selectedKeys.has(k));
  const someSelected = rowKeys.some((k) => selectedKeys.has(k)) && !allSelected;

  const toggleAll = (checked: boolean) => {
    setSelectedKeys(checked ? new Set(rowKeys) : new Set());
  };

  const toggleRow = (key: string, checked: boolean) => {
    const next = new Set(selectedKeys);
    checked ? next.add(key) : next.delete(key);
    setSelectedKeys(next);
  };

  return (
    <div className={cn('overflow-hidden rounded-[var(--radius-card)] border border-sand/30 bg-white shadow-[var(--shadow-surface)] dark:border-white/10 dark:bg-deep-sea', containerClassName)}>
      <Table className={className}>
        {caption && <TableCaption>{caption}</TableCaption>}
        <TableHeader>
          <TableRow>
            {selectable && (
              <TableHead className='w-10'>
                <Checkbox
                  checked={allSelected}
                  ref={(el) => {
                    if (el) el.indeterminate = someSelected;
                  }}
                  onChange={(e) => toggleAll(e.target.checked)}
                  aria-label='Select all'
                />
              </TableHead>
            )}
            {columns.map((col) => (
              <SortableTableHeader
                key={col.key}
                field={col.key}
                label={col.header}
                currentSort={sort}
                onSort={handleSort}
                sortable={col.sortable !== false}
                align={col.align === 'right' ? 'right' : 'left'}
                className={col.headerClassName}
              />
            ))}
          </TableRow>
        </TableHeader>

        <TableBody>
          {sortedData.length === 0 ? (
            <TableRow>
              <TableCell colSpan={columns.length + (selectable ? 1 : 0)} className='h-24 text-center text-text-secondary dark:text-text-secondary'>
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            sortedData.map((row, i) => {
              const key = rowKeys[i];
              const isSelected = selectedKeys.has(key);
              return (
                <TableRow key={key} data-selected={isSelected}>
                  {selectable && (
                    <TableCell>
                      <Checkbox
                        checked={isSelected}
                        onChange={(e) => toggleRow(key, e.target.checked)}
                        aria-label={`Select row ${key}`}
                      />
                    </TableCell>
                  )}
                  {columns.map((col) => (
                    <TableCell key={col.key} className={cn(col.align === 'right' && 'text-right', col.align === 'center' && 'text-center', col.className)}>
                      {col.cell ? col.cell(row, i) : String((row as Record<string, unknown>)[col.key] ?? '')}
                    </TableCell>
                  ))}
                </TableRow>
              );
            })
          )}
        </TableBody>

        {footer && <TableFooter>{footer(sortedData)}</TableFooter>}
      </Table>
    </div>
  );
}

// (SortState and SortableTableHeader are defined earlier in this file and exported directly)
