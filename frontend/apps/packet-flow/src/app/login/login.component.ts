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
  selector: 'pf-login',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, TranslatePipe, TablerIconComponent, TypewriterComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent implements AfterViewInit {
  private fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private router = inject(Router);

  @ViewChild('formEl', { static: true }) formEl!: ElementRef<HTMLFormElement>;

  form = this.fb.nonNullable.group({
    usernameOrEmail: ['', [Validators.required]],
    password: ['', [Validators.required]],
    rememberMe: [false],
  });

  serverError = signal('');
  subtitleDone = signal(false);
  private animated = false;

  showPassword = signal(false);

  ngAfterViewInit(): void {}

  onSubtitleDone(): void {
    if (this.animated) return;
    this.animated = true;
    this.subtitleDone.set(true);

    requestAnimationFrame(() => {
      const fields = this.formEl.nativeElement.querySelectorAll<HTMLElement>(
        '.form-field, .form-btn, .form-footer',
      );
      gsap.fromTo(fields,
        { opacity: 0, y: 20 },
        { opacity: 1, y: 0, duration: 0.5, stagger: 0.08, ease: 'power2.out' },
      );
    });
  }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.serverError.set('');
    const { usernameOrEmail, password, rememberMe } = this.form.getRawValue();
    this.auth.login(usernameOrEmail, password, rememberMe).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err: Error) => this.serverError.set(err.message || 'Login failed'),
    });
  }
}
