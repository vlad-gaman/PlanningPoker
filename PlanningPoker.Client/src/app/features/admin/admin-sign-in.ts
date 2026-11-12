import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminSessionService } from '../../core/admin-session.service';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { ERROR_CODES } from '../../models/api';

type Step = 'password' | 'code';

/** Operator sign-in: username and password, then (when an authenticator is set up) a separate screen for its code. */
@Component({
  selector: 'app-admin-sign-in',
  imports: [ReactiveFormsModule],
  templateUrl: './admin-sign-in.html',
  styleUrl: './admin-sign-in.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSignIn implements OnDestroy {
  private readonly session = inject(AdminSessionService);
  private readonly router = inject(Router);
  private readonly builder = inject(FormBuilder);

  protected readonly flash = inject(FlashMessageService);
  protected readonly step = signal<Step>('password');
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected readonly passwordForm = this.builder.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });
  protected readonly codeForm = this.builder.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\s*(\d\s*){6}$/)]],
  });

  private challenge = '';

  ngOnDestroy(): void {
    this.flash.clear();
  }

  protected async submitPassword(): Promise<void> {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    await this.run(async () => {
      const { username, password } = this.passwordForm.getRawValue();
      const outcome = await this.session.signIn(username, password);
      this.passwordForm.controls.password.reset('');
      if (outcome.kind === 'code') {
        this.challenge = outcome.challenge;
        this.step.set('code');
        return;
      }
      await this.router.navigateByUrl('/admin');
    });
  }

  protected async submitCode(): Promise<void> {
    if (this.codeForm.invalid) {
      this.codeForm.markAllAsTouched();
      return;
    }
    await this.run(async () => {
      try {
        await this.session.completeCode(this.challenge, this.codeForm.getRawValue().code);
      } catch (error) {
        this.codeForm.reset();
        // The password step is too old (or not genuine): the only way on is to start again.
        if (error instanceof ApiRequestError && error.hasCode(ERROR_CODES.adminLoginExpired)) {
          this.backToPassword(errorMessage(error));
          return;
        }
        throw error;
      }
      await this.router.navigateByUrl('/admin');
    });
  }

  protected back(): void {
    this.backToPassword(null);
  }

  private backToPassword(message: string | null): void {
    this.challenge = '';
    this.codeForm.reset();
    this.step.set('password');
    this.error.set(message);
  }

  /** Runs a step with the buttons disabled, and shows the server's message when it fails. */
  private async run(action: () => Promise<void>): Promise<void> {
    this.error.set(null);
    this.flash.clear();
    this.busy.set(true);
    try {
      await action();
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
