import React from 'react';

import { Button } from './components/button';
import { Card } from './components/card';
import { Field } from './components/field';
import { Input } from './components/input';
import { AuthShell } from './layout/auth-shell';

export interface LoginScreenProps {
  brand?: React.ReactNode;
  description?: string;
  forgotPasswordHref?: string;
  onSubmit?: React.FormEventHandler<HTMLFormElement>;
  signUpHref?: string;
  title?: string;
}

export function LoginScreen({
  brand,
  description = 'Sign in to your account to continue.',
  forgotPasswordHref = '#',
  onSubmit,
  signUpHref = '#',
  title = 'Welcome back'
}: LoginScreenProps) {
  return (
    <AuthShell brand={brand} layout='centered'>
      <Card variant='elevated' className='w-full space-y-7 p-7 sm:p-8'>
        <header className='space-y-2 text-center'>
          <h1 className='text-balance text-3xl font-semibold tracking-refined text-text-primary'>{title}</h1>
          <p className='font-secondary text-pretty text-sm leading-6 text-text-secondary'>{description}</p>
        </header>

        <form
          className='space-y-5'
          onSubmit={(event) => {
            if (!onSubmit) event.preventDefault();
            onSubmit?.(event);
          }}
        >
          <Field label='Email address' required>
            <Input id='email' name='email' type='email' autoComplete='email' placeholder='name@example.com' required />
          </Field>

          <Field
            label='Password'
            required
            action={
              <a href={forgotPasswordHref} className='text-xs font-medium text-sea underline-offset-4 hover:underline dark:text-accent-blue'>
                Forgot password?
              </a>
            }
          >
            <Input id='password' name='password' type='password' autoComplete='current-password' required />
          </Field>

          <Button fullWidth size='lg' type='submit'>Sign in</Button>
        </form>

        <p className='font-secondary text-center text-sm text-text-secondary'>
          Don&apos;t have an account?{' '}
          <a href={signUpHref} className='font-medium text-sea underline-offset-4 hover:underline dark:text-accent-blue'>Sign up</a>
        </p>
      </Card>
    </AuthShell>
  );
}
