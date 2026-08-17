import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export type ContributionMethod = 'Upi' | 'Card' | 'NetBanking' | 'Cash' | 'Other';

@Injectable({ providedIn: 'root' })
export class ContributionsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/contributions`;

  /** Same upi:// deep link works for Google Pay, PhonePe, Paytm and every other UPI app. The VPA is
   *  admin-configured server-side (see /api/settings/payments) — never accepted from the client — so
   *  it can't be tampered with by editing this page. `configured: false` means an Admin hasn't set a
   *  payout UPI id yet. */
  getUpiLink(amount?: number, note?: string) {
    const params: Record<string, string> = {};
    if (amount) params['amount'] = String(amount);
    if (note) params['note'] = note;
    return this.http.get<{
      configured: boolean;
      link: string | null;
      vpa: string | null;
      payeeName: string;
      hasUploadedQr: boolean;
    }>(`${this.base}/upi-link`, { params });
  }

  submit(req: {
    contributorName: string;
    email?: string;
    phone?: string;
    amount: number;
    method: ContributionMethod;
    upiApp?: string;
    transactionRef?: string;
    message?: string;
    isAnonymous: boolean;
    items?: string[];
  }) {
    return this.http.post(this.base, req);
  }
}
