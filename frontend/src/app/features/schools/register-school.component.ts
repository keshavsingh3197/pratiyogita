import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
      <button type="submit" class="btn btn-primary">Submit for approval</button>

      @if (submitted()) {
        <p class="notice">Thanks — your school has been submitted and is pending admin approval.</p>
      }
    </form>
  `,
  styles: `
    .form-card { max-width: 480px; }
  `,
})
export class RegisterSchoolComponent {
  private schoolsApi = inject(SchoolsService);
  protected readonly submitted = signal(false);

  protected name = '';
  protected contactEmail = '';
  protected contactPhone = '';
  protected address = '';

  submit(): void {
    this.schoolsApi
      .register({
        name: this.name,
        contactEmail: this.contactEmail,
        contactPhone: this.contactPhone || undefined,
        address: this.address || undefined,
      })
      .subscribe(() => this.submitted.set(true));
  }
}
