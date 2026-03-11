import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '@core/auth';

const DEMO_PASSWORD = 'LedgerFlow!2026';

@Component({
  selector: 'lf-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly demos = [
    { role: 'AP clerk', email: 'clerk@ledgerflow.dev', icon: 'bi-pencil-square' },
    { role: 'Approver', email: 'approver@ledgerflow.dev', icon: 'bi-check2-circle' },
    { role: 'Controller', email: 'controller@ledgerflow.dev', icon: 'bi-shield-check' },
  ];

  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  useDemo(email: string): void {
    this.form.setValue({ email, password: DEMO_PASSWORD });
    this.submit();
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        void this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : '/dashboard');
      },
      error: err => {
        this.busy.set(false);
        this.error.set(err.status === 429 ? 'Too many attempts. Wait a minute and try again.' : 'Email or password is incorrect.');
      },
    });
  }
}
