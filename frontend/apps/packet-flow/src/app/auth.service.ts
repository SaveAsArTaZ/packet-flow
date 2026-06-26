import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, map } from 'rxjs';
import { ApiService } from './api.service';

export interface UserInfo {
  id: string;
  username: string;
  email: string;
  emailVerified: boolean;
  firstName?: string;
  lastName?: string;
  avatarUrl?: string;
  roles: string[];
  mfaEnabled: boolean;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
  user: UserInfo;
}

interface LoginRequest {
  usernameOrEmail: string;
  password: string;
  rememberMe: boolean;
}

interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
}

const TOKEN_KEY = 'pf_access_token';
const USER_KEY = 'pf_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  readonly currentUser = signal<UserInfo | null>(null);
  readonly isAuthenticated = signal(false);
  readonly isLoading = signal(false);

  constructor() {
    this.restoreSession();
  }

  private restoreSession(): void {
    const token = localStorage.getItem(TOKEN_KEY);
    const userJson = localStorage.getItem(USER_KEY);
    if (token && userJson) {
      try {
        const user = JSON.parse(userJson) as UserInfo;
        this.currentUser.set(user);
        this.isAuthenticated.set(true);
      } catch {
        this.clearSession();
      }
    }
  }

  login(usernameOrEmail: string, password: string, rememberMe = false): Observable<UserInfo> {
    this.isLoading.set(true);
    const body: LoginRequest = { usernameOrEmail, password, rememberMe };
    return this.api.post<TokenResponse>('/auth/login', body).pipe(
      tap({
        next: (res) => this.persistSession(res),
        error: () => this.isLoading.set(false),
      }),
      map((res) => res.user),
    );
  }

  register(
    username: string,
    email: string,
    password: string,
    confirmPassword: string,
    firstName?: string,
    lastName?: string,
  ): Observable<UserInfo> {
    this.isLoading.set(true);
    const body: RegisterRequest = { username, email, password, confirmPassword, firstName, lastName };
    return this.api.post<TokenResponse>('/auth/register', body).pipe(
      tap({
        next: (res) => this.persistSession(res),
        error: () => this.isLoading.set(false),
      }),
      map((res) => res.user),
    );
  }

  logout(): void {
    this.api.post('/auth/logout', {}).subscribe({
      next: () => {/* ok */},
      error: () => {/* proceed regardless */},
      complete: () => this.clearSessionAndRedirect(),
    });
  }

  private persistSession(res: TokenResponse): void {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.currentUser.set(res.user);
    this.isAuthenticated.set(true);
    this.isLoading.set(false);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }

  private clearSessionAndRedirect(): void {
    this.clearSession();
    this.router.navigate(['/']);
  }

  /** Returns an Authorization header if the user has a token */
  getAuthHeaders(): Record<string, string> {
    const token = localStorage.getItem(TOKEN_KEY);
    return token ? { Authorization: `Bearer ${token}` } : {};
  }
}
