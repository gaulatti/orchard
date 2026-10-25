import type { Meta, StoryObj } from '@storybook/react-vite';

import { NavigationMenu } from '../../src/components/navigation-menu';

const meta = {
  component: NavigationMenu,
  title: 'Components/NavigationMenu',
  parameters: { layout: 'padded' }
} satisfies Meta<typeof NavigationMenu>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    entries: [
      { href: '/', label: 'Home' },
      {
        trigger: 'Components',
        items: [
          { href: '/components/buttons', label: 'Buttons', description: 'Interactive button components with multiple variants.' },
          { href: '/components/forms', label: 'Forms', description: 'Input, select, radio, and other form controls.' },
          { href: '/components/overlays', label: 'Overlays', description: 'Modals, sheets, drawers, and popovers.' }
        ]
      },
      {
        trigger: 'Layout',
        items: [
          { href: '/layout/header', label: 'Header', description: 'Top navigation and brand lockup.' },
          { href: '/layout/footer', label: 'Footer', description: 'Site footer with links and legal.' },
          { href: '/layout/app-shell', label: 'App Shell', description: 'Full-page layout wrapper.' }
        ]
      },
      { href: '/docs', label: 'Documentation' },
      { href: 'https://github.com', label: 'GitHub', external: true }
    ]
  }
};

export const LinksOnly: Story = {
  args: {
    entries: [
      { href: '/', label: 'Home' },
      { href: '/about', label: 'About' },
      { href: '/pricing', label: 'Pricing' },
      { href: '/blog', label: 'Blog' }
    ]
  }
};
