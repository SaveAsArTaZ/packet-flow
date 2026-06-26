import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'packet-flow-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly isDark = signal(false);

  constructor() {
    const stored = localStorage.getItem(STORAGE_KEY);
    const prefersDark = stored
      ? stored === 'dark'
      : typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.isDark.set(prefersDark);
    this.apply();
  }

  toggle(): void {
    this.isDark.update((v) => !v);
    this.apply();
    localStorage.setItem(STORAGE_KEY, this.isDark() ? 'dark' : 'light');
  }

  private apply(): void {
    document.documentElement.classList.toggle('dark', this.isDark());
  }
}
