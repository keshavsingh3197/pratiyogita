import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ContributionItem } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class ContributionItemsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/contribution-items`;

  /** `all` also returns inactive items — ignored server-side unless the caller is Admin/Editor. */
  getAll(all = false) {
    return this.http.get<ContributionItem[]>(this.base, { params: all ? { all: 'true' } : {} });
  }

  create(req: { name: string; description?: string; amount: number }) {
    return this.http.post<ContributionItem>(this.base, req);
  }

  setActive(id: string, isActive: boolean) {
    return this.http.put<void>(`${this.base}/${id}/active`, { isActive });
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
