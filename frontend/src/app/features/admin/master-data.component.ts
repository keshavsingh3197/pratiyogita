import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationsService } from '../../core/services/locations.service';
import { CategoriesService } from '../../core/services/categories.service';
import { SettingsService } from '../../core/services/settings.service';
import { AdMonetizationConfig, EcosystemConfigService } from '../../core/services/ecosystem-config.service';
import { CompetitionCategory, CompetitionType, Location, PaymentSettings } from '../../core/models/domain.models';

@Component({
  selector: 'app-master-data',
  imports: [FormsModule],
  template: `
    <h1>Manage master data &amp; platform settings</h1>
    <p>Admin-only. Locations, competition categories, payment target, and dynamic site branding are managed here.</p>

    <div class="stack sections">
      <section class="card">
        <h2>Site Branding &amp; Central Ecosystem</h2>
        <p>Controls the header title, landing hero text, and ecosystem switcher dynamically without redeployment.</p>
        <div class="grid payment-grid">
          <div class="field">
            <label for="siteName">Site Name</label>
            <input id="siteName" [(ngModel)]="siteName" name="siteName" />
          </div>
          <div class="field">
            <label for="heroBadge">Hero Badge</label>
            <input id="heroBadge" [(ngModel)]="heroBadge" name="heroBadge" />
          </div>
          <div class="field full-span">
            <label for="heroTitle">Hero Title</label>
            <input id="heroTitle" [(ngModel)]="heroTitle" name="heroTitle" />
          </div>
          <div class="field full-span">
            <label for="heroSubtitle">Hero Subtitle</label>
            <textarea id="heroSubtitle" rows="2" [(ngModel)]="heroSubtitle" name="heroSubtitle"></textarea>
          </div>
          <div class="field full-span">
            <label for="tagline">Tagline</label>
            <input id="tagline" [(ngModel)]="tagline" name="tagline" />
          </div>
          <div class="field">
            <label for="centralUrl">Central Config URL</label>
            <input id="centralUrl" [(ngModel)]="centralConfigUrl" name="centralUrl" />
          </div>
          <div class="field checkbox-row">
            <label>
              <input type="checkbox" [(ngModel)]="showEcosystemBar" name="showEcosystemBar" />
              Show Top Ecosystem Switcher Bar
            </label>
          </div>
          <div class="field full-span">
            <hr />
            <strong>Google AdSense &amp; Sponsor Monetization (Runtime Config)</strong>
          </div>
          <div class="field checkbox-row full-span">
            <label>
              <input type="checkbox" [(ngModel)]="adsForm.adsEnabled" name="adsEnabled" />
              Enable Google AdSense Slots
            </label>
          </div>
          <div class="field">
            <label for="adClientId">AdSense Client ID (ca-pub-...)</label>
            <input id="adClientId" [(ngModel)]="adsForm.adSenseClientId" name="adClientId" placeholder="ca-pub-0000000000000000" />
          </div>
          <div class="field">
            <label for="adLabel">Ad Container Label</label>
            <input id="adLabel" [(ngModel)]="adsForm.adLabel" name="adLabel" placeholder="Advertisement" />
          </div>
          <div class="field">
            <label for="inFeedSlot">In-Feed Ad Slot ID</label>
            <input id="inFeedSlot" [(ngModel)]="adsForm.inFeedAdSlotId" name="inFeedSlot" placeholder="10-digit slot ID" />
          </div>
          <div class="field">
            <label for="footerSlot">Footer Ad Slot ID</label>
            <input id="footerSlot" [(ngModel)]="adsForm.footerAdSlotId" name="footerSlot" placeholder="10-digit slot ID" />
          </div>
          <div class="field checkbox-row full-span">
            <label>
              <input type="checkbox" [(ngModel)]="adsForm.sponsorBannerEnabled" name="sponsorEnabled" />
              Enable Sponsor Banner Fallback
            </label>
          </div>
          <div class="field">
            <label for="sponsorTitle">Sponsor Title</label>
            <input id="sponsorTitle" [(ngModel)]="adsForm.sponsorTitle" name="sponsorTitle" placeholder="Study Kits / Sponsor" />
          </div>
          <div class="field">
            <label for="sponsorUrl">Sponsor URL (https://)</label>
            <input id="sponsorUrl" [(ngModel)]="adsForm.sponsorUrl" name="sponsorUrl" placeholder="https://..." />
          </div>
        </div>
        <div class="row action-row">
          <button type="button" class="btn btn-primary" (click)="saveBranding()">Save branding settings</button>
          <a [href]="ecosystem.adminPortalUrl() + '/website'" target="_blank" rel="noopener" class="btn btn-outline">
            Manage Websites &amp; Channels in Central Admin →
          </a>
        </div>
        @if (brandingSaved()) {
          <p class="notice">Branding settings saved and applied live.</p>
        }
        <p class="meta-hint">
          Synced {{ ecosystem.websites().length }} ecosystem app(s) and {{ ecosystem.footerChannels().length }} footer channel(s) from Central Config.
        </p>
      </section>

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
    .sections { max-width: 820px; }
    .add-row { flex-wrap: wrap; margin-bottom: var(--space-4); }
    .add-row input, .add-row select { flex: 1; min-width: 140px; }
    .error-text { color: var(--danger-500); font-weight: 600; margin-bottom: var(--space-4); }
    .payment-grid { grid-template-columns: repeat(2, 1fr); gap: var(--space-4); margin-bottom: var(--space-4); }
    .full-span { grid-column: 1 / -1; }
    .checkbox-row { justify-content: flex-end; }
    .checkbox-row label { display: flex; align-items: center; gap: 0.45rem; font-weight: 600; }
    .action-row { flex-wrap: wrap; gap: var(--space-3); }
    .meta-hint { margin-top: var(--space-3); font-size: 0.85rem; color: var(--fg-muted); }
    @media (max-width: 560px) { .payment-grid { grid-template-columns: 1fr; } }
  `,
})
export class MasterDataComponent {
  private locationsApi = inject(LocationsService);
  private categoriesApi = inject(CategoriesService);
  private settingsApi = inject(SettingsService);
  protected readonly ecosystem = inject(EcosystemConfigService);

  protected readonly locations = signal<Location[]>([]);
  protected readonly categories = signal<CompetitionCategory[]>([]);
  protected readonly locationError = signal<string | null>(null);
  protected readonly categoryError = signal<string | null>(null);
  protected readonly paymentsSaved = signal(false);
  protected readonly brandingSaved = signal(false);

  protected newCity = '';
  protected newState = '';
  protected newDistrict = '';
  protected newVillage = '';
  protected newCategoryName = '';
  protected newCategoryType: CompetitionType = 'Academic';
  protected upiVpa = '';
  protected payeeName = '';

  protected siteName = 'Pratiyogita';
  protected tagline = '';
  protected heroBadge = '';
  protected heroTitle = '';
  protected heroSubtitle = '';
  protected centralConfigUrl = '';
  protected showEcosystemBar = true;
  protected adsForm: AdMonetizationConfig = this.emptyAds();

  constructor() {
    this.reloadLocations();
    this.reloadCategories();
    this.settingsApi.getPayments().subscribe((s: PaymentSettings) => {
      this.upiVpa = s.upiVpa ?? '';
      this.payeeName = s.payeeName;
    });
    this.settingsApi.getBranding().subscribe((b) => {
      this.siteName = b.siteName || 'Pratiyogita';
      this.tagline = b.tagline || '';
      this.heroBadge = b.heroBadge || '';
      this.heroTitle = b.heroTitle || '';
      this.heroSubtitle = b.heroSubtitle || '';
      this.centralConfigUrl = b.centralConfigUrl || '';
      this.showEcosystemBar = b.showEcosystemBar ?? true;
      this.adsForm = { ...this.emptyAds(), ...(b.ads || {}) };
    });
  }

  saveBranding(): void {
    this.brandingSaved.set(false);
    this.settingsApi
      .updateBranding({
        siteName: this.siteName || 'Pratiyogita',
        tagline: this.tagline,
        heroBadge: this.heroBadge,
        heroTitle: this.heroTitle,
        heroSubtitle: this.heroSubtitle,
        centralConfigUrl: this.centralConfigUrl,
        showEcosystemBar: this.showEcosystemBar,
        ads: { ...this.adsForm },
      })
      .subscribe(() => {
        this.ecosystem.refresh();
        this.brandingSaved.set(true);
      });
  }

  private emptyAds(): AdMonetizationConfig {
    return {
      adsEnabled: false,
      adSenseClientId: '',
      headerAdSlotId: '',
      inFeedAdSlotId: '',
      sidebarAdSlotId: '',
      footerAdSlotId: '',
      adLabel: 'Advertisement',
      sponsorBannerEnabled: false,
      sponsorBadge: 'Featured',
      sponsorTitle: '',
      sponsorDescription: '',
      sponsorCtaText: 'Learn More',
      sponsorUrl: ''
    };
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
      .updatePayments({ upiVpa: this.upiVpa, payeeName: this.payeeName || this.siteName || 'Pratiyogita' })
      .subscribe(() => this.paymentsSaved.set(true));
  }

  private reloadLocations(): void {
    this.locationsApi.getAll().subscribe((list) => this.locations.set(list));
  }

  private reloadCategories(): void {
    this.categoriesApi.getAll().subscribe((list) => this.categories.set(list));
  }
}
