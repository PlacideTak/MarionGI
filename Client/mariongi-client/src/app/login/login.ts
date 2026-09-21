import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';

import { AuthService } from './auth.service';
import { ApiError, AuthResponse, LoginStep } from './auth.models';
import { MotDePasseOublie } from '../motdepasseoublie/motdepasseoublie';

const MAX_ATTEMPTS = 5;
const LOCKOUT_SECONDS = 60;
const OTP_LENGTH = 6;
const RESEND_COOLDOWN_SECONDS = 30;

function identifierValidator(control: { value: string }) {
  const value = (control.value ?? '').trim();
  if (!value) return null;

  const isEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  const isPhone = /^\+?[0-9\s.-]{8,15}$/.test(value);

  return isEmail || isPhone ? null : { invalidIdentifier: true };
}

function maskPhone(phone: string): string {
  if (!phone) return '';
  const digits = phone.replace(/\s/g, '');
  const start = digits.slice(0, 5);
  const end = digits.slice(-2);
  return `${start}${'•'.repeat(Math.max(digits.length - 7, 4))}${end}`;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    MessageModule,
    MotDePasseOublie
],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly step = signal<LoginStep>('credentials');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly failedAttempts = signal(0);
  readonly lockoutRemaining = signal(0);
  readonly isLockedOut = computed(() => this.lockoutRemaining() > 0);
  private lockoutTimerId: ReturnType<typeof setInterval> | null = null;

  readonly telephone = signal<string | null>(null);
  readonly maskedPhone = computed(() => {
    const phone = this.telephone();
    return phone ? maskPhone(phone) : '';
  });
  readonly otpResendCooldown = signal(0);
  private otpTimerId: ReturnType<typeof setInterval> | null = null;

  readonly devOtpCode = signal<string | null>(null);

  readonly credentialsForm = this.fb.nonNullable.group({
    identifiant: ['', [Validators.required, identifierValidator]],
    motDePasse: ['', [Validators.required, Validators.minLength(4)]],
  });

  readonly otpForm = this.fb.nonNullable.group({
    codeOtp: [
      '',
      [
        Validators.required,
        Validators.pattern(/^\d+$/),
        Validators.minLength(OTP_LENGTH),
        Validators.maxLength(OTP_LENGTH),
      ],
    ],
  });

  canSubmitCredentials(): boolean {
    return (
      this.credentialsForm.valid && !this.isSubmitting() && !this.isLockedOut()
    );
  }

  canSubmitOtp(): boolean {
    return this.otpForm.valid && !this.isSubmitting();
  }

  // ===== Étape 1 : Identifiants =====
  submitCredentials(): void {
    if (!this.canSubmitCredentials()) {
      this.credentialsForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const formVal = this.credentialsForm.getRawValue();

    const payload = {
      Identifiant: formVal.identifiant.trim(),
      MotDePasse: formVal.motDePasse,
    };

    this.authService.login(payload as any).subscribe({
      next: (res) => this.handleAuthResponse(res),
      error: (err: ApiError) => {
        this.isSubmitting.set(false);
        this.handleLoginError(err);
      },
    });
  }

  private handleAuthResponse(res: AuthResponse | any): void {
    this.isSubmitting.set(false);
    this.failedAttempts.set(0);

    // 🎯 RESTAURATION : Vérifie si le backend demande un OTP
    const isOtpRequired = res.requisOtp ?? res.RequisOtp;
    const telephone = res.telephone ?? res.Telephone;
    const codeParSms = res.codeParSms ?? res.CodeParSms;

    if (isOtpRequired) {
      this.telephone.set(telephone ?? this.credentialsForm.getRawValue().identifiant);
      this.devOtpCode.set(codeParSms ?? null);

      if (codeParSms) {
        this.otpForm.patchValue({ codeOtp: codeParSms });
      }

      this.step.set('otp');
      this.startResendCooldown();
      return; // Stoppe ici pour afficher l'écran OTP
    }

    // Sinon, c'est la validation finale : on stocke la session et on redirige
    this.storeSession(res);

    this.router.navigateByUrl('/dashboard').then(success => {
      console.log("Redirection réussie vers /dashboard ?", success);
    });
  }

  private storeSession(res: any): void {
    const token = res.AccessToken || res.accessToken || res.token;
    const refreshToken = res.RefreshToken || res.refreshToken;

    if (token) {
      sessionStorage.setItem('accessToken', token);
    }
    if (refreshToken) {
      sessionStorage.setItem('refreshToken', refreshToken);
    }
  }

  private handleLoginError(err: ApiError): void {
    if (err.status === 429) {
      this.startLockout(err.retryAfterSeconds ?? LOCKOUT_SECONDS);
      this.errorMessage.set(err.message);
      return;
    }

    this.failedAttempts.update((n) => n + 1);
    this.errorMessage.set(err.message);

    if (this.failedAttempts() >= MAX_ATTEMPTS) {
      this.startLockout(LOCKOUT_SECONDS);
      this.failedAttempts.set(0);
    }
  }

  private startLockout(seconds: number): void {
    this.lockoutRemaining.set(seconds);
    if (this.lockoutTimerId) clearInterval(this.lockoutTimerId);

    this.lockoutTimerId = setInterval(() => {
      const remaining = this.lockoutRemaining() - 1;
      if (remaining <= 0) {
        this.lockoutRemaining.set(0);
        if (this.lockoutTimerId) clearInterval(this.lockoutTimerId);
      } else {
        this.lockoutRemaining.set(remaining);
      }
    }, 1000);
  }


// ===== Étape 2 : Code OTP =====
  submitOtp(): void {
    if (!this.canSubmitOtp()) {
      this.otpForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { codeOtp } = this.otpForm.getRawValue();
    const phone = this.telephone() || this.credentialsForm.getRawValue().identifiant.trim();

    const payload = {
      Telephone: phone,
      CodeOtp: codeOtp,
    };

    this.authService.verifyOtp(payload).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.failedAttempts.set(0);

        // 🎯 Stockage direct des tokens reçus à la validation de l'OTP
        this.storeSession(res);

        // 🚀 Redirection forcée vers le dashboard
        this.router.navigateByUrl('/').then(success => {
          console.log("Redirection vers /dashboard réussie :", success);
        });
      },
      error: (err: ApiError) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.message || 'Code OTP invalide ou expiré.');
      },
    });
  }

  resendOtp(): void {
    if (this.otpResendCooldown() > 0) return;

    const formVal = this.credentialsForm.getRawValue();
    const payload = {
      login: formVal.identifiant.trim(),
      motDePasse: formVal.motDePasse,
    };

    this.authService.resendOtp(payload as any).subscribe({
      next: (res) => {
        if (res.telephone) this.telephone.set(res.telephone);
        this.devOtpCode.set(res.codeParSms ?? null);
        if (res.codeParSms) {
          this.otpForm.patchValue({ codeOtp: res.codeParSms });
        }
        this.startResendCooldown();
      },
      error: (err: ApiError) => {
        this.errorMessage.set(err.message);
      },
    });
  }

  private startResendCooldown(): void {
    this.otpResendCooldown.set(RESEND_COOLDOWN_SECONDS);
    if (this.otpTimerId) clearInterval(this.otpTimerId);

    this.otpTimerId = setInterval(() => {
      const remaining = this.otpResendCooldown() - 1;
      if (remaining <= 0) {
        this.otpResendCooldown.set(0);
        if (this.otpTimerId) clearInterval(this.otpTimerId);
      } else {
        this.otpResendCooldown.set(remaining);
      }
    }, 1000);
  }

  backToCredentials(): void {
    this.step.set('credentials');
    this.errorMessage.set(null);
    this.otpForm.reset();
    this.devOtpCode.set(null);
    if (this.otpTimerId) clearInterval(this.otpTimerId);
    this.otpResendCooldown.set(0);
  }
}