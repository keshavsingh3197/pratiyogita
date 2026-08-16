import { Injectable, effect, signal } from '@angular/core';

export type LanguageCode = 'en' | 'hi';

const STORAGE_KEY = 'pratiyogita.language';

export const SUPPORTED_LANGUAGES: { code: LanguageCode; label: string }[] = [
  { code: 'en', label: 'English' },
  { code: 'hi', label: 'हिन्दी (Hindi)' },
];

/** Stores the visitor's language preference. This is a lightweight preference store only — it
 *  does not yet translate page content; wiring real i18n (e.g. an Angular locale/translation
 *  pipeline reading this signal) is a follow-up, not done in this pass. */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  readonly language = signal<LanguageCode>(this.readStored());

  constructor() {
    effect(() => localStorage.setItem(STORAGE_KEY, this.language()));
  }

  set(code: LanguageCode): void {
    this.language.set(code);
  }

  private readStored(): LanguageCode {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'hi' ? 'hi' : 'en';
  }
}
