import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  APP_INITIALIZER,
  inject,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LanguageService } from './language.service';
import { appRoutes } from './app.routes';

/**
 * Preload translations for the saved / detected language before
 * Angular bootstraps, so the translate pipe never renders raw keys.
 */
function preloadTranslations(): () => Promise<void> {
  const translate = inject(TranslateService);
  const langService = inject(LanguageService);
  const lang = langService.currentLang();
  return () =>
    new Promise<void>((resolve) => {
      translate.use(lang).subscribe({
        next: () => resolve(),
        error: () => resolve(), // resolve anyway so app boots
      });
    });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withFetch()),
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
