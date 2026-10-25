import type { Meta, StoryObj } from '@storybook/react-vite';
import { ArrowRight, LockKeyhole, Mail } from 'lucide-react';

import { Button } from '../../src/components/button';
import { Card } from '../../src/components/card';
import { Checkbox } from '../../src/components/checkbox';
import { Field } from '../../src/components/field';
import { Eyebrow } from '../../src/components/eyebrow';
import { Input } from '../../src/components/input';
import { AuthShell } from '../../src/layout/auth-shell';

const meta = {
  component: AuthShell,
  title: 'Layout/AuthShell',
  tags: ['autodocs'],
  parameters: { layout: 'fullscreen' },
  args: { children: null }
} satisfies Meta<typeof AuthShell>;

export default meta;
type Story = StoryObj<typeof meta>;

function AtelierBrand() {
  return (
    <a href='#' className='inline-flex items-center gap-3 text-text-primary'>
      <span className='grid size-9 place-items-center rounded-full border border-sand/60 text-sm font-semibold'>B</span>
      <span className='text-sm font-semibold tracking-[0.08em]'>BLEECKER ATELIER</span>
    </a>
  );
}

export const SplitSignIn: Story = {
  render: () => (
    <AuthShell
      layout='split'
      side='start'
      asideLabel='Atelier membership note'
      brand={<AtelierBrand />}
      footer={<>By continuing, you agree to our <a className='underline underline-offset-4 hover:text-text-primary' href='#'>terms</a> and <a className='underline underline-offset-4 hover:text-text-primary' href='#'>privacy policy</a>.</>}
      aside={
        <div className='max-w-md space-y-8'>
          <div className='h-px w-12 bg-accent-gold' />
          <blockquote className='text-3xl font-medium leading-[1.25] tracking-refined xl:text-4xl'>
            “A private place for objects, stories, and the people who collect them.”
          </blockquote>
          <div className='font-secondary space-y-1 text-sm text-white/65'>
            <p className='text-white'>Elena Marais</p>
            <p>Curatorial Director</p>
          </div>
        </div>
      }
    >
      <div className='space-y-9'>
        <header className='space-y-3'>
          <Eyebrow>Member access</Eyebrow>
          <h1 className='text-4xl font-semibold tracking-refined'>Welcome back</h1>
          <p className='font-secondary text-sm leading-6 text-text-secondary'>Enter your details to continue to your private collection.</p>
        </header>

        <form className='space-y-5' onSubmit={(event) => event.preventDefault()}>
          <Field label='Email address' required>
            <Input type='email' autoComplete='email' placeholder='name@example.com' startIcon={<Mail className='size-4' aria-hidden='true' />} />
          </Field>
          <Field label='Password' required>
            <Input type='password' autoComplete='current-password' startIcon={<LockKeyhole className='size-4' aria-hidden='true' />} />
          </Field>
          <div className='flex items-center justify-between gap-4'>
            <Checkbox label='Remember me' />
            <a href='#' className='text-xs font-medium text-sea hover:underline dark:text-accent-blue'>Forgot password?</a>
          </div>
          <Button type='submit' fullWidth size='lg'>Sign in <ArrowRight className='size-4' aria-hidden='true' /></Button>
        </form>

        <p className='font-secondary text-center text-sm text-text-secondary'>New to the atelier? <a href='#' className='font-medium text-sea hover:underline dark:text-accent-blue'>Request membership</a></p>
      </div>
    </AuthShell>
  )
};

export const CenteredRecovery: Story = {
  render: () => (
    <AuthShell
      layout='centered'
      brand={<div className='flex justify-center'><AtelierBrand /></div>}
      footer={<p className='text-center'>Need personal assistance? <a href='#' className='underline underline-offset-4 hover:text-text-primary'>Contact the concierge</a></p>}
    >
      <Card variant='elevated' padding='lg' className='space-y-8 sm:p-10'>
        <header className='space-y-3 text-center'>
          <span className='mx-auto grid size-11 place-items-center rounded-full bg-light-sand text-sea dark:bg-white/[0.07] dark:text-accent-blue'>
            <Mail className='size-5' aria-hidden='true' />
          </span>
          <h1 className='text-3xl font-semibold tracking-refined'>Recover access</h1>
          <p className='font-secondary text-sm leading-6 text-text-secondary'>We’ll send a private recovery link to the email associated with your membership.</p>
        </header>

        <form className='space-y-5' onSubmit={(event) => event.preventDefault()}>
          <Field label='Email address' required>
            <Input type='email' autoComplete='email' placeholder='name@example.com' />
          </Field>
          <Button type='submit' fullWidth size='lg'>Send recovery link</Button>
        </form>

        <p className='font-secondary text-center text-sm text-text-secondary'><a href='#' className='font-medium text-sea hover:underline dark:text-accent-blue'>Return to sign in</a></p>
      </Card>
    </AuthShell>
  )
};
