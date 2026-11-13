import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { ThemeService } from './core/services/theme.service';
import { EcosystemConfigService } from './core/services/ecosystem-config.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly ecosystem = inject(EcosystemConfigService);
  // Instantiated here (not just used) so its constructor effect applies the stored theme on boot.
  private readonly theme = inject(ThemeService);
  protected readonly year = new Date().getFullYear();

  constructor() {
    this.ecosystem.load();
    // Silent SSO check on load so a nav bar signed-in state shows without forcing a login.
    this.auth.refresh().subscribe({ error: () => this.auth.forceClear() });
  }

  login(): void {
    this.auth.loginRedirect(location.origin);
  }

  logout(): void {
    this.auth.logout().subscribe({ next: () => this.auth.loginRedirect(location.origin) });
  }
}
