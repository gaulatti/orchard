import type { Meta, StoryObj } from '@storybook/react-vite';

import { FileInput } from '../../src/components/file-input';

const meta = {
  component: FileInput,
  title: 'Components/FileInput',
  parameters: {
    layout: 'centered'
  }
} satisfies Meta<typeof FileInput>;

export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <div className='w-96'>
      <FileInput {...args} accept='.m3u,.m3u8,image/*' />
    </div>
  )
};

export const ImagesOnly: Story = {
  render: (args) => (
    <div className='w-96'>
      <FileInput {...args} accept='image/png,image/jpeg' />
    </div>
  )
};
