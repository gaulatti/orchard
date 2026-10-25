'use client';

import * as React from 'react';

/**
 * Returns true once the component has mounted. Useful for avoiding SSR
 * hydration mismatches in components that read from `window`/`document`.
 */
export function useMounted(): boolean {
  const [mounted, setMounted] = React.useState(false);
  React.useEffect(() => setMounted(true), []);
  return mounted;
}

/**
 * Subscribes to a media query and returns its current match state.
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = React.useState(false);

  React.useEffect(() => {
    if (typeof window === 'undefined') return undefined;
    const media = window.matchMedia(query);
    const update = () => setMatches(media.matches);
    update();
    media.addEventListener('change', update);
    return () => media.removeEventListener('change', update);
  }, [query]);

  return matches;
}

/**
 * Tracks whether a debounced value has settled.
 */
export function useDebounce<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = React.useState(value);

  React.useEffect(() => {
    const handler = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(handler);
  }, [value, delay]);

  return debounced;
}

/**
 * Returns the previous value of a prop/state.
 */
export function usePrevious<T>(value: T): T | undefined {
  const ref = React.useRef<T>(undefined);

  React.useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}

/**
 * Keeps a conditional surface mounted long enough for its exit motion to
 * complete, and separates DOM presence from visual visibility.
 */
export function usePresence(open: boolean, exitDuration = 150) {
  const [present, setPresent] = React.useState(open);
  const [visible, setVisible] = React.useState(false);
  const enterTimerRef = React.useRef<ReturnType<typeof setTimeout> | null>(null);
  const exitTimerRef = React.useRef<ReturnType<typeof setTimeout> | null>(null);

  React.useEffect(() => {
    if (open) {
      if (exitTimerRef.current) clearTimeout(exitTimerRef.current);
      setPresent(true);
      return;
    }

    setVisible(false);
    if (present) {
      exitTimerRef.current = setTimeout(() => setPresent(false), exitDuration);
    }
  }, [exitDuration, open, present]);

  React.useLayoutEffect(() => {
    if (!open || !present) return;
    enterTimerRef.current = setTimeout(() => setVisible(true), 20);
    return () => {
      if (enterTimerRef.current) clearTimeout(enterTimerRef.current);
    };
  }, [open, present]);

  React.useEffect(() => {
    return () => {
      if (enterTimerRef.current) clearTimeout(enterTimerRef.current);
      if (exitTimerRef.current) clearTimeout(exitTimerRef.current);
    };
  }, []);

  return { present, visible };
}

/**
 * Returns a stable callback that always calls the latest callback reference
 * without causing effect re-runs.
 */
export function useEventCallback<T extends (...args: any[]) => any>(callback?: T): T {
  const ref = React.useRef<T | undefined>(callback);
  React.useEffect(() => {
    ref.current = callback;
  }, [callback]);
  return React.useCallback((...args: Parameters<T>) => ref.current?.(...args), []) as T;
}

interface ModalScope {
  ref: React.RefObject<HTMLElement | null>;
}
const ModalContext = React.createContext<ModalScope[]>([]);

/** Preserves logical modal ancestry across React portals. */
export function ModalLayerScope({ layerRef, children }: {
  layerRef: React.RefObject<HTMLElement | null>;
  children: React.ReactNode;
}) {
  const ancestors = React.useContext(ModalContext);
  const value = React.useMemo(() => [...ancestors, { ref: layerRef }], [ancestors, layerRef]);
  return React.createElement(ModalContext.Provider, { value }, children);
}

const modalBranches = new WeakMap<HTMLElement, Set<HTMLElement>>();

/** Attach to a portaled widget owned by the nearest modal. */
export function useModalBranch<T extends HTMLElement>() {
  const ancestors = React.useContext(ModalContext);
  const ownerRef = ancestors.at(-1)?.ref;
  const [node, setNode] = React.useState<T | null>(null);
  const ref = React.useCallback((element: T | null) => {
    if (ownerRef) setNode(element);
  }, [ownerRef]);
  React.useLayoutEffect(() => {
    const owner = ownerRef?.current;
    if (!owner || !node) return;
    const branches = modalBranches.get(owner) ?? new Set<HTMLElement>();
    branches.add(node);
    modalBranches.set(owner, branches);
    node.inert = owner.inert;
    const overlay = owner.closest<HTMLElement>('[data-bleecker-modal-overlay]');
    const surface = node.closest<HTMLElement>('[data-radix-popper-content-wrapper]') ?? node;
    if (overlay) surface.style.zIndex = String(Number(overlay.style.zIndex) + 1);
    const stack = modalStacks.get(owner.ownerDocument);
    if (stack) updateModalStack(stack);
    return () => {
      branches.delete(node);
      const current = modalStacks.get(owner.ownerDocument);
      if (current) updateModalStack(current);
    };
  }, [node, ownerRef]);
  return ref;
}

function focusableElements(layer: HTMLElement) {
  const doc = layer.ownerDocument;
  const view = doc.defaultView;
  if (!view) return [];
  const roots = [layer, ...modalBranches.get(layer) ?? []];
  const candidates = roots.flatMap((root) => Array.from(root.querySelectorAll<HTMLElement>('*'))).filter((element) => {
    const editableHost = element.isContentEditable && !element.parentElement?.isContentEditable;
    const sequential = element.tabIndex >= 0 || (editableHost && !element.hasAttribute('tabindex'));
    return sequential && !element.matches(':disabled') &&
      !element.closest('[hidden], [inert], [aria-hidden="true"]') &&
      element.getClientRects().length > 0 && view.getComputedStyle(element).visibility === 'visible';
  });
  return [...new Set(candidates)].filter((element) => {
    if (!element.matches('input[type="radio"]') || !(element as HTMLInputElement).name) return true;
    const input = element as HTMLInputElement;
    const group = candidates.filter((candidate) => candidate.matches('input[type="radio"]') &&
      (candidate as HTMLInputElement).name === input.name && (candidate as HTMLInputElement).form === input.form) as HTMLInputElement[];
    return input === (group.find((radio) => radio.checked) ?? group[0]);
  }).sort((a, b) => {
    const aIndex = a.tabIndex > 0 ? a.tabIndex : Infinity;
    const bIndex = b.tabIndex > 0 ? b.tabIndex : Infinity;
    if (aIndex !== bIndex) return aIndex < bIndex ? -1 : 1;
    return a.compareDocumentPosition(b) & view.Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
  });
}

interface ModalEntry {
  layer: HTMLElement;
  ancestors: ModalScope[];
  previousFocus: HTMLElement | null;
  order: number;
}
interface ModalStack {
  doc: Document;
  entries: ModalEntry[];
  overflow: string;
  active: ModalEntry | null;
  sequence: number;
  background: Map<HTMLElement, boolean>;
  observer: MutationObserver | null;
}
const modalStacks = new WeakMap<Document, ModalStack>();

function rootEntry(entry: ModalEntry, stack: ModalStack) {
  return entry.ancestors.map((scope) => stack.entries.find((candidate) => scope.ref.current === candidate.layer)).find((candidate) => candidate !== undefined) ?? entry;
}

function updateModalStack(stack: ModalStack) {
  for (const [element, inert] of stack.background) element.inert = inert;
  stack.background.clear();
  stack.entries.sort((a, b) => rootEntry(a, stack).order - rootEntry(b, stack).order || a.ancestors.length - b.ancestors.length || a.order - b.order);
  const active = stack.entries.at(-1) ?? null;
  for (const [index, entry] of stack.entries.entries()) {
    entry.layer.inert = entry !== active;
    const overlay = entry.layer.closest<HTMLElement>('[data-bleecker-modal-overlay]');
    if (overlay) overlay.style.zIndex = String(50 + index);
    for (const branch of modalBranches.get(entry.layer) ?? []) {
      branch.inert = entry !== active;
      const surface = branch.closest<HTMLElement>('[data-radix-popper-content-wrapper]') ?? branch;
      surface.style.zIndex = String(51 + index);
    }
  }
  stack.active = active;
  if (active) {
    const roots = [active.layer.closest<HTMLElement>('[data-bleecker-modal-overlay]') ?? active.layer,
      ...Array.from(modalBranches.get(active.layer) ?? []).map((branch) => branch.closest<HTMLElement>('[data-radix-popper-content-wrapper]') ?? branch)];
    for (const child of Array.from(stack.doc.body.children) as HTMLElement[]) {
      const allowed = roots.some((root) => child === root || child.contains(root));
      if (!allowed) stack.background.set(child, child.inert);
      child.inert = !allowed;
    }
  }
}

function containsModalTarget(layer: HTMLElement, target: HTMLElement) {
  return layer.contains(target) || Array.from(modalBranches.get(layer) ?? []).some((branch) => branch.contains(target));
}

function focusFirst(entry: ModalEntry) {
  (focusableElements(entry.layer)[0] ?? entry.layer).focus({ preventScroll: true });
}

/**
 * All Bleecker modal surfaces share attachment, ancestry, focus, keyboard, and
 * scroll ownership. The returned object ref remains the public hook contract.
 */
export function useModalLayer<T extends HTMLElement>(open: boolean, onDismiss: () => void) {
  const [layer, setLayer] = React.useState<T | null>(null);
  const layerRef = React.useRef<T>(null);
  const ancestors = React.useContext(ModalContext);
  const snapshot = React.useRef<{ layer: T | null; open: boolean; focus: HTMLElement | null }>({ layer: null, open: false, focus: null });
  const dismiss = useEventCallback(onDismiss);

  React.useLayoutEffect(() => {
    const node = layerRef.current;
    if (node && open && (!snapshot.current.open || snapshot.current.layer !== node)) {
      const active = node.ownerDocument.activeElement;
      const constructor = node.ownerDocument.defaultView?.HTMLElement;
      snapshot.current.focus = constructor && active instanceof constructor ? active as HTMLElement : null;
    }
    snapshot.current.layer = node;
    snapshot.current.open = open;
    if (node) {
      if (!open) node.inert = true;
      const stack = modalStacks.get(node.ownerDocument);
      if (stack && open) updateModalStack(stack);
    }
    setLayer(node);
  });

  React.useEffect(() => {
    if (!open || !layer) return;
    const doc = layer.ownerDocument;
    let stack = modalStacks.get(doc);
    if (!stack) {
      stack = { doc, entries: [], overflow: doc.body.style.overflow, active: null, sequence: 0, background: new Map(), observer: null };
      modalStacks.set(doc, stack);
      doc.body.style.overflow = 'hidden';
      const observer = doc.defaultView?.MutationObserver;
      if (observer) {
        const currentStack = stack;
        stack.observer = new observer(() => { updateModalStack(currentStack); });
        stack.observer.observe(doc.body, { childList: true });
      }
    }
    const entry: ModalEntry = { layer, ancestors, previousFocus: snapshot.current.focus, order: ++stack.sequence };
    stack.entries.push(entry);
    updateModalStack(stack);
    // All simultaneously mounted descendants register before focus is chosen.
    queueMicrotask(() => {
      if (modalStacks.get(doc) === stack && stack.active === entry) focusFirst(entry);
    });
    const handleKeyDown = (event: KeyboardEvent) => {
      if (stack.active !== entry || event.defaultPrevented) return;
      if (event.key === 'Escape') {
        event.preventDefault();
        dismiss();
        return;
      }
      if (event.key !== 'Tab') return;
      const focusable = focusableElements(layer);
      const first = focusable[0];
      const last = focusable.at(-1);
      if (!first || !last) {
        event.preventDefault();
        layer.focus({ preventScroll: true });
      } else if (!focusable.includes(doc.activeElement as HTMLElement)) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus({ preventScroll: true });
      } else if (event.shiftKey && doc.activeElement === first) {
        event.preventDefault();
        last.focus({ preventScroll: true });
      } else if (!event.shiftKey && doc.activeElement === last) {
        event.preventDefault();
        first.focus({ preventScroll: true });
      } else {
        // Positive tabindex participates in the document's global order. Move
        // through the modal's sequential order so background stops are skipped.
        event.preventDefault();
        const index = focusable.indexOf(doc.activeElement as HTMLElement);
        focusable[index + (event.shiftKey ? -1 : 1)].focus({ preventScroll: true });
      }
    };
    doc.addEventListener('keydown', handleKeyDown);
    return () => {
      doc.removeEventListener('keydown', handleKeyDown);
      const wasActive = stack.active === entry;
      for (const successor of stack.entries) {
        if (successor.previousFocus && containsModalTarget(layer, successor.previousFocus)) successor.previousFocus = entry.previousFocus;
      }
      stack.entries.splice(stack.entries.indexOf(entry), 1);
      layer.inert = true;
      updateModalStack(stack);
      const parent = stack.active;
      if (!parent) {
        stack.observer?.disconnect();
        doc.body.style.overflow = stack.overflow;
        modalStacks.delete(doc);
      }
      const overlay = layer.closest<HTMLElement>('[data-bleecker-modal-overlay]');
      if (overlay) overlay.inert = true;
      if (!wasActive) return;
      const previous = entry.previousFocus;
      if (previous?.isConnected && !previous.closest('[inert]') && (!parent || containsModalTarget(parent.layer, previous))) {
        previous.focus({ preventScroll: true });
      } else if (parent) {
        focusFirst(parent);
      }
    };
  }, [ancestors, dismiss, layer, open]);
  return layerRef;
}
