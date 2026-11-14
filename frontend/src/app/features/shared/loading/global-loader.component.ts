import { Component, effect, inject, signal } from '@angular/core';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-global-loader',
  template: `
    @if (loading.active()) {
      <div class="global-progress" role="progressbar" aria-label="Loading">
        <div class="global-progress-track"></div>
      </div>
    }
    @if (loading.active() && loading.slow() && !dismissed()) {
      <div class="wakeup-toast" role="status" aria-live="polite">
        <span class="toast-spinner" aria-hidden="true"></span>
        <span>Connecting to Pratiyogita server &amp; loading data…</span>
        <button type="button" class="toast-close" aria-label="Dismiss" (click)="dismissed.set(true)">✕</button>
      </div>
    }
  `,
  styles: `
    .global-progress {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      height: 3px;
      z-index: 9999;
      pointer-events: none;
      overflow: hidden;
      background: color-mix(in srgb, var(--brand-500) 20%, transparent);
    }
    .global-progress-track {
      position: absolute;
      inset: 0 auto 0 0;
      width: 40%;
      background: linear-gradient(90deg, transparent, var(--brand-500), var(--accent-500));
      animation: prati-slide 1.15s ease-in-out infinite;
    }
    .wakeup-toast {
      position: fixed;
      bottom: 1rem;
      right: 1rem;
      z-index: 9998;
      display: inline-flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.65rem 0.9rem;
      border-radius: var(--radius);
      background: var(--surface);
      color: var(--fg);
      border: 1px solid var(--border);
      box-shadow: var(--shadow);
      font-size: 0.84rem;
      font-weight: 500;
      max-width: min(92vw, 370px);
    }
    .toast-spinner {
      width: 16px;
      height: 16px;
      border-radius: 999px;
      border: 2.5px solid color-mix(in srgb, var(--brand-500) 22%, transparent);
      border-top-color: var(--brand-500);
      animation: prati-spin 0.75s linear infinite;
      flex: none;
    }
    .toast-close {
      border: none;
      background: transparent;
      color: var(--fg-muted);
      cursor: pointer;
      padding: 0 0.2rem;
      font-size: 0.85rem;
    }
    .toast-close:hover { color: var(--fg); }
    @keyframes prati-slide {
      0% { transform: translateX(-110%); }
      100% { transform: translateX(310%); }
    }
    @keyframes prati-spin {
      to { transform: rotate(360deg); }
    }
    @media (prefers-reduced-motion: reduce) {
      .global-progress-track { animation-duration: 2.2s; }
      .toast-spinner { animation-duration: 1.5s; }
    }
  `,
})
export class GlobalLoaderComponent {
  readonly loading = inject(LoadingService);
  readonly dismissed = signal(false);

  constructor() {
    effect(() => {
      if (!this.loading.slow()) {
        this.dismissed.set(false);
      }
    });
  }
}
