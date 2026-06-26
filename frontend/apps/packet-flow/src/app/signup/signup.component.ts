import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../auth.service';

@Component({
  selector: 'pf-signup',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './signup.component.html',
})
export class SignupComponent {
  private fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private router = inject(Router);

  form = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  serverError = '';

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { username, email, password, confirmPassword, firstName, lastName } =
      this.form.getRawValue();

    if (password !== confirmPassword) {
      this.form.controls.confirmPassword.setErrors({ mismatch: true });
      this.form.controls.confirmPassword.markAsTouched();
      return;
    }

    this.serverError = '';
    this.auth
      .register(username, email, password, confirmPassword, firstName || undefined, lastName || undefined)
      .subscribe({
        next: () => this.router.navigate(['/dashboard']),
        error: (err: Error) => {
          this.serverError = err.message || 'Registration failed';
        },
      });
  }
}
