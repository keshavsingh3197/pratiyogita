import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EcosystemConfigService } from '../../core/services/ecosystem-config.service';
import { LocaleService } from '../../core/services/locale.service';
import { AdSlotComponent } from '../shared/ad-slot/ad-slot.component';

@Component({
  selector: 'app-home',
  imports: [RouterLink, AdSlotComponent],
  template: `
    <section class="hero">
      <span class="badge">{{ ecosystem.heroBadge() }}</span>
      <h1>{{ ecosystem.heroTitle() }}</h1>
      <p class="hero-subtitle">
        {{ ecosystem.heroSubtitle() }}
      </p>
      <div class="row cta-row">
        <a routerLink="/competitions" class="btn btn-primary">{{ locale.t('pratiyogita.home.exploreCompetitions', 'Browse competitions') }}</a>
        <a routerLink="/leaderboard" class="btn btn-outline">{{ locale.t('pratiyogita.home.viewLeaderboard', 'See toppers') }}</a>
        <a routerLink="/winners" class="btn btn-outline">🏆 {{ locale.t('pratiyogita.home.viewWinners', 'Live Quiz Winners') }}</a>
      </div>

      @if (ecosystem.footerChannels().length > 0) {
        <div class="community-strip">
          <span class="comm-label">{{ locale.t('pratiyogita.home.updatesHighlights', 'Updates & Highlights:') }}</span>
          @for (ch of ecosystem.footerChannels(); track ch.id || ch.key) {
            <a [href]="ch.url" target="_blank" rel="noopener" class="comm-pill">
              📺 {{ ch.name }}
            </a>
          }
        </div>
      }
    </section>

    <section class="grid grid-cards">
      <a routerLink="/competitions" class="card card-hover link-card">
        <h3>{{ locale.t('pratiyogita.home.card.compTitle', 'Competitions & Tournaments') }}</h3>
        <p>{{ locale.t('pratiyogita.home.card.compDesc', 'Academic olympiads and sports tournaments — schedules and registration.') }}</p>
      </a>
      <a routerLink="/leaderboard" class="card card-hover link-card">
        <h3>{{ locale.t('pratiyogita.home.card.lbTitle', 'Toppers & Contributors') }}</h3>
        <p>{{ locale.t('pratiyogita.home.card.lbDesc', "Rankings by school, city and category — plus who's supporting the platform.") }}</p>
      </a>
      <a routerLink="/news" class="card card-hover link-card">
        <h3>{{ locale.t('pratiyogita.home.card.newsTitle', 'Latest news') }}</h3>
        <p>{{ locale.t('pratiyogita.home.card.newsDesc', 'Results announcements, deadlines and upcoming events.') }}</p>
      </a>
      <a routerLink="/contribute" class="card card-hover link-card">
        <h3>{{ locale.t('pratiyogita.home.card.contribTitle', 'Contribute') }}</h3>
        <p>{{ locale.t('pratiyogita.home.card.contribDesc', 'Support via UPI — Google Pay, PhonePe, Paytm and more.') }}</p>
      </a>
    </section>

    <app-ad-slot placement="infeed" />
  `,
  styles: `
    .hero {
      text-align: center;
      padding: var(--space-8) 0 var(--space-8);
      max-width: 760px;
      margin: 0 auto var(--space-8);
    }
    .hero .badge { margin-bottom: var(--space-4); }
    .hero-subtitle { font-size: 1.05rem; }
    .hero .cta-row { justify-content: center; flex-wrap: wrap; }
    .community-strip {
      margin-top: var(--space-5);
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .comm-label { font-size: 0.8rem; color: var(--fg-muted); font-weight: 600; }
    .comm-pill {
      font-size: 0.8rem;
      font-weight: 600;
      padding: 0.25rem 0.7rem;
      border-radius: 999px;
      border: 1px solid var(--border);
      background: var(--surface);
      color: var(--fg);
    }
    .link-card { color: inherit; display: block; transition: transform 0.15s ease; }
    .link-card:hover { text-decoration: none; transform: translateY(-2px); }
    .link-card h3 { color: var(--brand-600); }
    .link-card p { margin-bottom: 0; }
  `,
})
export class HomeComponent {
  protected readonly ecosystem = inject(EcosystemConfigService);
  protected readonly locale = inject(LocaleService);
}
