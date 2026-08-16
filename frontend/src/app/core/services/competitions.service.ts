import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Competition, CompetitionStatus, CompetitionType } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class CompetitionsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/competitions`;

  getAll(type?: CompetitionType, status?: CompetitionStatus) {
    const params: Record<string, string> = {};
    if (type) params['type'] = type;
    if (status) params['status'] = status;
    return this.http.get<Competition[]>(this.base, { params });
  }

  getById(id: string) {
    return this.http.get<Competition>(`${this.base}/${id}`);
  }

  registerSelf(competitionId: string, teamName?: string) {
    return this.http.post(`${this.base}/${competitionId}/register`, { teamName });
  }
}
