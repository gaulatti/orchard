import * as React from 'react';
import * as ContextMenuPrimitive from '@radix-ui/react-context-menu';
import { cn } from '../utils/cn';
import { useModalBranch } from '../utils/hooks';
import { useComposedRefs } from '@radix-ui/react-compose-refs';

const ContextMenu = ContextMenuPrimitive.Root;

const ContextMenuTrigger = ContextMenuPrimitive.Trigger;

const ContextMenuContent = React.forwardRef<
  React.ElementRef<typeof ContextMenuPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof ContextMenuPrimitive.Content>
>(({ className, ...props }, ref) => {
  const branchRef = useModalBranch<HTMLDivElement>();
  const composedRef = useComposedRefs(ref, branchRef);
  return (
    <ContextMenuPrimitive.Portal>
      <ContextMenuPrimitive.Content
        ref={composedRef}
        className={cn(
          'z-50 min-w-[10rem] overflow-hidden rounded-[var(--radius-ui)] border border-sand/35 bg-white p-1.5 text-text-primary shadow-[var(--shadow-overlay)] data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=open]:fade-in-0 data-[state=closed]:fade-out-0 data-[state=open]:zoom-in-[0.99] data-[state=closed]:zoom-out-[0.99] data-[state=open]:duration-[var(--motion-surface)] data-[state=closed]:duration-[var(--motion-exit)] data-[state=open]:ease-premium data-[state=closed]:ease-in dark:border-white/15 dark:bg-deep-sea dark:text-text-primary',
          className
        )}
        {...props}
      />
    </ContextMenuPrimitive.Portal>
  );
});
ContextMenuContent.displayName = ContextMenuPrimitive.Content.displayName;

const ContextMenuItem = React.forwardRef<
  React.ElementRef<typeof ContextMenuPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof ContextMenuPrimitive.Item> & {
    inset?: boolean;
  }
>(({ className, inset, ...props }, ref) => (
  <ContextMenuPrimitive.Item
    ref={ref}
    className={cn(
      'relative flex min-h-9 cursor-default select-none items-center rounded-[5px] px-2.5 py-1.5 text-[13px] text-text-primary outline-none transition-colors duration-[var(--motion-control)] ease-premium focus:bg-light-sand data-[disabled]:pointer-events-none data-[disabled]:opacity-40 dark:text-text-primary dark:focus:bg-white/[0.07]',
      inset && 'pl-8',
      className
    )}
    {...props}
  />
));
ContextMenuItem.displayName = ContextMenuPrimitive.Item.displayName;

export { ContextMenu, ContextMenuTrigger, ContextMenuContent, ContextMenuItem };
