import React from 'react';
import type { Meta, StoryObj } from '@storybook/react-vite';

import { Sonner, toast } from '../../src/components/sonner';
import { Button } from '../../src/components/button';

const meta = {
  component: Sonner,
  title: 'Components/Sonner',
  parameters: { layout: 'padded' },
  decorators: [
    (Story: React.ComponentType) => (
      <>
        <Story />
        <Sonner position='bottom-right' duration={4000} />
      </>
    )
  ]
} satisfies Meta<typeof Sonner>;

export default meta;
type Story = StoryObj<typeof meta>;

export const AllVariants: Story = {
  render: () => (
    <div className='flex flex-wrap gap-3'>
      <Button onClick={() => toast.show({ title: 'Default notification', description: 'Something happened.' })}>Default</Button>
      <Button variant='secondary' onClick={() => toast.success('Saved successfully', { description: 'Your changes have been saved.' })}>
        Success
      </Button>
      <Button variant='secondary' onClick={() => toast.error('An error occurred', { description: 'Please try again later.' })}>
        Error
      </Button>
      <Button variant='secondary' onClick={() => toast.warning('Heads up', { description: 'This action may have side effects.' })}>
        Warning
      </Button>
      <Button variant='secondary' onClick={() => toast.info('Did you know?', { description: 'You can undo this action within 5 seconds.' })}>
        Info
      </Button>
    </div>
  )
};

export const WithLinkAction: Story = {
  render: () => (
    <Button
      onClick={() =>
        toast.show({
          title: 'Message deleted',
          description: 'The message has been moved to trash.',
          action: { label: 'Undo', variant: 'link', onClick: () => toast.info('Restored') }
        })
      }
    >
      Delete with link action
    </Button>
  )
};

export const WithDefaultAction: Story = {
  render: () => (
    <Button
      onClick={() =>
        toast.success('Ready to continue', {
          description: 'Omitting an action variant uses the primary button.',
          action: { label: 'Continue', onClick: () => toast.info('Continuing') }
        })
      }
    >
      Show default action
    </Button>
  )
};

export const WithGhostAction: Story = {
  render: () => (
    <Button
      onClick={() =>
        toast.info('Continue browsing?', {
          description: 'Return to the beginning of the list.',
          duration: 0,
          action: { label: 'Back to top', variant: 'ghost', onClick: () => toast.info('Returned to top') }
        })
      }
    >
      Show ghost action
    </Button>
  )
};

export const WithButtonActions: Story = {
  render: () => (
    <div className='flex flex-wrap gap-3'>
      {(['primary', 'secondary', 'ghost', 'destructive'] as const).map((variant) => (
        <Button
          key={variant}
          variant='secondary'
          onClick={() =>
            toast.show({
              title: `${variant} action`,
              description: 'Sonner actions use standard button variants.',
              action: { label: 'Proceed', variant, onClick: () => toast.success('Done') }
            })
          }
        >
          {variant}
        </Button>
      ))}
    </div>
  )
};

export const Persistent: Story = {
  render: () => (
    <Button
      onClick={() =>
        toast.show({
          title: 'Processing upload...',
          description: 'Please wait while your file is uploaded.',
          duration: 0,
          variant: 'info'
        })
      }
    >
      Show persistent toast
    </Button>
  )
};
