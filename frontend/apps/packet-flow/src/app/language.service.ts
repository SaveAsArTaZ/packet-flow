import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type SupportedLang = 'en' | 'fr' | 'fa';

const LANG_KEY = 'packet-flow-lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private translate = inject(TranslateService);

  readonly currentLang = signal<SupportedLang>('en');
  readonly available = [
    { code: 'en' as const, label: 'English', dir: 'ltr' as const },
    { code: 'fr' as const, label: 'Français', dir: 'ltr' as const },
    { code: 'fa' as const, label: 'فارسی', dir: 'rtl' as const },
  ];

  constructor() {
    const saved = localStorage.getItem(LANG_KEY) as SupportedLang | null;
    const browserLang = navigator.language?.split('-')[0] as SupportedLang;
    const validLangs: SupportedLang[] = ['en', 'fr', 'fa'];
    const lang = saved && validLangs.includes(saved) ? saved
      : validLangs.includes(browserLang) ? browserLang
      : 'en';

    this.translate.use(lang);
    this.currentLang.set(lang);
    this.applyDir(lang);
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
