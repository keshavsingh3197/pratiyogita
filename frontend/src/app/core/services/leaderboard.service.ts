import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { TopContributor, TopperEntry } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/leaderboard`;

  getToppers(filters: { schoolId?: string; city?: string; category?: string; top?: number } = {}) {
    const params: Record<string, string> = {};
    if (filters.schoolId) params['schoolId'] = filters.schoolId;
    if (filters.city) params['city'] = filters.city;
    if (filters.category) params['category'] = filters.category;
    if (filters.top) params['top'] = String(filters.top);
    return this.http.get<TopperEntry[]>(`${this.base}/toppers`, { params });
  }

  getTopContributors(top = 20) {
    return this.http.get<TopContributor[]>(`${this.base}/contributors`, { params: { top: String(top) } });
  }
}
