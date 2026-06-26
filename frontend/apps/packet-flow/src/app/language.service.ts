import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type SupportedLang = 'en' | 'fr' | 'fa';

const LANG_KEY = 'packet-flow-lang';

/** Resolve the initial language at module load so the signal
 *  has the correct value before any component renders. */
function resolveInitialLang(): SupportedLang {
  const valid: SupportedLang[] = ['en', 'fr', 'fa'];
  try {
    const saved = localStorage.getItem(LANG_KEY) as SupportedLang | null;
    if (saved && valid.includes(saved)) return saved;
  } catch { /* localStorage unavailable */ }

  try {
    const browser = navigator.language?.split('-')[0] as SupportedLang;
    if (valid.includes(browser)) return browser;
  } catch { /* navigator unavailable */ }

  return 'en';
}

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);

  /** Signal initialized at module load — has the right value instantly */
  readonly currentLang = signal<SupportedLang>(resolveInitialLang());

  readonly available = [
    { code: 'en' as const, label: 'English', dir: 'ltr' as const },
    { code: 'fr' as const, label: 'Français', dir: 'ltr' as const },
    { code: 'fa' as const, label: 'فارسی', dir: 'rtl' as const },
  ];

  constructor() {
    // Translations are preloaded by APP_INITIALIZER — just apply dir.
    this.applyDir(this.currentLang());
  }

  switchLang(lang: SupportedLang): void {
    this.translate.use(lang);
    this.currentLang.set(lang);
    localStorage.setItem(LANG_KEY, lang);
    this.applyDir(lang);
  }

  get currentDir(): 'ltr' | 'rtl' {
    return this.available.find(l => l.code === this.currentLang())?.dir ?? 'ltr';
  }

  private applyDir(lang: SupportedLang): void {
    const dir = lang === 'fa' ? 'rtl' : 'ltr';
    document.documentElement.dir = dir;
    document.documentElement.lang = lang;
  }
}
