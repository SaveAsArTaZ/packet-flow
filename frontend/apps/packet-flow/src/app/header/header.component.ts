import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ThemeService } from '../theme.service';
import { LanguageService, SupportedLang } from '../language.service';
import { AuthService } from '../auth.service';

@Component({
  selector: 'pf-header',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  readonly theme = inject(ThemeService);
  readonly lang = inject(LanguageService);
  readonly auth = inject(AuthService);

  toggleTheme(): void {
    this.theme.toggle();
  }

  switchLang(lang: SupportedLang): void {
    this.lang.switchLang(lang);
  }
}
