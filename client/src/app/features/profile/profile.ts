import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { HouseholdMemberSummary, HouseholdService, HouseholdSummary } from '../../core/services/household.service';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;
  return newPassword === confirmPassword ? null : { mismatch: true };
}

@Component({
  selector: 'app-profile',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly householdApi = inject(HouseholdService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly household = signal<HouseholdSummary | null>(null);

  readonly nameForm = this.fb.nonNullable.group({
    displayName: [this.auth.currentUser()?.displayName ?? '', Validators.required],
  });
  readonly nameSubmitting = signal(false);
  readonly nameSuccess = signal(false);
  readonly nameError = signal<string | null>(null);

  readonly emailForm = this.fb.nonNullable.group({
    newEmail: [this.auth.currentUser()?.email ?? '', [Validators.required, Validators.email]],
    currentPassword: ['', Validators.required],
  });
  readonly emailSubmitting = signal(false);
  readonly emailSuccess = signal(false);
  readonly emailError = signal<string | null>(null);

  readonly passwordForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatchValidator },
  );
  readonly passwordSubmitting = signal(false);
  readonly passwordSuccess = signal(false);
  readonly passwordError = signal<string | null>(null);

  readonly inviteForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });
  readonly inviteSubmitting = signal(false);
  readonly inviteSuccess = signal(false);
  readonly inviteError = signal<string | null>(null);

  ngOnInit(): void {
    this.householdApi.getMyHousehold().subscribe((h) => this.household.set(h));
  }

  memberStatusLabel(member: HouseholdMemberSummary): string {
    return member.status;
  }

  saveName(): void {
    if (this.nameForm.invalid || this.nameSubmitting()) return;
    this.nameSubmitting.set(true);
    this.nameSuccess.set(false);
    this.nameError.set(null);

    this.auth.updateProfile(this.nameForm.getRawValue().displayName).subscribe({
      next: () => {
        this.nameSubmitting.set(false);
        this.nameSuccess.set(true);
      },
      error: (err) => {
        this.nameSubmitting.set(false);
        this.nameError.set(firstError(err) ?? 'Could not update your name.');
      },
    });
  }

  saveEmail(): void {
    if (this.emailForm.invalid || this.emailSubmitting()) return;
    this.emailSubmitting.set(true);
    this.emailSuccess.set(false);
    this.emailError.set(null);

    const { newEmail, currentPassword } = this.emailForm.getRawValue();
    this.auth.updateEmail(newEmail, currentPassword).subscribe({
      next: () => {
        this.emailSubmitting.set(false);
        this.emailSuccess.set(true);
        this.emailForm.patchValue({ currentPassword: '' });
      },
      error: (err) => {
        this.emailSubmitting.set(false);
        this.emailError.set(firstError(err) ?? 'Could not update your email.');
      },
    });
  }

  savePassword(): void {
    if (this.passwordForm.invalid || this.passwordSubmitting()) return;
    if (this.passwordForm.errors?.['mismatch']) {
      this.passwordError.set('New password and confirmation do not match.');
      return;
    }

    this.passwordSubmitting.set(true);
    this.passwordSuccess.set(false);
    this.passwordError.set(null);

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordSubmitting.set(false);
        this.passwordSuccess.set(true);
        this.passwordForm.reset();
      },
      error: (err) => {
        this.passwordSubmitting.set(false);
        this.passwordError.set(firstError(err) ?? 'Could not change your password.');
      },
    });
  }

  sendInvite(): void {
    if (this.inviteForm.invalid || this.inviteSubmitting()) return;
    this.inviteSubmitting.set(true);
    this.inviteSuccess.set(false);
    this.inviteError.set(null);

    this.householdApi.invite(this.inviteForm.getRawValue().email).subscribe({
      next: () => {
        this.inviteSubmitting.set(false);
        this.inviteSuccess.set(true);
        this.inviteForm.reset();
        this.householdApi.getMyHousehold().subscribe((h) => this.household.set(h));
      },
      error: (err) => {
        this.inviteSubmitting.set(false);
        this.inviteError.set(firstError(err) ?? 'Could not send that invite.');
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}

function firstError(err: unknown): string | null {
  const errors = (err as { error?: { errors?: string[]; error?: string } })?.error;
  return errors?.errors?.[0] ?? errors?.error ?? null;
}
