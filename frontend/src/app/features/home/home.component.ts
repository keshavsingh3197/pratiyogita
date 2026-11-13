import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EcosystemConfigService } from '../../core/services/ecosystem-config.service';
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
        <a routerLink="/competitions" class="btn btn-primary">Browse competitions</a>
        <a routerLink="/leaderboard" class="btn btn-outline">See toppers</a>
      </div>

      @if (ecosystem.footerChannels().length > 0) {
        <div class="community-strip">
          <span class="comm-label">Updates &amp; Highlights:</span>
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
        <h3>Competitions &amp; Tournaments</h3>
        <p>Academic olympiads and sports tournaments — schedules and registration.</p>
      </a>
      <a routerLink="/leaderboard" class="card card-hover link-card">
        <h3>Toppers &amp; Contributors</h3>
        <p>Rankings by school, city and category — plus who's supporting the platform.</p>
      </a>
      <a routerLink="/news" class="card card-hover link-card">
        <h3>Latest news</h3>
        <p>Results announcements, deadlines and upcoming events.</p>
      </a>
      <a routerLink="/contribute" class="card card-hover link-card">
        <h3>Contribute</h3>
        <p>Support via UPI — Google Pay, PhonePe, Paytm and more.</p>
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
}
