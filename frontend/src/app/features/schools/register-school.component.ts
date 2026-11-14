import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { SchoolsService } from '../../core/services/schools.service';

@Component({
  selector: 'app-register-school',
  imports: [FormsModule],
  template: `
    <h1>Register a school</h1>
    <p>Submitted schools are reviewed by an admin before they appear publicly or can enter competitions.</p>

    <form class="card form-card" (submit)="submit(); $event.preventDefault()">
      <div class="field">
        <label for="name">School name</label>
        <input id="name" [(ngModel)]="name" name="name" required />
      </div>
      <div class="field">
        <label for="contactEmail">Contact email</label>
        <input id="contactEmail" type="email" [(ngModel)]="contactEmail" name="contactEmail" required />
      </div>
      <div class="field">
        <label for="contactPhone">Contact phone</label>
        <input id="contactPhone" [(ngModel)]="contactPhone" name="contactPhone" />
      </div>
      <div class="field">
        <label for="address">Address</label>
        <input id="address" [(ngModel)]="address" name="address" />
      </div>
      <button type="submit" class="btn btn-primary" [disabled]="submitting()">
        @if (submitting()) { <span class="btn-spin" aria-hidden="true"></span>Submitting… } @else { Submit for approval }
      </button>

      @if (submitted()) {
        <p class="notice">Thanks — your school has been submitted and is pending admin approval.</p>
      }
      @if (error()) {
        <p class="error-text">{{ error() }}</p>
      }
    </form>
  `,
  styles: `
    .form-card { max-width: 480px; }
    .error-text { color: var(--danger-500); font-weight: 600; margin-top: var(--space-3); }
  `,
})
export class RegisterSchoolComponent {
  private schoolsApi = inject(SchoolsService);
  protected readonly submitted = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected name = '';
  protected contactEmail = '';
  protected contactPhone = '';
  protected address = '';

  submit(): void {
    if (this.submitting()) return;
    this.error.set(null);
    this.submitted.set(false);
    this.submitting.set(true);
    this.schoolsApi
      .register({
        name: this.name,
        contactEmail: this.contactEmail,
        contactPhone: this.contactPhone || undefined,
        address: this.address || undefined,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.submitted.set(true),
        error: () => this.error.set('Could not submit school right now — please try again.'),
      });
  }
}
