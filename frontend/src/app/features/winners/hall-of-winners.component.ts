import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { EcosystemConfigService } from '../../core/services/ecosystem-config.service';
import { LocaleService } from '../../core/services/locale.service';
import {
  ActiveQuiz,
  QuizWinnerRecord,
  VerifiedYouTubeChannel,
  WinnersService,
} from '../../core/services/winners.service';
import { AdSlotComponent } from '../shared/ad-slot/ad-slot.component';
import { LoadingStateComponent } from '../shared/loading/loading-state.component';

const VERIFY_SESSION_KEY = 'pratiyogita.yt.verify.proof';

interface StoredProof {
  channel: VerifiedYouTubeChannel;
  verificationToken: string;
  expiresAtUtc: string;
}

@Component({
  selector: 'app-hall-of-winners',
  standalone: true,
  imports: [FormsModule, DatePipe, AdSlotComponent, LoadingStateComponent],
  template: `
    <section class="winners-head">
      <div>
        <span class="badge">🏆 {{ locale.t('pratiyogita.nav.winners', 'Quiz Winners') }}</span>
        <h1>{{ locale.t('pratiyogita.winners.title', 'Hall of Winners — Live Stream Quiz') }}</h1>
        <p class="muted">{{ locale.t('pratiyogita.winners.subtitle', 'Verified viewers who answered live stream questions correctly and won via random selection.') }}</p>
      </div>
    </section>

    <!-- Zero-Log Privacy Guarantee Banner -->
    <div class="card privacy-banner" role="note">
      <strong>🛡️ Zero-Log Verification</strong>
      <p>{{ locale.t('pratiyogita.winners.privacyNote', 'Channel Verification Privacy: YouTube Sign-In reads your public channel name only in memory and NEVER logs or stores OAuth tokens or private data.') }}</p>
    </div>

    @if (error(); as err) {
      <div class="alert-err" role="alert">{{ err }}</div>
    }
    @if (notice(); as msg) {
      <div class="alert-ok" role="status">{{ msg }}</div>
    }

    <!-- Active Live Stream Quiz Card -->
    <section class="card active-quiz-card">
      <div class="quiz-top-row">
        <div>
          <h2>📺 {{ locale.t('pratiyogita.winners.activeQuiz', 'Active Live Quiz') }}</h2>
          @if (activeQuiz(); as q) {
            <span class="muted">{{ q.streamTitle }} · {{ q.streamChannelHandle }}</span>
          }
        </div>
        <button type="button" class="btn btn-outline" (click)="loadActiveQuiz()" [disabled]="loadingActive()">
          ↻ {{ isHindi() ? 'रीफ़्रेश' : 'Refresh' }}
        </button>
      </div>

      @if (loadingActive()) {
        <app-loading-state variant="rows" [count]="2" [label]="isHindi() ? 'लाइव क्विज़ लोड हो रहा है...' : 'Checking live stream quiz…'" />
      } @else if (activeQuiz(); as q) {
        <div class="quiz-body">
          <div class="quiz-badges">
            <span class="pill" [class.pill-live]="q.status === 'Open'" [class.pill-drawn]="q.status === 'Drawn'">
              {{ q.status === 'Open' ? (isHindi() ? '🟢 लाइव — उत्तर खुले हैं' : '🟢 OPEN FOR ANSWERS') : (q.status === 'Drawn' ? (isHindi() ? '🏆 विजेता घोषित' : '🏆 WINNER DRAWN') : '⏳ CLOSED') }}
            </span>
            <span class="pill">👥 {{ q.totalEntrants }} {{ isHindi() ? 'सत्यापित प्रतिभागी' : 'verified entrants' }}</span>
            <span class="pill">🏅 {{ q.prizeTitle }}</span>
          </div>

          <h3 class="quiz-question">
            {{ isHindi() && q.questionHi ? q.questionHi : q.question }}
          </h3>
          @if (isHindi() && q.questionHi && q.question && q.questionHi !== q.question) {
            <p class="muted">{{ q.question }}</p>
          } @else if (!isHindi() && q.questionHi) {
            <p class="muted">{{ q.questionHi }}</p>
          }

          @if (q.winner; as w) {
            <div class="winner-spotlight">
              <div class="ws-head">🎉 {{ isHindi() ? 'लाइव स्ट्रीम विजेता' : 'LIVE WINNER REVEAL' }}</div>
              <div class="ws-row">
                @if (w.winnerThumbnailUrl) {
                  <img [src]="w.winnerThumbnailUrl" [alt]="w.winnerChannelTitle" class="winner-avatar" />
                } @else {
                  <div class="winner-avatar avatar-fallback">{{ w.winnerChannelTitle.charAt(0).toUpperCase() }}</div>
                }
                <div>
                  <strong>{{ w.winnerChannelTitle }}</strong>
                  @if (w.winnerChannelHandle) { <div class="muted">{{ w.winnerChannelHandle }}</div> }
                  <div class="muted" style="font-size: 0.85rem">
                    {{ w.prizeTitle }} · {{ w.drawAlgorithm }} · {{ w.selectedAtUtc | date: 'medium' }}
                  </div>
                </div>
              </div>
            </div>
          }

          <!-- Step 1: Zero-Log Verify -->
          <div class="verify-box">
            @if (verifiedChannel(); as ch) {
              <div class="verified-channel">
                @if (ch.thumbnailUrl) {
                  <img [src]="ch.thumbnailUrl" [alt]="ch.channelTitle" class="mini-avatar" />
                }
                <div>
                  <strong>✅ {{ isHindi() ? 'सत्यापित चैनल:' : 'Verified Channel:' }} {{ ch.channelTitle }}</strong>
                  <div class="muted" style="font-size: 0.82rem">
                    {{ ch.channelHandle || ch.channelId }} · {{ isHindi() ? 'अस्थायी मेमोरी प्रमाण सक्रिय' : 'Temporary in-session proof active' }}
                  </div>
                </div>
                <button type="button" class="btn btn-outline" (click)="clearVerified()" style="margin-left: auto">
                  {{ isHindi() ? 'चैनल बदलें' : 'Switch Channel' }}
                </button>
              </div>
            } @else {
              <div class="verify-cta">
                <span>{{ isHindi() ? 'उत्तर सबमिट करने के लिए अपना यूट्यूब चैनल सत्यापित करें (कोई टोकन सेव नहीं होता):' : 'Verify your public YouTube channel to enter the draw (zero tokens logged or stored):' }}</span>
                <button type="button" class="btn btn-primary" (click)="startYouTubeVerify()"
                        [disabled]="verifying() || !youTubeVerifyConfigured()">
                  {{ verifying() ? (isHindi() ? 'सत्यापन जारी...' : 'Connecting…') : '▶ ' + locale.t('pratiyogita.winners.signInYouTube', 'Verify with YouTube Channel (Zero-Log)') }}
                </button>
              </div>
              @if (!youTubeVerifyConfigured()) {
                <p class="muted" style="font-size: 0.82rem; margin-top: 0.4rem">
                  {{ isHindi() ? 'नोट: Google OAuth क्रेडेंशियल अभी कॉन्फ़िगर हो रहे हैं — आप अभी भी YouTube Live Chat में !ans A/B/C/D लिखकर भाग ले सकते हैं।' : 'Google OAuth web verification is not enabled on this server yet — viewers can reply in YouTube Live Chat with !ans A/B/C/D.' }}
                </p>
              }
            }
          </div>

          <!-- Step 2: Select Option & Submit -->
          <div class="options-grid">
            @for (opt of q.options; track opt.key) {
              <button type="button"
                      class="option-btn"
                      [class.selected]="selectedOption() === opt.key"
                      [class.correct]="q.revealedCorrectOptionKey === opt.key"
                      [disabled]="q.status !== 'Open'"
                      (click)="selectedOption.set(opt.key)">
                <span class="opt-key">{{ opt.key }}</span>
                <span class="opt-label">
                  <strong>{{ isHindi() && opt.textHi ? opt.textHi : opt.text }}</strong>
                  @if (isHindi() && opt.textHi && opt.text && opt.textHi !== opt.text) {
                    <small>{{ opt.text }}</small>
                  }
                </span>
              </button>
            }
          </div>

          @if (q.status === 'Open') {
            <div class="submit-row">
              <button type="button" class="btn btn-primary"
                      (click)="submitAnswer()"
                      [disabled]="submitting() || !verifiedChannel() || !selectedOption()">
                {{ submitting() ? (isHindi() ? 'सबमिट हो रहा है...' : 'Submitting…') : locale.t('pratiyogita.winners.submitAnswer', 'Submit Answer') }}
              </button>
            </div>
          }
        </div>
      } @else {
        <div class="empty-state">
          {{ isHindi() ? 'अभी कोई लाइव क्विज़ राउंड सक्रिय नहीं है। अगले लाइव स्ट्रीम के दौरान यहाँ जुड़ें!' : 'No live stream quiz round is open right now. Join during our next live broadcast!' }}
        </div>
      }
    </section>

    <app-ad-slot placement="infeed" />

    <!-- Past Winners Timeline -->
    <section class="timeline-section">
      <div class="timeline-head">
        <h2>📜 {{ locale.t('pratiyogita.winners.historyTitle', 'Past Winners Timeline') }}</h2>
        <div class="filters">
          <select [(ngModel)]="channelFilter" (ngModelChange)="loadWinners()" aria-label="Filter by channel">
            <option value="">{{ isHindi() ? 'सभी चैनल' : 'All Channels' }}</option>
            @for (ch of channelOptions(); track ch) {
              <option [value]="ch">{{ ch }}</option>
            }
          </select>
          <input type="search" [(ngModel)]="searchQuery" (keyup.enter)="loadWinners()"
                 [placeholder]="isHindi() ? 'विजेता या प्रश्न खोजें...' : 'Search winner, stream, or question…'" />
          <button type="button" class="btn btn-outline" (click)="loadWinners()">{{ isHindi() ? 'खोजें' : 'Search' }}</button>
        </div>
      </div>

      @if (loadingHistory()) {
        <app-loading-state variant="rows" [count]="3" [label]="isHindi() ? 'विजेता इतिहास लोड हो रहा है...' : 'Loading past winners…'" />
      } @else if (winners().length) {
        <div class="winners-grid">
          @for (w of winners(); track w.id) {
            <article class="card winner-card">
              <div class="wc-top">
                <div class="wc-identity">
                  @if (w.winnerThumbnailUrl) {
                    <img [src]="w.winnerThumbnailUrl" [alt]="w.winnerChannelTitle" class="winner-avatar-sm" />
                  } @else {
                    <div class="winner-avatar-sm avatar-fallback">{{ w.winnerChannelTitle.charAt(0).toUpperCase() }}</div>
                  }
                  <div>
                    <strong>{{ w.winnerChannelTitle }}</strong>
                    <div class="muted" style="font-size: 0.82rem">{{ w.winnerChannelHandle || w.winnerChannelId }}</div>
                  </div>
                </div>
                <span class="pill">{{ w.streamChannelHandle }}</span>
              </div>

              <div class="wc-question">
                <strong>{{ isHindi() && w.questionHi ? w.questionHi : w.question }}</strong>
                <div class="muted" style="font-size: 0.84rem">
                  ✅ {{ w.correctOptionKey }}: {{ w.correctOptionText }}
                </div>
              </div>

              <div class="wc-meta muted">
                <span>📺 {{ w.streamTitle }}</span>
                <span>🏅 {{ w.prizeTitle }}</span>
                <span>📅 {{ w.selectedAtUtc | date: 'medium' }} · {{ w.eligibleEntrants }}/{{ w.totalEntrants }}</span>
              </div>
            </article>
          }
        </div>
      } @else {
        <div class="empty-state">
          {{ isHindi() ? 'अभी तक कोई विजेता दर्ज नहीं हुआ है — पहले लाइव क्विज़ में भाग लें!' : 'No past quiz winners recorded yet — join the first live broadcast!' }}
        </div>
      }
    </section>
  `,
  styles: [`
    .winners-head { margin-bottom: var(--space-4); }
    .privacy-banner {
      border-left: 4px solid var(--ok-500); background: color-mix(in srgb, var(--ok-100) 35%, var(--surface));
      margin-bottom: var(--space-4);
    }
    .privacy-banner p { margin: 0.25rem 0 0; font-size: 0.92rem; }
    .active-quiz-card { margin-bottom: var(--space-6); display: grid; gap: var(--space-4); }
    .quiz-top-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; flex-wrap: wrap; }
    .quiz-body { display: grid; gap: var(--space-4); }
    .quiz-badges { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .pill {
      display: inline-block; padding: 0.2rem 0.65rem; border-radius: 999px; font-size: 0.78rem;
      font-weight: 600; border: 1px solid var(--border); background: var(--bg);
    }
    .pill-live { border-color: var(--ok-500); color: var(--ok-500); }
    .pill-drawn { border-color: var(--accent-500); color: var(--accent-600); }
    .quiz-question { font-size: 1.25rem; margin: 0; }
    .winner-spotlight {
      padding: var(--space-4); border-radius: var(--radius);
      border: 2px solid var(--accent-500); background: color-mix(in srgb, var(--warn-100) 45%, var(--surface));
      display: grid; gap: 0.6rem;
    }
    .ws-head { font-weight: 800; font-size: 0.85rem; color: var(--accent-600); }
    .ws-row, .verified-channel, .wc-identity { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
    .winner-avatar { width: 56px; height: 56px; border-radius: 50%; object-fit: cover; border: 2px solid var(--accent-500); }
    .winner-avatar-sm, .mini-avatar { width: 42px; height: 42px; border-radius: 50%; object-fit: cover; border: 1px solid var(--border); }
    .avatar-fallback { display: grid; place-items: center; font-weight: 800; background: var(--brand-600); color: #fff; }
    .verify-box { padding: var(--space-3); border-radius: var(--radius); border: 1px dashed var(--border); background: var(--bg); }
    .verify-cta { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
    .options-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(230px, 1fr)); gap: 0.65rem; }
    .option-btn {
      display: flex; align-items: center; gap: 0.65rem; text-align: left; padding: 0.75rem;
      border-radius: var(--radius); border: 1px solid var(--border); background: var(--surface);
      color: var(--fg); cursor: pointer; transition: border-color 0.15s, background 0.15s;
    }
    .option-btn:hover:not(:disabled) { border-color: var(--brand-500); }
    .option-btn.selected { border-color: var(--brand-600); background: color-mix(in srgb, var(--brand-100) 45%, var(--surface)); }
    .option-btn.correct { border-color: var(--ok-500); background: color-mix(in srgb, var(--ok-100) 45%, var(--surface)); }
    .opt-key {
      width: 1.9rem; height: 1.9rem; border-radius: 8px; display: grid; place-items: center;
      font-weight: 800; background: var(--brand-600); color: #fff; flex-shrink: 0;
    }
    .opt-label { display: grid; gap: 0.1rem; }
    .opt-label small { color: var(--fg-muted); }
    .submit-row { display: flex; justify-content: flex-end; }
    .timeline-section { margin-top: var(--space-6); display: grid; gap: var(--space-4); }
    .timeline-head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .filters { display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center; }
    .winners-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(290px, 1fr)); gap: var(--space-4); }
    .winner-card { display: grid; gap: 0.75rem; align-content: start; }
    .wc-top { display: flex; justify-content: space-between; align-items: flex-start; gap: 0.5rem; }
    .wc-meta { display: grid; gap: 0.2rem; font-size: 0.82rem; }
    .alert-err { padding: 0.75rem; border-radius: var(--radius); background: var(--danger-100); color: var(--danger-500); margin-bottom: var(--space-4); }
    .alert-ok { padding: 0.75rem; border-radius: var(--radius); background: var(--ok-100); color: var(--ok-500); margin-bottom: var(--space-4); }
    .empty-state { text-align: center; padding: var(--space-6); color: var(--fg-muted); }
    .muted { color: var(--fg-muted); }
  `],
})
export class HallOfWinnersComponent implements OnInit {
  private readonly winnersService = inject(WinnersService);
  private readonly ecosystem = inject(EcosystemConfigService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly locale = inject(LocaleService);

  readonly activeQuiz = signal<ActiveQuiz | null>(null);
  readonly youTubeVerifyConfigured = signal(false);
  readonly winners = signal<QuizWinnerRecord[]>([]);
  readonly verifiedChannel = signal<VerifiedYouTubeChannel | null>(null);
  readonly verificationToken = signal<string | null>(null);
  readonly selectedOption = signal<string>('');

  readonly loadingActive = signal(true);
  readonly loadingHistory = signal(true);
  readonly verifying = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  channelFilter = '';
  searchQuery = '';

  readonly channelOptions = computed(() => {
    const central = this.ecosystem.channels().map((c) => c.handle).filter((h) => !!h);
    const defaults = ['@keshavsinghworld', '@omkarshivbhakti', '@KESHAV-ANIM-FF', '@pratiyogitaindia'];
    return Array.from(new Set([...central, ...defaults]));
  });

  isHindi(): boolean {
    return this.locale.language() === 'hi';
  }

  async ngOnInit(): Promise<void> {
    this.restoreProof();
    const qp = this.route.snapshot.queryParamMap;
    const code = qp.get('code');
    const state = qp.get('state');

    if (code && state) {
      // Strip transient OAuth query params from browser URL immediately.
      void this.router.navigate([], { replaceUrl: true, queryParams: {} });
      await this.finishYouTubeVerify(code, state);
    }

    await Promise.all([this.loadActiveQuiz(), this.loadWinners()]);
  }

  async loadActiveQuiz(): Promise<void> {
    this.loadingActive.set(true);
    try {
      const res = await firstValueFrom(this.winnersService.getActiveQuiz());
      this.youTubeVerifyConfigured.set(res.youTubeVerifyConfigured);
      this.activeQuiz.set(res.activeQuiz);
    } catch {
      this.activeQuiz.set(null);
    } finally {
      this.loadingActive.set(false);
    }
  }

  async loadWinners(): Promise<void> {
    this.loadingHistory.set(true);
    try {
      const list = await firstValueFrom(this.winnersService.listWinners(this.channelFilter, this.searchQuery));
      this.winners.set(list);
    } catch {
      this.winners.set([]);
    } finally {
      this.loadingHistory.set(false);
    }
  }

  async startYouTubeVerify(): Promise<void> {
    this.verifying.set(true);
    this.error.set(null);
    try {
      const redirectUri = this.currentRedirectUri();
      const res = await firstValueFrom(this.winnersService.getYouTubeAuthUrl(redirectUri));
      window.location.href = res.authorizationUrl;
    } catch (err: unknown) {
      this.verifying.set(false);
      this.error.set(this.readError(err, 'Could not start zero-log YouTube verification.'));
    }
  }

  private async finishYouTubeVerify(code: string, state: string): Promise<void> {
    this.verifying.set(true);
    this.error.set(null);
    try {
      const res = await firstValueFrom(
        this.winnersService.completeYouTubeVerify(code, state, this.currentRedirectUri())
      );
      this.verifiedChannel.set(res.channel);
      this.verificationToken.set(res.verificationToken);
      this.saveProof({
        channel: res.channel,
        verificationToken: res.verificationToken,
        expiresAtUtc: res.expiresAtUtc,
      });
      this.notice.set(
        this.isHindi()
          ? `चैनल सत्यापित: ${res.channel.channelTitle} (कोई टोकन सेव नहीं किया गया)`
          : `Verified channel: ${res.channel.channelTitle} (zero tokens logged or stored)`
      );
    } catch (err: unknown) {
      this.error.set(this.readError(err, 'YouTube channel verification failed. Please try again.'));
    } finally {
      this.verifying.set(false);
    }
  }

  async submitAnswer(): Promise<void> {
    const q = this.activeQuiz();
    const token = this.verificationToken();
    const opt = this.selectedOption();
    if (!q || !token || !opt) return;

    this.submitting.set(true);
    this.error.set(null);
    this.notice.set(null);
    try {
      const res = await firstValueFrom(this.winnersService.submitAnswer(q.id, token, opt));
      this.notice.set(
        this.isHindi()
          ? `आपका उत्तर (${res.answerOption}) ${res.channelTitle} के लिए दर्ज हो गया!`
          : `Answer (${res.answerOption}) recorded for ${res.channelTitle}! Good luck in the random draw!`
      );
      await this.loadActiveQuiz();
    } catch (err: unknown) {
      this.error.set(this.readError(err, 'Could not submit answer.'));
    } finally {
      this.submitting.set(false);
    }
  }

  clearVerified(): void {
    this.verifiedChannel.set(null);
    this.verificationToken.set(null);
    try {
      sessionStorage.removeItem(VERIFY_SESSION_KEY);
    } catch {
      // ignore storage failure
    }
  }

  private currentRedirectUri(): string {
    return `${window.location.origin}/winners`;
  }

  private saveProof(proof: StoredProof): void {
    try {
      sessionStorage.setItem(VERIFY_SESSION_KEY, JSON.stringify(proof));
    } catch {
      // session-only convenience
    }
  }

  private restoreProof(): void {
    try {
      const raw = sessionStorage.getItem(VERIFY_SESSION_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as StoredProof;
      if (new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
        sessionStorage.removeItem(VERIFY_SESSION_KEY);
        return;
      }
      this.verifiedChannel.set(parsed.channel);
      this.verificationToken.set(parsed.verificationToken);
    } catch {
      // ignore corrupt session cache
    }
  }

  private readError(err: unknown, fallback: string): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const e = (err as { error?: { message?: string } }).error;
      if (e?.message) return e.message;
    }
    return fallback;
  }
}
