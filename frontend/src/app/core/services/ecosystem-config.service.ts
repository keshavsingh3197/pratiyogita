import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EcosystemWebsiteItem {
  key: string;
  name: string;
  shortLabel: string;
  tagline: string;
  url: string;
  icon: string;
  sortOrder: number;
  showInEcosystemBar: boolean;
}

export interface CentralChannelItem {
  id: string;
  key: string;
  name: string;
  handle: string;
  platform: string;
  url: string;
  tagline?: string;
  showInFooter: boolean;
  isEnabled: boolean;
  sortOrder: number;
}

export interface AdMonetizationConfig {
  adsEnabled: boolean;
  adSenseClientId: string;
  headerAdSlotId: string;
  inFeedAdSlotId: string;
  sidebarAdSlotId: string;
  footerAdSlotId: string;
  adLabel: string;
  sponsorBannerEnabled: boolean;
  sponsorBadge: string;
  sponsorTitle: string;
  sponsorDescription: string;
  sponsorCtaText: string;
  sponsorUrl: string;
}

export interface BrandingSettings {
  siteName: string;
  tagline: string;
  heroBadge: string;
  heroTitle: string;
  heroSubtitle: string;
  centralConfigUrl: string;
  showEcosystemBar: boolean;
  ads?: AdMonetizationConfig | null;
  lastUpdatedAt?: string | null;
}

interface CentralConfigEnvelope {
  siteTitle?: string;
  values?: Record<string, string>;
  websites?: EcosystemWebsiteItem[];
  channels?: CentralChannelItem[];
}

@Injectable({ providedIn: 'root' })
export class EcosystemConfigService {
  private http = inject(HttpClient);
  private loaded = false;

  readonly siteTitle = signal<string>('');
  readonly values = signal<Record<string, string>>({});
  readonly websites = signal<EcosystemWebsiteItem[]>([]);
  readonly channels = signal<CentralChannelItem[]>([]);
  readonly branding = signal<BrandingSettings | null>(null);

  readonly ecosystemBarSites = computed(() =>
    this.websites()
      .filter((w) => w.showInEcosystemBar)
      .sort((a, b) => a.sortOrder - b.sortOrder)
  );

  readonly footerSites = computed(() =>
    this.websites()
      .filter((w) => w.showInEcosystemBar)
      .sort((a, b) => a.sortOrder - b.sortOrder)
  );

  readonly footerChannels = computed(() =>
    this.channels()
      .filter((c) => c.isEnabled !== false && c.showInFooter)
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
  );

  readonly creatorName = computed(
    () => this.values()['ecosystem.creatorName'] || this.siteTitle() || 'Creator Network'
  );

  readonly copyrightNotice = computed(() => this.values()['ecosystem.copyrightNotice'] || '');

  readonly adminPortalUrl = computed(
    () =>
      this.values()['ecosystem.adminUrl'] ||
      this.websites().find((w) => w.key === 'admin')?.url ||
      environment.loginUrl.replace(/\/login\/?$/, '')
  );

  readonly siteName = computed(
    () => this.branding()?.siteName || this.values()['pratiyogita.title'] || 'Pratiyogita'
  );

  readonly tagline = computed(
    () =>
      this.branding()?.tagline ||
      this.values()['pratiyogita.tagline'] ||
      'School exams, academic olympiads & sports tournaments platform.'
  );

  readonly heroBadge = computed(
    () => this.branding()?.heroBadge || 'School exams · Olympiads · Sports tournaments'
  );

  readonly heroTitle = computed(
    () => this.branding()?.heroTitle || 'One place for every competition, every result, every topper.'
  );

  readonly heroSubtitle = computed(
    () =>
      this.branding()?.heroSubtitle ||
      'Schedules, live registrations, published results and location/school-wise leaderboards — for academic exams and sports tournaments alike.'
  );

  readonly showEcosystemBar = computed(() => this.branding()?.showEcosystemBar ?? true);

  load(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.refresh();
  }

  refresh(): void {
    this.http
      .get<BrandingSettings>(`${environment.apiUrl}/settings/branding`)
      .pipe(catchError(() => of(null)))
      .subscribe((b) => {
        if (b) this.branding.set(b);
        const centralUrl = b?.centralConfigUrl || `${environment.idpUrl}/config`;
        this.fetchCentralConfig(centralUrl);
      });
  }

  private fetchCentralConfig(url: string): void {
    this.http
      .get<CentralConfigEnvelope>(url)
      .pipe(catchError(() => of(null)))
      .subscribe((res) => {
        if (!res) return;
        if (res.siteTitle) this.siteTitle.set(res.siteTitle);
        if (res.values) this.values.set(res.values);
        if (res.websites?.length) this.websites.set(res.websites);
        if (res.channels) this.channels.set(res.channels);
      });
  }
}
