import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, input } from '@angular/core';
import { AdSenseService } from '../../../core/services/adsense.service';
import { EcosystemConfigService } from '../../../core/services/ecosystem-config.service';

@Component({
  selector: 'app-ad-slot',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (adReady()) {
      <aside class="ad-shell" [class]="'ad-shell ad-' + placement()" [attr.aria-label]="adLabel()">
        <span class="ad-caption">{{ adLabel() }}</span>
        <ins
          class="adsbygoogle ad-ins"
          [attr.data-ad-client]="clientId()"
          [attr.data-ad-slot]="slotId()"
          data-ad-format="auto"
          data-full-width-responsive="true">
        </ins>
      </aside>
    } @else if (sponsorReady()) {
      <aside class="sponsor-banner" [class]="'sponsor-banner ad-' + placement()">
        <div class="sponsor-copy">
          <span class="sponsor-badge">{{ ads()?.sponsorBadge || 'Featured' }}</span>
          <strong class="sponsor-title">{{ ads()?.sponsorTitle }}</strong>
          @if (ads()?.sponsorDescription) {
            <span class="sponsor-desc">{{ ads()?.sponsorDescription }}</span>
          }
        </div>
        <a [href]="sponsorHref()!" target="_blank" rel="noopener sponsored" class="sponsor-cta">
          {{ ads()?.sponsorCtaText || 'Learn More' }} ->
        </a>
      </aside>
    }
  `,
  styles: [`
    :host { display: block; }
    .ad-shell {
      background: var(--surface, #ffffff);
      border: 1px solid var(--border, #e2e8f0);
      border-radius: 12px;
      padding: 0.75rem 1rem;
      text-align: center;
      overflow: hidden;
      margin: 1rem auto;
    }
    .ad-shell.ad-footer { max-width: 1120px; }
    .ad-caption {
      display: block; font-size: 0.65rem; font-weight: 700;
      letter-spacing: 0.08em; text-transform: uppercase; color: var(--fg-muted, #64748b); margin-bottom: 0.35rem;
    }
    .ad-ins { display: block; min-height: 90px; }
    .sponsor-banner {
      display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap;
      background: var(--surface, #f8fafc); border: 1px solid var(--border, #cbd5e1);
      border-radius: 12px; padding: 0.9rem 1.25rem; margin: 1.25rem 0;
    }
    .sponsor-copy { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
    .sponsor-badge {
      font-size: 0.68rem; font-weight: 800; text-transform: uppercase;
      background: var(--brand-600, #2563eb); color: #fff; padding: 0.2rem 0.55rem; border-radius: 6px;
    }
    .sponsor-title { color: var(--fg, #0f172a); font-size: 0.95rem; }
    .sponsor-desc { color: var(--fg-muted, #475569); font-size: 0.84rem; }
    .sponsor-cta {
      background: var(--brand-600, #2563eb); color: #fff; text-decoration: none;
      font-weight: 700; font-size: 0.82rem; padding: 0.5rem 1rem; border-radius: 8px; white-space: nowrap;
    }
  `]
})
export class AdSlotComponent {
  private readonly ecosystem = inject(EcosystemConfigService);
  private readonly adSense = inject(AdSenseService);

  readonly placement = input<'header' | 'infeed' | 'sidebar' | 'footer'>('infeed');

  private pushedKey = '';

  readonly ads = computed(() => this.ecosystem.branding()?.ads);
  readonly clientId = computed(() => {
    const id = this.ads()?.adSenseClientId;
    return this.adSense.isValidClientId(id) ? id.trim() : null;
  });
  readonly slotId = computed(() => this.adSense.slotForPlacement(this.ads(), this.placement()));
  readonly adReady = computed(() => !!this.ads()?.adsEnabled && !!this.clientId() && !!this.slotId());
  readonly adLabel = computed(() => this.ads()?.adLabel?.trim() || 'Advertisement');
  readonly sponsorHref = computed(() => this.adSense.safeSponsorUrl(this.ads()?.sponsorUrl));
  readonly sponsorReady = computed(() => {
    const cfg = this.ads();
    const p = this.placement();
    return (p === 'infeed' || p === 'header')
      && !!cfg?.sponsorBannerEnabled
      && !!cfg.sponsorTitle?.trim()
      && !!this.sponsorHref();
  });

  constructor() {
    effect(() => {
      if (!this.adReady()) return;
      const client = this.clientId()!;
      const slot = this.slotId()!;
      this.adSense.ensureScriptLoaded(client);
      const key = `${client}:${slot}:${this.placement()}`;
      if (this.pushedKey === key) return;
      this.pushedKey = key;
      queueMicrotask(() => this.adSense.pushAdSlot());
    });
  }
}
