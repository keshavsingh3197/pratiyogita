import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface QuizOption {
  key: string;
  text: string;
  textHi?: string | null;
}

export interface VerifiedYouTubeChannel {
  channelId: string;
  channelTitle: string;
  channelHandle?: string | null;
  thumbnailUrl?: string | null;
  verifiedAtUtc: string;
}

export interface ActiveQuizWinner {
  winnerId: string;
  winnerChannelId: string;
  winnerChannelTitle: string;
  winnerChannelHandle?: string | null;
  winnerThumbnailUrl?: string | null;
  source: string;
  prizeTitle: string;
  totalEntrants: number;
  eligibleEntrants: number;
  drawAlgorithm: string;
  selectedAtUtc: string;
}

export interface ActiveQuiz {
  id: string;
  streamTitle: string;
  streamChannelHandle: string;
  broadcastId?: string | null;
  question: string;
  questionHi?: string | null;
  options: QuizOption[];
  revealedCorrectOptionKey?: string | null;
  prizeTitle: string;
  language: string;
  status: string;
  totalEntrants: number;
  createdAtUtc: string;
  closedAtUtc?: string | null;
  drawnAtUtc?: string | null;
  winner?: ActiveQuizWinner | null;
}

export interface ActiveQuizStatusResponse {
  youTubeVerifyConfigured: boolean;
  activeQuiz: ActiveQuiz | null;
}

export interface QuizWinnerRecord {
  id: string;
  sessionId: string;
  streamTitle: string;
  broadcastId?: string | null;
  streamChannelHandle: string;
  streamDateUtc: string;
  question: string;
  questionHi?: string | null;
  correctOptionKey: string;
  correctOptionText: string;
  winnerChannelId: string;
  winnerChannelTitle: string;
  winnerChannelHandle?: string | null;
  winnerThumbnailUrl?: string | null;
  winnerSource: string;
  prizeTitle: string;
  totalEntrants: number;
  eligibleEntrants: number;
  drawAlgorithm: string;
  selectedAtUtc: string;
  verifiedByYouTubeReadonly: boolean;
}

export interface YouTubeVerifyCompleteResponse {
  channel: VerifiedYouTubeChannel;
  verificationToken: string;
  expiresAtUtc: string;
  zeroLogGuarantee: string;
}

export interface SubmitQuizAnswerResponse {
  entryId: string;
  sessionId: string;
  channelId: string;
  channelTitle: string;
  channelHandle?: string | null;
  answerOption: string;
  submittedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class WinnersService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/winners`;

  listWinners(channel?: string, q?: string): Observable<QuizWinnerRecord[]> {
    let params = new HttpParams();
    if (channel?.trim()) params = params.set('channel', channel.trim());
    if (q?.trim()) params = params.set('q', q.trim());
    return this.http.get<QuizWinnerRecord[]>(this.base, { params });
  }

  getActiveQuiz(): Observable<ActiveQuizStatusResponse> {
    return this.http.get<ActiveQuizStatusResponse>(`${this.base}/active-quiz`);
  }

  getYouTubeAuthUrl(redirectUri: string): Observable<{ authorizationUrl: string; scope: string; zeroLogGuarantee: string }> {
    return this.http.get<{ authorizationUrl: string; scope: string; zeroLogGuarantee: string }>(
      `${this.base}/youtube-auth-url`,
      { params: { redirectUri } }
    );
  }

  completeYouTubeVerify(code: string, state: string, redirectUri: string): Observable<YouTubeVerifyCompleteResponse> {
    return this.http.post<YouTubeVerifyCompleteResponse>(`${this.base}/youtube-verify`, {
      code,
      state,
      redirectUri,
    });
  }

  submitAnswer(sessionId: string, verificationToken: string, optionKey: string): Observable<SubmitQuizAnswerResponse> {
    return this.http.post<SubmitQuizAnswerResponse>(`${this.base}/submit-answer`, {
      sessionId,
      verificationToken,
      optionKey,
    });
  }
}
