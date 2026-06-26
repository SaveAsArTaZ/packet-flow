import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  APP_INITIALIZER,
  inject,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { provideTablerIcons } from '@tabler/icons-angular';
import { IconSun, IconMoon, IconEye, IconEyeOff } from '@tabler/icons-angular';
import { LanguageService } from './language.service';
import { appRoutes } from './app.routes';

function preloadTranslations(): () => Promise<void> {
  const translate = inject(TranslateService);
  const langService = inject(LanguageService);
  const lang = langService.currentLang();
  return () =>
    new Promise<void>((resolve) => {
      translate.use(lang).subscribe({
        next: () => resolve(),
        error: () => resolve(),
      });
    });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(appRoutes),
    provideHttpClient(withFetch()),
    provideTablerIcons({ IconSun, IconMoon, IconEye, IconEyeOff }),
    provideTranslateService(),
    ...provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' }),
    {
      provide: APP_INITIALIZER,
      useFactory: preloadTranslations,
      multi: true,
      deps: [TranslateService, LanguageService],
    },
  ],
};
