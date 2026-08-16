import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SchoolsService } from '../../core/services/schools.service';

@Component({
  selector: 'app-register-school',
  imports: [FormsModule],
  template: `
    <h1>Register a school</h1>
    <p>Submitted schools are reviewed by an admin before they appear publicly or can enter competitions.</p>
    <form (submit)="submit(); $event.preventDefault()">
      <label>School name <input [(ngModel)]="name" name="name" required /></label>
      <label>Contact email <input type="email" [(ngModel)]="contactEmail" name="contactEmail" required /></label>
      <label>Contact phone <input [(ngModel)]="contactPhone" name="contactPhone" /></label>
      <label>Address <input [(ngModel)]="address" name="address" /></label>
      <button type="submit">Submit for approval</button>
    </form>
    @if (submitted()) {
      <p>Thanks — your school has been submitted and is pending admin approval.</p>
    }
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
