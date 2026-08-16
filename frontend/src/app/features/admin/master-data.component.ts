import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationsService } from '../../core/services/locations.service';
import { CategoriesService } from '../../core/services/categories.service';
import { SettingsService } from '../../core/services/settings.service';
import { CompetitionCategory, CompetitionType, Location, PaymentSettings } from '../../core/models/domain.models';

@Component({
  selector: 'app-master-data',
  imports: [FormsModule],
  template: `
    <h1>Manage master data</h1>
    <p>Admin-only. City/state locations and competition categories used everywhere else in the
       app (school registration, competition creation, leaderboard filters) are managed here.</p>

    <div class="stack sections">
      <section class="card">
        <h2>Locations (cities)</h2>
        <div class="row add-row">
          <input [(ngModel)]="newCity" name="newCity" placeholder="City" />
          <input [(ngModel)]="newState" name="newState" placeholder="State" />
          <input [(ngModel)]="newDistrict" name="newDistrict" placeholder="District (optional)" />
          <input [(ngModel)]="newVillage" name="newVillage" placeholder="Village/town (optional)" />
          <button type="button" class="btn btn-primary" (click)="addLocation()">Add</button>
        </div>
        @if (locationError()) {
          <p class="error-text">{{ locationError() }}</p>
        }
        <table>
          <thead><tr><th>City</th><th>District</th><th>State</th><th>Country</th></tr></thead>
          <tbody>
            @for (l of locations(); track l.id) {
              <tr>
                <td>{{ l.city }}</td>
                <td>{{ l.district }}</td>
                <td>{{ l.state }}</td>
                <td>{{ l.country }}</td>
              </tr>
            } @empty {
              <tr><td colspan="4">No locations yet.</td></tr>
            }
          </tbody>
        </table>
      </section>

      <section class="card">
        <h2>Competition categories</h2>
        <div class="row add-row">
          <input [(ngModel)]="newCategoryName" name="newCategoryName" placeholder="e.g. Cricket (U-14)" />
          <select [(ngModel)]="newCategoryType" name="newCategoryType">
            <option value="Academic">Academic</option>
            <option value="Sports">Sports</option>
          </select>
          <button type="button" class="btn btn-primary" (click)="addCategory()">Add</button>
        </div>
        @if (categoryError()) {
          <p class="error-text">{{ categoryError() }}</p>
        }
        <table>
          <thead><tr><th>Name</th><th>Type</th><th></th></tr></thead>
          <tbody>
            @for (c of categories(); track c.id) {
              <tr>
                <td>{{ c.name }}</td>
                <td>{{ c.type }}</td>
                <td><button type="button" class="btn btn-outline" (click)="deleteCategory(c.id)">Delete</button></td>
              </tr>
            } @empty {
              <tr><td colspan="3">No categories yet.</td></tr>
            }
          </tbody>
        </table>
      </section>

      <section class="card">
        <h2>Payment settings</h2>
        <p>The UPI id contributors pay to on the Contribute page.</p>
        <div class="grid payment-grid">
          <div class="field">
            <label for="upiVpa">UPI VPA</label>
            <input id="upiVpa" [(ngModel)]="upiVpa" name="upiVpa" placeholder="yourname@bank" />
          </div>
          <div class="field">
            <label for="payeeName">Payee name</label>
            <input id="payeeName" [(ngModel)]="payeeName" name="payeeName" />
          </div>
        </div>
        <button type="button" class="btn btn-primary" (click)="savePayments()">Save</button>
        @if (paymentsSaved()) {
          <p class="notice">Saved — the Contribute page now uses this UPI id.</p>
        }
      </section>
    </div>
  `,
  styles: `
    .sections { max-width: 760px; }
    .add-row { flex-wrap: wrap; margin-bottom: var(--space-4); }
    .add-row input, .add-row select { flex: 1; min-width: 140px; }
    .error-text { color: var(--danger-500); font-weight: 600; margin-bottom: var(--space-4); }
    .payment-grid { grid-template-columns: repeat(2, 1fr); gap: var(--space-4); margin-bottom: var(--space-4); }
    @media (max-width: 560px) { .payment-grid { grid-template-columns: 1fr; } }
  `,
})
export class MasterDataComponent {
  private locationsApi = inject(LocationsService);
  private categoriesApi = inject(CategoriesService);
  private settingsApi = inject(SettingsService);

  protected readonly locations = signal<Location[]>([]);
  protected readonly categories = signal<CompetitionCategory[]>([]);
  protected readonly locationError = signal<string | null>(null);
  protected readonly categoryError = signal<string | null>(null);
  protected readonly paymentsSaved = signal(false);

  protected newCity = '';
  protected newState = '';
  protected newDistrict = '';
  protected newVillage = '';
  protected newCategoryName = '';
  protected newCategoryType: CompetitionType = 'Academic';
  protected upiVpa = '';
  protected payeeName = '';

  constructor() {
    this.reloadLocations();
    this.reloadCategories();
    this.settingsApi.getPayments().subscribe((s: PaymentSettings) => {
      this.upiVpa = s.upiVpa ?? '';
      this.payeeName = s.payeeName;
    });
  }

  addLocation(): void {
    this.locationError.set(null);
    if (!this.newCity || !this.newState) return;
    this.locationsApi
      .create({
        city: this.newCity,
        state: this.newState,
        district: this.newDistrict || undefined,
        villageOrTown: this.newVillage || undefined,
      })
      .subscribe({
        next: () => {
          this.newCity = this.newState = this.newDistrict = this.newVillage = '';
          this.reloadLocations();
        },
        error: () => this.locationError.set('Could not add that location — sign-in may have expired.'),
      });
  }

  addCategory(): void {
    this.categoryError.set(null);
    if (!this.newCategoryName) return;
    this.categoriesApi.create({ name: this.newCategoryName, type: this.newCategoryType }).subscribe({
      next: () => {
        this.newCategoryName = '';
        this.reloadCategories();
      },
      error: () => this.categoryError.set('Could not add that category — sign-in may have expired.'),
    });
  }

  deleteCategory(id: string): void {
    this.categoriesApi.delete(id).subscribe(() => this.reloadCategories());
  }

  savePayments(): void {
    if (!this.upiVpa) return;
    this.paymentsSaved.set(false);
    this.settingsApi
      .updatePayments({ upiVpa: this.upiVpa, payeeName: this.payeeName || 'Pratiyogita' })
      .subscribe(() => this.paymentsSaved.set(true));
  }

  private reloadLocations(): void {
    this.locationsApi.getAll().subscribe((list) => this.locations.set(list));
  }

  private reloadCategories(): void {
    this.categoriesApi.getAll().subscribe((list) => this.categories.set(list));
  }
}
