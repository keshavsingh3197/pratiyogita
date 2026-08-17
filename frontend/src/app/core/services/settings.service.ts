import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { PaymentSettings } from '../models/domain.models';

/** Runtime, DB-backed platform settings — currently just Payments. Reads are public; updates are
 *  Admin-only server-side (backend returns 403 for anyone else). */
@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/settings`;

  getPayments() {
    return this.http.get<PaymentSettings>(`${this.base}/payments`);
  }

  updatePayments(req: { upiVpa: string; payeeName: string }) {
    return this.http.put<PaymentSettings>(`${this.base}/payments`, req);
  }

  /** Absolute URL for the admin-uploaded QR image (public endpoint) — bust the cache on re-upload
   *  by appending a timestamp, since the object key/URL itself doesn't change per upload. */
  qrImageUrl(): string {
    return `${this.base}/payments/qr-image?t=${Date.now()}`;
  }

  uploadQrImage(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<PaymentSettings>(`${this.base}/payments/qr-image`, form);
  }

  deleteQrImage() {
    return this.http.delete<void>(`${this.base}/payments/qr-image`);
  }
}
