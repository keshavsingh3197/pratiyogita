import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NewsService } from '../../core/services/news.service';
import { NewsPost } from '../../core/models/domain.models';

@Component({
  selector: 'app-news',
  imports: [DatePipe],
  template: `
    <h1>News</h1>
    @for (post of posts(); track post.id) {
      <article>
        <h3>{{ post.title }}</h3>
        <p>{{ post.summary }}</p>
        <small>{{ post.publishedAt | date: 'medium' }}</small>
      </article>
    } @empty {
      <p>No news yet.</p>
    }
  `,
})
export class NewsComponent {
  private newsApi = inject(NewsService);
  protected readonly posts = signal<NewsPost[]>([]);

  constructor() {
    this.newsApi.getPublished().subscribe((list) => this.posts.set(list));
  }
}
