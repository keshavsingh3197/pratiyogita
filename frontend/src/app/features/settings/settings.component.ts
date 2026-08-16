import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { LocaleService, SUPPORTED_LANGUAGES } from '../../core/services/locale.service';
import { StudentProfileService } from '../../core/services/student-profile.service';
import { SchoolsService } from '../../core/services/schools.service';
import { School, StudentProfile } from '../../core/models/domain.models';

@Component({
  selector: 'app-settings',
  imports: [FormsModule],
  template: `
    <h1>Settings</h1>

    <div class="stack settings-sections">
      <section class="card">
        <h2>My details</h2>
        <p>Your account (name, email, sign-in) is managed centrally at your keshavsingh.in account.</p>
        @if (auth.user(); as u) {
          <dl class="details-grid">
            <dt>Name</dt><dd>{{ u.displayName }}</dd>
            <dt>Email</dt><dd>{{ u.email }}</dd>
            <dt>Roles</dt><dd>{{ u.roles.join(', ') || 'Member' }}</dd>
          </dl>
        }

        <h3 class="profile-heading">Student profile</h3>
        <p>School, class and contact details used for competition registrations and results.</p>
        <div class="grid profile-grid">
          <div class="field">
            <label for="school">School</label>
            <select id="school" [(ngModel)]="schoolId" name="school">
              <option value="" disabled>Select your school</option>
              @for (s of schools(); track s.id) {
                <option [value]="s.id">{{ s.name }}</option>
              }
            </select>
          </div>
          <div class="field">
            <label for="firstName">First name</label>
            <input id="firstName" [(ngModel)]="firstName" name="firstName" />
          </div>
          <div class="field">
            <label for="lastName">Last name</label>
            <input id="lastName" [(ngModel)]="lastName" name="lastName" />
          </div>
          <div class="field">
            <label for="classGrade">Class</label>
            <input id="classGrade" [(ngModel)]="classGrade" name="classGrade" placeholder="e.g. 10" />
          </div>
          <div class="field">
            <label for="dob">Date of birth</label>
            <input id="dob" type="date" [(ngModel)]="dateOfBirth" name="dateOfBirth" />
          </div>
          <div class="field">
            <label for="phone">Phone</label>
            <input id="phone" [(ngModel)]="phone" name="phone" />
          </div>
        </div>
        <button type="button" class="btn btn-primary" (click)="saveProfile()">Save profile</button>
        @if (saved()) {
          <p class="notice">Saved.</p>
        }
      </section>

      <section class="card">
        <h2>Appearance</h2>
        <div class="row">
          <button type="button" class="btn" [class.btn-primary]="theme.theme() === 'light'"
                  [class.btn-outline]="theme.theme() !== 'light'" (click)="theme.set('light')">
            Light
          </button>
          <button type="button" class="btn" [class.btn-primary]="theme.theme() === 'dark'"
                  [class.btn-outline]="theme.theme() !== 'dark'" (click)="theme.set('dark')">
            Dark
          </button>
        </div>
      </section>

      <section class="card">
        <h2>Language</h2>
        <div class="field lang-field">
          <select [(ngModel)]="selectedLanguage" name="language" (ngModelChange)="locale.set($event)">
            @for (l of languages; track l.code) {
              <option [value]="l.code">{{ l.label }}</option>
            }
          </select>
          <small>More of the app will be translated as this preference is wired up further.</small>
        </div>
      </section>
    </div>
  `,
  styles: `
    .settings-sections { max-width: 640px; }
    .details-grid { display: grid; grid-template-columns: auto 1fr; gap: var(--space-2) var(--space-4); margin: 0 0 var(--space-4); }
    .details-grid dt { color: var(--fg-muted); font-weight: 600; }
    .details-grid dd { margin: 0; }
    .profile-heading { margin-top: var(--space-6); }
    .profile-grid { grid-template-columns: repeat(2, 1fr); gap: var(--space-4); margin-bottom: var(--space-4); }
    @media (max-width: 560px) { .profile-grid { grid-template-columns: 1fr; } }
    .lang-field select { max-width: 260px; }
  `,
})
export class SettingsComponent {
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly locale = inject(LocaleService);
  private profileApi = inject(StudentProfileService);
  private schoolsApi = inject(SchoolsService);

  protected readonly languages = SUPPORTED_LANGUAGES;
  protected readonly schools = signal<School[]>([]);
  protected readonly saved = signal(false);
  protected selectedLanguage = this.locale.language();

  protected schoolId = '';
  protected firstName = '';
  protected lastName = '';
  protected classGrade = '';
  protected dateOfBirth = '';
  protected phone = '';

  constructor() {
    this.schoolsApi.getApproved().subscribe((list) => this.schools.set(list));
    this.profileApi.getMine().subscribe({
      next: (p: StudentProfile) => {
        this.schoolId = p.schoolId;
        this.firstName = p.firstName;
        this.lastName = p.lastName ?? '';
        this.classGrade = p.classGrade ?? '';
        this.dateOfBirth = p.dateOfBirth?.slice(0, 10) ?? '';
        this.phone = p.phone ?? '';
      },
      error: () => {}, // no profile yet — form just starts blank
    });
  }

  saveProfile(): void {
    if (!this.schoolId || !this.firstName) return;
    this.profileApi
      .upsertMine({
        schoolId: this.schoolId,
        firstName: this.firstName,
        lastName: this.lastName || undefined,
        classGrade: this.classGrade || undefined,
        dateOfBirth: this.dateOfBirth || undefined,
        phone: this.phone || undefined,
      })
      .subscribe(() => this.saved.set(true));
  }
}
