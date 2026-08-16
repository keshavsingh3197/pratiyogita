import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { StudentProfile } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class StudentProfileService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/students`;

  getMine() {
    return this.http.get<StudentProfile>(`${this.base}/me`);
  }

  upsertMine(req: {
    schoolId: string;
    firstName: string;
    lastName?: string;
    dateOfBirth?: string;
    classGrade?: string;
    academicYear?: string;
    email?: string;
    phone?: string;
    guardianName?: string;
  }) {
    return this.http.put<StudentProfile>(`${this.base}/me`, req);
  }
}
