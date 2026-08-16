import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly auth = inject(AuthService);

  constructor() {
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
