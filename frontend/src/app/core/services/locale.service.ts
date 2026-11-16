import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export type LanguageCode = 'en' | 'hi';

const STORAGE_KEY = 'pratiyogita.language';
const OVERRIDDEN_KEY = 'pratiyogita.language.visitorSet';

export const SUPPORTED_LANGUAGES: { code: LanguageCode; label: string; shortLabel: string }[] = [
  { code: 'en', label: 'English', shortLabel: 'EN' },
  { code: 'hi', label: 'हिन्दी (Hindi)', shortLabel: 'हिं' },
];

export interface LocalizationPolicy {
  siteKey: string;
  defaultLocale: LanguageCode;
  enabledLocales: string[];
  allowVisitorLanguageSwitch: boolean;
  showLanguagePicker: boolean;
  forceLocale: boolean;
  controlledBy: 'admin' | 'site-override' | string;
  overrideCentralLocalization: boolean;
}

export interface SiteLocalizationConfigDto {
  id?: string;
  overrideCentralLocalization: boolean;
  defaultLocale: LanguageCode;
  enabledLocales: string[];
  allowVisitorLanguageSwitch: boolean;
  showLanguagePicker: boolean;
  forceLocale: boolean;
  localOverrides?: Record<string, Record<string, string>>;
}

export interface SiteLocalizationAdminState {
  centralAdminPolicy: LocalizationPolicy;
  localSettings: SiteLocalizationConfigDto;
  effectivePolicy: LocalizationPolicy;
}

interface BundleResponse {
  locale: LanguageCode;
  version: string;
  policy: LocalizationPolicy;
  entries: Record<string, string>;
  source: string;
}

const BUILTIN_EN: Record<string, string> = {
  'pratiyogita.nav.home': 'Home',
  'pratiyogita.nav.competitions': 'Competitions',
  'pratiyogita.nav.leaderboard': 'Leaderboard',
  'pratiyogita.nav.winners': 'Quiz Winners',
  'pratiyogita.nav.news': 'News',
  'pratiyogita.nav.contribute': 'Contribute',
  'pratiyogita.nav.manageData': 'Manage data',
  'pratiyogita.nav.registerSchool': 'Register school',
  'pratiyogita.nav.settings': 'Settings',
  'pratiyogita.home.exploreCompetitions': 'Browse competitions',
  'pratiyogita.home.viewLeaderboard': 'See toppers',
  'pratiyogita.home.viewWinners': 'Live Quiz Winners',
  'pratiyogita.home.updatesHighlights': 'Updates & Highlights:',
  'pratiyogita.home.card.compTitle': 'Competitions & Tournaments',
  'pratiyogita.home.card.compDesc': 'Academic olympiads and sports tournaments — schedules and registration.',
  'pratiyogita.home.card.lbTitle': 'Toppers & Contributors',
  'pratiyogita.home.card.lbDesc': "Rankings by school, city and category — plus who's supporting the platform.",
  'pratiyogita.home.card.newsTitle': 'Latest news',
  'pratiyogita.home.card.newsDesc': 'Results announcements, deadlines and upcoming events.',
  'pratiyogita.home.card.contribTitle': 'Contribute',
  'pratiyogita.home.card.contribDesc': 'Support via UPI — Google Pay, PhonePe, Paytm and more.',
  'pratiyogita.competitions.title': 'Competitions & Tournaments',
  'pratiyogita.competitions.subtitle': 'Academic exams/olympiads and sports tournaments, side by side.',
  'pratiyogita.competitions.empty': 'No competitions published yet — check back soon.',
  'pratiyogita.leaderboard.title': 'Top Performers',
  'pratiyogita.leaderboard.subtitle': 'Filter by city or category to see location/field-wise rankings.',
  'pratiyogita.leaderboard.city': 'City',
  'pratiyogita.leaderboard.allCities': 'All cities',
  'pratiyogita.leaderboard.category': 'Category',
  'pratiyogita.leaderboard.allCategories': 'All categories',
  'pratiyogita.leaderboard.topContributors': 'Top contributors',
  'pratiyogita.news.title': 'News',
  'pratiyogita.news.subtitle': "What's next — results announcements, deadlines and upcoming events.",
  'pratiyogita.news.empty': 'No news yet — check back soon.',
  'pratiyogita.contribute.title': 'Contribute',
  'pratiyogita.contribute.subtitle': 'Pay any amount via Google Pay, PhonePe, Paytm or any UPI app, then paste the reference number below so we can record and verify your contribution.',
  'pratiyogita.registerSchool.title': 'Register a school',
  'pratiyogita.registerSchool.subtitle': 'Submitted schools are reviewed by an admin before they appear publicly or can enter competitions.',
  'pratiyogita.registerSchool.submit': 'Submit for approval',
  'pratiyogita.settings.title': 'Settings',
  'pratiyogita.settings.myDetails': 'My details',
  'pratiyogita.settings.appearance': 'Appearance',
  'pratiyogita.settings.language': 'Language',
  'pratiyogita.settings.saveProfile': 'Save profile',
  'pratiyogita.settings.saved': 'Saved.',
  'pratiyogita.footer.support': 'Support platform',
  'ecosystem.language.label': 'Language',
};

const BUILTIN_HI: Record<string, string> = {
  'pratiyogita.nav.home': 'होम',
  'pratiyogita.nav.competitions': 'प्रतियोगिताएँ',
  'pratiyogita.nav.leaderboard': 'लीडरबोर्ड',
  'pratiyogita.nav.winners': 'क्विज़ विजेता',
  'pratiyogita.nav.news': 'समाचार',
  'pratiyogita.nav.contribute': 'योगदान दें',
  'pratiyogita.nav.manageData': 'डेटा प्रबंधन',
  'pratiyogita.nav.registerSchool': 'स्कूल पंजीकरण',
  'pratiyogita.nav.settings': 'सेटिंग्स',
  'pratiyogita.home.exploreCompetitions': 'प्रतियोगिताएँ देखें',
  'pratiyogita.home.viewLeaderboard': 'टॉपर्स देखें',
  'pratiyogita.home.viewWinners': 'लाइव क्विज़ विजेता',
  'pratiyogita.home.updatesHighlights': 'अपडेट और मुख्य आकर्षण:',
  'pratiyogita.home.card.compTitle': 'प्रतियोगिताएँ और टूर्नामेंट',
  'pratiyogita.home.card.compDesc': 'शैक्षणिक ओलंपियाड और खेल टूर्नामेंट — समय-सारणी और पंजीकरण।',
  'pratiyogita.home.card.lbTitle': 'टॉपर्स और योगदानकर्ता',
  'pratiyogita.home.card.lbDesc': 'स्कूल, शहर और श्रेणी के अनुसार रैंकिंग — और प्लेटफॉर्म के समर्थक।',
  'pratiyogita.home.card.newsTitle': 'ताज़ा समाचार',
  'pratiyogita.home.card.newsDesc': 'परिणाम घोषणाएँ, अंतिम तिथियाँ और आगामी कार्यक्रम।',
  'pratiyogita.home.card.contribTitle': 'योगदान दें',
  'pratiyogita.home.card.contribDesc': 'UPI द्वारा सहयोग करें — Google Pay, PhonePe, Paytm और अन्य।',
  'pratiyogita.competitions.title': 'प्रतियोगिताएँ और टूर्नामेंट',
  'pratiyogita.competitions.subtitle': 'शैक्षणिक परीक्षाएँ, ओलंपियाड और खेल टूर्नामेंट एक साथ।',
  'pratiyogita.competitions.empty': 'अभी कोई प्रतियोगिता प्रकाशित नहीं हुई है — जल्द देखें।',
  'pratiyogita.leaderboard.title': 'शीर्ष प्रदर्शनकर्ता',
  'pratiyogita.leaderboard.subtitle': 'शहर या श्रेणी के अनुसार रैंकिंग देखने के लिए फ़िल्टर करें।',
  'pratiyogita.leaderboard.city': 'शहर',
  'pratiyogita.leaderboard.allCities': 'सभी शहर',
  'pratiyogita.leaderboard.category': 'श्रेणी',
  'pratiyogita.leaderboard.allCategories': 'सभी श्रेणियाँ',
  'pratiyogita.leaderboard.topContributors': 'शीर्ष योगदानकर्ता',
  'pratiyogita.news.title': 'समाचार',
  'pratiyogita.news.subtitle': 'परिणाम घोषणाएँ, अंतिम तिथियाँ और आगामी कार्यक्रम।',
  'pratiyogita.news.empty': 'अभी कोई समाचार नहीं है — जल्द देखें।',
  'pratiyogita.contribute.title': 'योगदान दें',
  'pratiyogita.contribute.subtitle': 'Google Pay, PhonePe, Paytm या किसी भी UPI ऐप से भुगतान करें और संदर्भ संख्या दर्ज करें।',
  'pratiyogita.registerSchool.title': 'स्कूल पंजीकृत करें',
  'pratiyogita.registerSchool.subtitle': 'पंजीकृत स्कूलों की समीक्षा एडमिन द्वारा की जाती है।',
  'pratiyogita.registerSchool.submit': 'स्वीकृति के लिए भेजें',
  'pratiyogita.settings.title': 'सेटिंग्स',
  'pratiyogita.settings.myDetails': 'मेरा विवरण',
  'pratiyogita.settings.appearance': 'रूप और थीम',
  'pratiyogita.settings.language': 'भाषा',
  'pratiyogita.settings.saveProfile': 'प्रोफ़ाइल सहेजें',
  'pratiyogita.settings.saved': 'सहेजा गया।',
  'pratiyogita.footer.support': 'प्लेटफॉर्म का समर्थन करें',
  'ecosystem.language.label': 'भाषा',
};

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private http = inject(HttpClient);
  private loaded = false;

  readonly language = signal<LanguageCode>(this.readStored());
  readonly policy = signal<LocalizationPolicy>({
    siteKey: 'pratiyogita',
    defaultLocale: 'en',
    enabledLocales: ['en', 'hi'],
    allowVisitorLanguageSwitch: true,
    showLanguagePicker: true,
    forceLocale: false,
    controlledBy: 'admin',
    overrideCentralLocalization: false,
  });
  readonly entries = signal<Record<string, string>>({});

  readonly showPicker = computed(() => {
    const p = this.policy();
    return p.showLanguagePicker && !p.forceLocale && p.enabledLocales.length > 1;
  });

  readonly canSwitch = computed(() => {
    const p = this.policy();
    return p.allowVisitorLanguageSwitch && !p.forceLocale;
  });

  readonly availableLanguages = computed(() => {
    const enabled = new Set(this.policy().enabledLocales.map((x) => x.toLowerCase()));
    return SUPPORTED_LANGUAGES.filter((l) => enabled.has(l.code));
  });

  constructor() {
    effect(() => {
      const lang = this.language();
      localStorage.setItem(STORAGE_KEY, lang);
      if (typeof document !== 'undefined') {
        document.documentElement.lang = lang;
      }
    });
    this.init();
  }

  init(): void {
    if (this.loaded) return;
    this.loaded = true;
    const visitorSet = localStorage.getItem(OVERRIDDEN_KEY) === '1';
    const initial = this.language();

    this.http
      .get<BundleResponse>(`${environment.apiUrl}/i18n/bundle/${initial}`)
      .pipe(catchError(() => of(null)))
      .subscribe((res) => {
        if (!res) return;
        this.policy.set(res.policy);
        this.entries.set(res.entries || {});

        if (res.policy.forceLocale || !res.policy.allowVisitorLanguageSwitch || !visitorSet) {
          const target = this.normalize(res.policy.defaultLocale);
          if (target !== this.language()) {
            this.language.set(target);
            this.fetchBundle(target);
          }
        }
      });
  }

  set(code: LanguageCode, visitorChoice = true): void {
    if (visitorChoice && !this.canSwitch()) return;
    const normalized = this.normalize(code);
    if (visitorChoice) {
      localStorage.setItem(OVERRIDDEN_KEY, '1');
    }
    this.language.set(normalized);
    this.fetchBundle(normalized);
  }

  refresh(): void {
    this.fetchBundle(this.language(), true);
  }

  t(key: string, fallback?: string): string {
    const map = this.entries();
    if (map[key]) return map[key];
    const builtIn = this.language() === 'hi' ? BUILTIN_HI[key] : BUILTIN_EN[key];
    return builtIn || BUILTIN_EN[key] || fallback || key;
  }

  getSiteLocalizationAdminState() {
    return this.http.get<SiteLocalizationAdminState>(`${environment.apiUrl}/settings/localization`);
  }

  updateSiteLocalization(req: SiteLocalizationConfigDto) {
    return this.http
      .put<SiteLocalizationAdminState>(`${environment.apiUrl}/settings/localization`, req)
      .pipe(
        tap((state) => {
          this.policy.set(state.effectivePolicy);
          if (state.effectivePolicy.forceLocale || !state.effectivePolicy.allowVisitorLanguageSwitch) {
            this.set(this.normalize(state.effectivePolicy.defaultLocale), false);
          } else {
            this.refresh();
          }
        })
      );
  }

  private fetchBundle(code: LanguageCode, applyDefaultIfLocked = false): void {
    this.http
      .get<BundleResponse>(`${environment.apiUrl}/i18n/bundle/${code}`)
      .pipe(catchError(() => of(null)))
      .subscribe((res) => {
        if (!res) return;
        this.policy.set(res.policy);
        this.entries.set(res.entries || {});
        if (applyDefaultIfLocked && (res.policy.forceLocale || !res.policy.allowVisitorLanguageSwitch)) {
          this.language.set(this.normalize(res.policy.defaultLocale));
        }
      });
  }

  private normalize(raw: string | null | undefined): LanguageCode {
    return raw?.toLowerCase().startsWith('hi') ? 'hi' : 'en';
  }

  private readStored(): LanguageCode {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'hi' ? 'hi' : 'en';
  }
}
