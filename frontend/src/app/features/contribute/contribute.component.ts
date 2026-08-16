import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContributionsService } from '../../core/services/contributions.service';

@Component({
  selector: 'app-contribute',
  imports: [FormsModule],
  template: `
    <h1>Contribute</h1>
    <p>
      Pay any amount via Google Pay, PhonePe, Paytm or any UPI app, then paste the reference number
      below so we can record and verify your contribution.
    </p>

    <div class="grid contribute-grid">
      @if (upi(); as u) {
        <div class="card upi-card">
          <span class="badge">Pay via UPI</span>
          <h3>{{ u.payeeName }}</h3>
          <p class="vpa">{{ u.vpa }}</p>
          <a [href]="u.link" class="btn btn-accent">Open in a UPI app</a>
        </div>
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
    .vpa { font-weight: 700; font-size: 1.1rem; color: var(--fg); }
    .anon-check { font-weight: 500; color: var(--fg-muted); margin-bottom: var(--space-4); }
    .anon-check input { width: auto; }
  `,
})
export class ContributeComponent {
  private contributionsApi = inject(ContributionsService);
  protected readonly upi = signal<{ link: string; vpa: string; payeeName: string } | null>(null);
  protected readonly submitted = signal(false);

  protected amount: number | null = null;
  protected name = '';
  protected email = '';
  protected transactionRef = '';
  protected anonymous = false;

  constructor() {
    this.contributionsApi.getUpiLink().subscribe({ next: (u) => this.upi.set(u), error: () => {} });
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
