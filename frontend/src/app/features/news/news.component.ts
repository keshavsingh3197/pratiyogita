import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NewsService } from '../../core/services/news.service';
import { NewsPost } from '../../core/models/domain.models';

@Component({
  selector: 'app-news',
  imports: [DatePipe],
  template: `
    <h1>News</h1>
    <p>What's next — results announcements, deadlines and upcoming events.</p>

    @if (posts().length) {
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
      <div class="empty-state">No news yet — check back soon.</div>
    }
  `,
  styles: `
    .meta { color: var(--fg-muted); }
  `,
})
export class NewsComponent {
  private newsApi = inject(NewsService);
  protected readonly posts = signal<NewsPost[]>([]);

  constructor() {
    this.newsApi.getPublished().subscribe((list) => this.posts.set(list));
  }
}
