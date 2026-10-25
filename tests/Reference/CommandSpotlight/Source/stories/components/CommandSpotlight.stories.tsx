import { Calendar, Command, LayoutGrid, Radio, Search, Settings, Tv } from 'lucide-react';
import type { Meta, StoryObj } from '@storybook/react-vite';
import React from 'react';

import { CommandSpotlight, type CommandSpotlightAction } from '../../src/components/command-spotlight';
import { Kbd } from '../../src/components/kbd';

const sampleActions: CommandSpotlightAction[] = [
  {
    id: 'go-dashboard',
    title: 'Go to Dashboard',
    description: 'Jump to the main workspace',
    group: 'Navigation',
    icon: <LayoutGrid size={16} />
  },
  {
    id: 'go-programs',
    title: 'Go to Programs',
    description: 'Manage global program slots',
    group: 'Navigation',
    icon: <Tv size={16} />
  },
  {
    id: 'open-live',
    title: 'Open Live Program',
    description: 'Launch the current playout output in a new tab',
    group: 'Program',
    icon: <Radio size={16} />
  },
  {
    id: 'switch-main',
    title: 'Select Program: main',
    description: 'Set main as the active global program',
    group: 'Program',
    icon: <Command size={16} />,
    shortcut: ['1']
  },
  {
    id: 'switch-fifthbell',
    title: 'Select Program: fifthbell',
    description: 'Set fifthbell as the active global program',
    group: 'Program',
    icon: <Command size={16} />,
    shortcut: ['2']
  },
  {
    id: 'open-layouts',
    title: 'Open Layouts',
    description: 'Edit reusable overlay compositions',
    group: 'Admin',
    icon: <Settings size={16} />
  },
  {
    id: 'open-preview',
    title: 'Open Component Preview',
    description: 'Inspect overlay parts in isolation',
    group: 'Admin',
    icon: <Search size={16} />,
    shortcut: ['P']
  },
  {
    id: 'jump-calendar',
    title: 'Open Rundown Calendar',
    description: 'Review upcoming rundown events',
    group: 'Tools',
    icon: <Calendar size={16} />,
    disabled: true
  }
];

import { MixedModalContract, verifyMixedModals } from './modal-composition-contract';

const meta = {
  component: CommandSpotlight,
  title: 'Components/CommandSpotlight',
  args: {
    actions: sampleActions,
    placeholder: 'Search commands...',
    triggerLabel: 'Spotlight'
  },
  argTypes: {
    onActionSelect: { control: false },
    onOpenChange: { control: false }
  },
  parameters: {
    layout: 'centered'
  }
} satisfies Meta<typeof CommandSpotlight>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => {
    const [lastAction, setLastAction] = React.useState<string>('No command executed yet');

    return (
      <div className='min-h-[22rem] space-y-4'>
        <CommandSpotlight
          {...args}
          actions={sampleActions}
          onActionSelect={(action) => {
            setLastAction(action.title);
          }}
        />
        <p className='text-sm text-text-secondary dark:text-text-secondary'>Last action: {lastAction}</p>
      </div>
    );
  }
};

export const ShortcutOnly: Story = {
  args: {
    showTrigger: false
  },
  render: (args) => (
    <div className='min-h-[14rem]'>
      <p className='text-sm text-text-secondary dark:text-text-secondary'>
        Press <Kbd keys={['Cmd/Ctrl', 'K']} className='mx-1' /> to open.
      </p>
      <CommandSpotlight {...args} actions={sampleActions} />
    </div>
  )
};

export const MixedModalStack: Story = { render: () => <MixedModalContract />, play: async ({ canvasElement }) => verifyMixedModals(canvasElement) };
