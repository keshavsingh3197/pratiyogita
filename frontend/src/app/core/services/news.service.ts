import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { NewsPost } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class NewsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/news`;

  getPublished() {
    return this.http.get<NewsPost[]>(this.base);
  }

  getBySlug(slug: string) {
    return this.http.get<NewsPost>(`${this.base}/${slug}`);
  }
}
