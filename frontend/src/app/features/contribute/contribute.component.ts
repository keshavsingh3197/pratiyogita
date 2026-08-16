import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ContributionsService } from '../../core/services/contributions.service';
import { SettingsService } from '../../core/services/settings.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-contribute',
  imports: [FormsModule, RouterLink],
  template: `
    <h1>Contribute</h1>
    <p>
      Pay any amount via Google Pay, PhonePe, Paytm or any UPI app, then paste the reference number
      below so we can record and verify your contribution.
    </p>

    <div class="grid contribute-grid">
      @if (upiChecked()) {
        @if (upi(); as u) {
          <div class="card upi-card">
            <span class="badge">Pay via UPI</span>
            <h3>{{ u.payeeName }}</h3>

            @if (qrDataUrl(); as qr) {
              <img [src]="qr" alt="Scan to pay via UPI" class="qr-image" />
            }

            <p class="vpa">{{ u.vpa }}</p>
            <a [href]="u.link" class="btn btn-accent">Open in a UPI app</a>

            <p class="security-note">
              ⚠️ Only trust the UPI ID shown above (fetched live from our server, never editable on
              this page). Never pay a QR code or link received by email/message claiming to be us —
              verify it matches <strong>{{ u.vpa }}</strong> first.
            </p>
          </div>
        } @else {
          <div class="card upi-card">
            <p>Online (UPI) contributions aren't configured yet — an admin needs to set a payout UPI ID.</p>
            <p>You can still submit a contribution below once you've arranged payment another way.</p>

            @if (auth.hasRole('Admin')) {
              @if (showConfigureForm()) {
                <div class="stack configure-form">
                  <div class="field">
                    <label for="cfgVpa">UPI VPA</label>
                    <input id="cfgVpa" [(ngModel)]="configVpa" name="cfgVpa" placeholder="yourname@bank" />
                  </div>
                  <div class="field">
                    <label for="cfgPayee">Payee name</label>
                    <input id="cfgPayee" [(ngModel)]="configPayeeName" name="cfgPayee" placeholder="Pratiyogita" />
                  </div>
                  <button type="button" class="btn btn-primary" (click)="saveConfig()">Save</button>
                </div>
              } @else {
                <button type="button" class="btn btn-outline" (click)="showConfigureForm.set(true)">
                  Configure now
                </button>
              }
              <p class="admin-hint">
                Full location/category/payment management lives on the
                <a routerLink="/admin/data">Manage data</a> screen.
              </p>
            }
          </div>
        }
      }

      <form class="card" (submit)="submit(); $event.preventDefault()">
        <div class="field">
          <label for="amount">Amount (₹)</label>
          <input id="amount" type="number" [(ngModel)]="amount" name="amount" required min="1" />
        </div>
        <div class="field">
          <label for="name">Name</label>
          <input id="name" [(ngModel)]="name" name="name" [disabled]="anonymous" />
        </div>
        <div class="field">
          <label for="email">Email</label>
          <input id="email" type="email" [(ngModel)]="email" name="email" />
        </div>
        <div class="field">
          <label for="ref">UPI reference no.</label>
          <input id="ref" [(ngModel)]="transactionRef" name="transactionRef" required />
          <small>Shown in your UPI app right after paying.</small>
        </div>
        <label class="row anon-check">
          <input type="checkbox" [(ngModel)]="anonymous" name="anonymous" /> Contribute anonymously
        </label>
        <button type="submit" class="btn btn-primary">Submit</button>

        @if (submitted()) {
          <p class="notice">Thanks! Your contribution is recorded and pending verification.</p>
        }
      </form>
    </div>
  `,
  styles: `
    .contribute-grid { grid-template-columns: 320px 1fr; align-items: start; }
    @media (max-width: 720px) { .contribute-grid { grid-template-columns: 1fr; } }
    .upi-card { text-align: center; }
    .qr-image { width: 180px; height: 180px; margin: var(--space-4) auto; display: block; border-radius: var(--radius-sm); }
    .vpa { font-weight: 700; font-size: 1.1rem; color: var(--fg); }
    .security-note { font-size: 0.78rem; text-align: left; margin-top: var(--space-4); margin-bottom: 0; }
    .configure-form { text-align: left; margin-top: var(--space-4); }
    .admin-hint { font-size: 0.8rem; margin-top: var(--space-4); margin-bottom: 0; }
    .anon-check { font-weight: 500; color: var(--fg-muted); margin-bottom: var(--space-4); }
    .anon-check input { width: auto; }
  `,
})
export class ContributeComponent {
  private contributionsApi = inject(ContributionsService);
  private settingsApi = inject(SettingsService);
  protected readonly auth = inject(AuthService);

  protected readonly upi = signal<{ link: string; vpa: string; payeeName: string } | null>(null);
  protected readonly upiChecked = signal(false);
  protected readonly qrDataUrl = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly showConfigureForm = signal(false);

  protected amount: number | null = null;
  protected name = '';
  protected email = '';
  protected transactionRef = '';
  protected anonymous = false;
  protected configVpa = '';
  protected configPayeeName = '';

  constructor() {
    this.loadUpiLink();
  }

  private loadUpiLink(): void {
    this.contributionsApi.getUpiLink().subscribe({
      next: (res) => {
        this.upiChecked.set(true);
        this.upi.set(null);
        this.qrDataUrl.set(null);
        if (res.configured && res.link && res.vpa) {
          this.upi.set({ link: res.link, vpa: res.vpa, payeeName: res.payeeName });
          this.generateQr(res.link);
        }
      },
      error: () => this.upiChecked.set(true),
    });
  }

  /** Generated entirely client-side (no third-party QR API) so the UPI link/VPA never leaves the
   *  browser just to render a code — it only ever encodes what's already shown as plain text. */
  private generateQr(link: string): void {
    import('qrcode')
      .then((QRCode) => QRCode.toDataURL(link, { margin: 1, width: 200 }))
      .then((dataUrl) => this.qrDataUrl.set(dataUrl))
      .catch(() => this.qrDataUrl.set(null));
  }

  saveConfig(): void {
    if (!this.configVpa) return;
    this.settingsApi
      .updatePayments({ upiVpa: this.configVpa, payeeName: this.configPayeeName || 'Pratiyogita' })
      .subscribe(() => {
        this.showConfigureForm.set(false);
        this.loadUpiLink();
      });
  }

  submit(): void {
    if (!this.amount || this.amount <= 0) return;
    this.contributionsApi
      .submit({
        contributorName: this.name || 'Anonymous',
        email: this.email || undefined,
        amount: this.amount,
        method: 'Upi',
        transactionRef: this.transactionRef,
        isAnonymous: this.anonymous,
      })
      .subscribe(() => this.submitted.set(true));
  }
}
