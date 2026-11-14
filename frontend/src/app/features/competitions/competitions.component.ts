import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { CompetitionsService } from '../../core/services/competitions.service';
import { Competition } from '../../core/models/domain.models';
import { LoadingStateComponent } from '../shared/loading/loading-state.component';

@Component({
  selector: 'app-competitions',
  imports: [DatePipe, LoadingStateComponent],
  template: `
    <div class="row page-head">
      <div>
        <h1>Competitions &amp; Tournaments</h1>
        <p>Academic exams/olympiads and sports tournaments, side by side.</p>
      </div>
    </div>

    @if (loading() && !competitions().length) {
      <app-loading-state variant="cards" [count]="3" label="Loading competitions…" />
    } @else if (error() && !competitions().length) {
      <app-loading-state [error]="error()" (retry)="loadCompetitions()" />
    } @else if (competitions().length) {
      <div class="grid grid-cards">
        @for (c of competitions(); track c.id) {
          <article class="card card-hover">
            <div class="row">
              <span class="badge" [class.badge-warn]="c.type === 'Sports'">{{ c.type }}</span>
              <span class="badge badge-ok">{{ c.status }}</span>
            </div>
            <h3>{{ c.name }}</h3>
            <p class="category">{{ c.category }} &middot; {{ c.level }}</p>
            <p class="meta">Starts {{ c.startsAt | date: 'mediumDate' }}</p>
          </article>
        }
      </div>
    } @else {
      <div class="empty-state">No competitions published yet — check back soon.</div>
    }
  `,
  styles: `
    .page-head { justify-content: space-between; margin-bottom: var(--space-6); }
    .category { margin-bottom: var(--space-1); font-weight: 500; color: var(--fg); }
    .meta { margin-bottom: 0; font-size: 0.85rem; }
    article .row { margin-bottom: var(--space-3); }
  `,
})
export class CompetitionsComponent {
  private competitionsApi = inject(CompetitionsService);
  protected readonly competitions = signal<Competition[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.loadCompetitions();
  }

  loadCompetitions(): void {
    this.loading.set(true);
    this.error.set(null);
    this.competitionsApi
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (list) => this.competitions.set(list),
        error: () => this.error.set('Could not load competitions right now. Please try again.'),
      });
  }
}
