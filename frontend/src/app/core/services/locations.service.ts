import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Location } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class LocationsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/locations`;

  getAll() {
    return this.http.get<Location[]>(this.base);
  }
}
