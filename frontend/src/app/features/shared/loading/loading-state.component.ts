import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `
    @if (error) {
      <div class="error-box" role="alert">
        <span>⚠️ {{ error }}</span>
        @if (showRetry) {
          <button type="button" class="btn btn-outline" (click)="retry.emit()">Retry</button>
        }
      </div>
    } @else if (variant === 'cards') {
      <div class="skel-wrap" role="status" aria-busy="true" [attr.aria-label]="label">
        @if (label) {
          <div class="loading-pill"><span class="mini-spin" aria-hidden="true"></span>{{ label }}</div>
        }
        <div class="grid grid-cards">
          @for (i of items(count); track i) {
            <div class="card skel-card">
              <span class="skel-bar w-70 h-md"></span>
              <span class="skel-bar w-100 h-sm"></span>
              <span class="skel-bar w-85 h-sm"></span>
              <span class="skel-bar w-45 h-xs"></span>
            </div>
          }
        </div>
      </div>
    } @else if (variant === 'rows') {
      <div class="skel-wrap" role="status" aria-busy="true" [attr.aria-label]="label">
        @if (label) {
          <div class="loading-pill"><span class="mini-spin" aria-hidden="true"></span>{{ label }}</div>
        }
        <div class="stack">
          @for (i of items(count); track i) {
            <div class="card skel-row">
              <span class="skel-bar w-35 h-sm"></span>
              <span class="skel-bar w-45 h-sm"></span>
              <span class="skel-bar w-20 h-sm"></span>
            </div>
          }
        </div>
      </div>
    } @else {
      <div class="spinner-box" role="status" aria-busy="true">
        <span class="mini-spin lg" aria-hidden="true"></span>
        <span>{{ label }}</span>
      </div>
    }
  `,
  styles: `
    .skel-wrap { display: flex; flex-direction: column; gap: var(--space-3); }
    .loading-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      align-self: flex-start;
      padding: 0.35rem 0.75rem;
      border-radius: 999px;
      border: 1px solid var(--border);
      background: var(--surface);
      color: var(--fg-muted);
      font-size: 0.82rem;
      font-weight: 600;
    }
    .mini-spin {
      width: 14px;
      height: 14px;
      border-radius: 999px;
      border: 2px solid color-mix(in srgb, var(--brand-500) 22%, transparent);
      border-top-color: var(--brand-500);
      animation: prati-spin 0.75s linear infinite;
      flex: none;
    }
    .mini-spin.lg { width: 26px; height: 26px; border-width: 3px; }
    .skel-card { display: flex; flex-direction: column; gap: 0.65rem; }
    .skel-row { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; padding: var(--space-4); }
    .skel-bar {
      display: block;
      border-radius: 999px;
      background: linear-gradient(
        90deg,
        color-mix(in srgb, var(--fg-muted) 12%, transparent) 20%,
        color-mix(in srgb, var(--fg-muted) 25%, transparent) 50%,
        color-mix(in srgb, var(--fg-muted) 12%, transparent) 80%
      );
      background-size: 200% 100%;
      animation: prati-shimmer 1.35s ease-in-out infinite;
    }
    .h-xs { height: 10px; }
    .h-sm { height: 13px; }
    .h-md { height: 18px; }
    .w-20 { width: 20%; }
    .w-35 { width: 35%; }
    .w-45 { width: 45%; }
    .w-70 { width: 70%; }
    .w-85 { width: 85%; }
    .w-100 { width: 100%; }
    .spinner-box {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.6rem;
      padding: var(--space-8);
      border: 1px dashed var(--border);
      border-radius: var(--radius);
      color: var(--fg-muted);
      font-weight: 500;
    }
    .error-box {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      flex-wrap: wrap;
      padding: var(--space-4);
      border-radius: var(--radius);
      background: var(--danger-100);
      color: #991b1b;
      font-weight: 600;
    }
    @keyframes prati-shimmer {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }
    @keyframes prati-spin {
      to { transform: rotate(360deg); }
    }
    @media (prefers-reduced-motion: reduce) {
      .skel-bar { animation: none; }
      .mini-spin { animation-duration: 1.5s; }
    }
  `,
})
export class LoadingStateComponent {
  @Input() variant: 'spinner' | 'cards' | 'rows' = 'cards';
  @Input() count = 3;
  @Input() label = 'Loading…';
  @Input() error: string | null = null;
  @Input() showRetry = true;
  @Output() retry = new EventEmitter<void>();

  items(n: number): number[] {
    return Array.from({ length: Math.min(12, Math.max(1, Math.trunc(n) || 1)) }, (_, i) => i);
  }
}
