import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { NewsService } from '../../core/services/news.service';
import { LocaleService } from '../../core/services/locale.service';
import { NewsPost } from '../../core/models/domain.models';
import { LoadingStateComponent } from '../shared/loading/loading-state.component';

@Component({
  selector: 'app-news',
  imports: [DatePipe, LoadingStateComponent],
  template: `
    <h1>{{ locale.t('pratiyogita.news.title', 'News') }}</h1>
    <p>{{ locale.t('pratiyogita.news.subtitle', "What's next — results announcements, deadlines and upcoming events.") }}</p>

    @if (loading() && !posts().length) {
      <app-loading-state variant="rows" [count]="4" label="Loading latest announcements…" />
    } @else if (error() && !posts().length) {
      <app-loading-state [error]="error()" (retry)="loadNews()" />
    } @else if (posts().length) {
      <div class="stack">
        @for (post of posts(); track post.id) {
          <article class="card">
            <h3>{{ post.title }}</h3>
            <p>{{ post.summary }}</p>
            <small class="meta">{{ post.publishedAt | date: 'mediumDate' }}</small>
          </article>
        }
      </div>
    } @else {
      <div class="empty-state">{{ locale.t('pratiyogita.news.empty', 'No news yet — check back soon.') }}</div>
    }
  `,
  styles: `
    .meta { color: var(--fg-muted); }
  `,
})
export class NewsComponent {
  private newsApi = inject(NewsService);
  protected readonly locale = inject(LocaleService);
  protected readonly posts = signal<NewsPost[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.loadNews();
  }

  loadNews(): void {
    this.loading.set(true);
    this.error.set(null);
    this.newsApi
      .getPublished()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (list) => this.posts.set(list),
        error: () => this.error.set('Could not load news right now. Please try again.'),
      });
  }
}
