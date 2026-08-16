import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContributionsService } from '../../core/services/contributions.service';

@Component({
  selector: 'app-contribute',
  imports: [FormsModule],
  template: `
    <h1>Contribute</h1>
    <p>
      Pay any amount to the UPI ID below using Google Pay, PhonePe, Paytm or any UPI app, then
      paste the reference number below so we can record and verify your contribution.
    </p>

    @if (upi(); as u) {
      <p><strong>UPI ID:</strong> {{ u.vpa }} ({{ u.payeeName }})</p>
      <a [href]="u.link">Open in a UPI app</a>
    }

    <form (submit)="submit(); $event.preventDefault()">
      <label>Amount (₹) <input type="number" [(ngModel)]="amount" name="amount" required /></label>
      <label>Name <input [(ngModel)]="name" name="name" [disabled]="anonymous" /></label>
      <label>Email <input [(ngModel)]="email" name="email" /></label>
      <label>UPI reference no. <input [(ngModel)]="transactionRef" name="transactionRef" required /></label>
      <label><input type="checkbox" [(ngModel)]="anonymous" name="anonymous" /> Contribute anonymously</label>
      <button type="submit">Submit</button>
    </form>

    @if (submitted()) {
      <p>Thanks! Your contribution is recorded and pending verification.</p>
    }
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
