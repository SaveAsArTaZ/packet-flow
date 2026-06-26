import { Component, inject, AfterViewInit, ElementRef, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { TablerIconComponent } from '@tabler/icons-angular';
import { TypewriterComponent } from '../typewriter/typewriter.component';
import { AuthService } from '../auth.service';
import gsap from 'gsap';

@Component({
  selector: 'pf-signup',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, TranslatePipe, TablerIconComponent, TypewriterComponent],
  templateUrl: './signup.component.html',
})
export class SignupComponent implements AfterViewInit {
  private fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private router = inject(Router);
  @ViewChild('formEl', { static: true }) formEl!: ElementRef<HTMLFormElement>;

  form = this.fb.nonNullable.group({
    firstName: [''], lastName: [''],
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  serverError = signal('');
  subtitleDone = signal(false);
  showPassword = signal(false);
  showConfirm = signal(false);
  private animated = false;

  ngAfterViewInit(): void {}

  onSubtitleDone(): void {
    if (this.animated) return;
    this.animated = true; this.subtitleDone.set(true);
    requestAnimationFrame(() => {
      const fields = this.formEl.nativeElement.querySelectorAll<HTMLElement>('.form-field, .form-btn, .form-footer');
      gsap.fromTo(fields, { opacity: 0, y: 20 }, { opacity: 1, y: 0, duration: 0.5, stagger: 0.06, ease: 'power2.out' });
    });
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const { username, email, password, confirmPassword, firstName, lastName } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.form.controls.confirmPassword.setErrors({ mismatch: true });
      this.form.controls.confirmPassword.markAsTouched();
      return;
    }
    this.serverError.set('');
    this.auth.register(username, email, password, confirmPassword, firstName || undefined, lastName || undefined).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err: Error) => this.serverError.set(err.message || 'Registration failed'),
    });
  }
}
