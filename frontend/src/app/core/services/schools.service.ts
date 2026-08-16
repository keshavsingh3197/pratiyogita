import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { School } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class SchoolsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/schools`;

  getApproved() {
    return this.http.get<School[]>(this.base);
  }

  register(req: {
    name: string;
    locationId?: string;
    address?: string;
    pincode?: string;
    principalName?: string;
    contactEmail: string;
    contactPhone?: string;
  }) {
    return this.http.post<School>(this.base, req);
  }
}
