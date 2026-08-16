import { Injectable, effect, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'pratiyogita.theme';

/** Persists the chosen theme in localStorage and applies it as a `data-theme` attribute on <html>,
 *  which styles.css keys its dark-mode variable overrides off. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<ThemeMode>(this.readStored());

  constructor() {
    effect(() => {
      const mode = this.theme();
      document.documentElement.setAttribute('data-theme', mode);
      localStorage.setItem(STORAGE_KEY, mode);
    });
  }

  set(mode: ThemeMode): void {
    this.theme.set(mode);
  }

  toggle(): void {
    this.theme.set(this.theme() === 'light' ? 'dark' : 'light');
  }

  private readStored(): ThemeMode {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'dark' ? 'dark' : 'light';
  }
}
