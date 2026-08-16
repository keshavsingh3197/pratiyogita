import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CompetitionCategory, CompetitionType } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/categories`;

  getAll(type?: CompetitionType) {
    return this.http.get<CompetitionCategory[]>(this.base, { params: type ? { type } : {} });
  }

  /** Admin-only server-side (backend returns 403 for anyone else). */
  create(req: { name: string; type: CompetitionType }) {
    return this.http.post<CompetitionCategory>(this.base, req);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
