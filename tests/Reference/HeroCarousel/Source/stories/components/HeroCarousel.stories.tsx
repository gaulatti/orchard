import type { Meta, StoryObj } from '@storybook/react-vite';

import { HeroCarousel, type HeroCarouselItem } from '../../src/components/hero-carousel';

const items: HeroCarouselItem[] = [
  {
    id: 'sanremo',
    title: 'Sanremo Festival Coverage',
    description: 'Executive production and live operations leadership across Italy, Spain, and Chile under sustained broadcast pressure.',
    href: 'https://gaulatti.com/success-cases/sanremo',
    kind: 'project',
    tags: ['Live Ops', 'Broadcast', 'Editorial'],
    imageUrl: 'https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?auto=format&fit=crop&w=1600&q=80'
  },
  {
    id: 'fifthbell',
    title: 'fifthbell Media Systems',
    description: 'A personal media infrastructure platform powering live workflows, content automation, and real-time newsroom monitoring.',
    href: 'https://gaulatti.com/success-cases/fifthbell',
    kind: 'project',
    tags: ['AWS', 'NestJS', 'Streaming'],
    imageUrl: 'https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1600&q=80'
  },
  {
    id: 'note',
    title: 'Building Better Broadcast Tooling',
    description: 'A practical write-up on balancing reliability, operator ergonomics, and speed in live production systems.',
    href: 'https://blog.gaulatti.com',
    kind: 'post',
    external: true,
    imageUrl: 'https://images.unsplash.com/photo-1432888622747-4eb9a8efeb07?auto=format&fit=crop&w=1600&q=80'
  }
];

const meta = {
  component: HeroCarousel,
  title: 'Components/HeroCarousel',
  args: {
    items
  },
  argTypes: {
    renderLink: {
      control: false
    }
  },
  parameters: {
    layout: 'fullscreen'
  }
} satisfies Meta<typeof HeroCarousel>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => <HeroCarousel {...args} />
};

export const WithoutIndicators: Story = {
  args: {
    showIndicators: false
  },
  render: (args) => <HeroCarousel {...args} />
};
