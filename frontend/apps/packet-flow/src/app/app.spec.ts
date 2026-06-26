import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideTranslateService } from '@ngx-translate/core';
import { App } from './app';
import { HeaderComponent } from './header/header.component';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, HeaderComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideTranslateService(),
      ],
    }).compileComponents();
  });

  it('should render the header', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const header = compiled.querySelector('pf-header');
    expect(header).toBeTruthy();
  });

  it('should have a router outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    const outlet = compiled.querySelector('router-outlet');
    expect(outlet).toBeTruthy();
  });
});
