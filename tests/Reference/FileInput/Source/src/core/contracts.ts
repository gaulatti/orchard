export const themeModes = ['light', 'dark', 'system'] as const;
export type ThemeMode = (typeof themeModes)[number];

export const buttonVariants = ['primary', 'secondary', 'outline', 'subtle', 'ghost', 'link', 'destructive'] as const;
export type ButtonVariant = (typeof buttonVariants)[number];

export const buttonSizes = ['xs', 'sm', 'md', 'lg'] as const;
export type ButtonSize = (typeof buttonSizes)[number];

export interface ButtonContract {
  disabled?: boolean;
  fullWidth?: boolean;
  loading?: boolean;
  size?: ButtonSize;
  variant?: ButtonVariant;
}

export const controlSizes = ['sm', 'md', 'lg'] as const;
export type ControlSize = (typeof controlSizes)[number];

export const statusBadgeVariants = ['live', 'offline', 'warning', 'info', 'default'] as const;
export type StatusBadgeVariant = (typeof statusBadgeVariants)[number];

export const cardPaddings = ['none', 'sm', 'md', 'lg'] as const;
export type CardPadding = (typeof cardPaddings)[number];

export const cardVariants = ['surface', 'outlined', 'elevated', 'subtle', 'transparent'] as const;
export type CardVariant = (typeof cardVariants)[number];

export const progressSizes = ['sm', 'md', 'lg'] as const;
export type ProgressSize = (typeof progressSizes)[number];

export const progressVariants = ['default', 'success', 'warning', 'destructive'] as const;
export type ProgressVariant = (typeof progressVariants)[number];

export const iconButtonSizes = ['sm', 'md', 'lg'] as const;
export type IconButtonSize = (typeof iconButtonSizes)[number];

export const iconButtonVariants = ['default', 'subtle', 'ghost'] as const;
export type IconButtonVariant = (typeof iconButtonVariants)[number];

export const toggleSizes = ['sm', 'md', 'lg'] as const;
export type ToggleSize = (typeof toggleSizes)[number];

export const toggleVariants = ['default', 'outline'] as const;
export type ToggleVariant = (typeof toggleVariants)[number];

export const alertTypes = ['success', 'error', 'info', 'warning'] as const;
export type AlertType = (typeof alertTypes)[number];

export const metricFormats = ['number', 'currency', 'percent', 'compact'] as const;
export type MetricFormat = (typeof metricFormats)[number];

export const avatarSizes = ['xs', 'sm', 'md', 'lg', 'xl'] as const;
export type AvatarSize = (typeof avatarSizes)[number];

export const loadingSizes = ['sm', 'md', 'lg'] as const;
export type LoadingSize = (typeof loadingSizes)[number];

export const iconBadgeSizes = ['md', 'lg'] as const;
export type IconBadgeSize = (typeof iconBadgeSizes)[number];

export const iconBadgeVariants = ['primary', 'subtle', 'outlined'] as const;
export type IconBadgeVariant = (typeof iconBadgeVariants)[number];

export const selectionOrientations = ['vertical', 'horizontal'] as const;
export type SelectionOrientation = (typeof selectionOrientations)[number];

export function resolveTheme(theme: ThemeMode, prefersDark: boolean): Exclude<ThemeMode, 'system'> {
  return theme === 'system' ? (prefersDark ? 'dark' : 'light') : theme;
}
