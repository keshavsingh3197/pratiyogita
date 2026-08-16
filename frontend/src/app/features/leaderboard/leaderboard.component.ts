import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { TopContributor, TopperEntry } from '../../core/models/domain.models';

@Component({
  selector: 'app-leaderboard',
  imports: [FormsModule],
  template: `
    <h1>Toppers</h1>
    <p>Filter by city or category to see location/field-wise rankings.</p>

    <div class="card filter-bar">
      <div class="field">
        <label for="city">City</label>
        <input id="city" [(ngModel)]="city" (change)="reload()" placeholder="e.g. Bengaluru" />
      </div>
      <div class="field">
        <label for="category">Category</label>
        <input id="category" [(ngModel)]="category" (change)="reload()" placeholder="e.g. Cricket" />
      </div>
    </div>

    @if (toppers().length) {
      <table>
        <thead>
          <tr><th>Rank</th><th>Student</th><th>School</th><th>City</th><th>Competition</th><th>Score</th></tr>
        </thead>
        <tbody>
          @for (t of toppers(); track t.studentProfileId + t.competitionId) {
            <tr>
              <td>{{ t.rank }}</td>
              <td>{{ t.studentName }}</td>
              <td>{{ t.schoolName }}</td>
              <td>{{ t.city }}</td>
              <td>{{ t.competitionName }}</td>
              <td>{{ t.score }}</td>
            </tr>
          }
        </tbody>
      </table>
    } @else {
      <div class="empty-state">No results published yet for these filters.</div>
    }

    <h2 class="contributors-heading">Top contributors</h2>
    @if (contributors().length) {
      <ol class="contributors-list">
        @for (c of contributors(); track c.name; let i = $index) {
          <li class="card row">
            <span class="rank">#{{ i + 1 }}</span>
            <span class="name">{{ c.name }}</span>
            <span class="amount">₹{{ c.total }}</span>
            <span class="badge">{{ c.contributionCount }} contribution(s)</span>
          </li>
        }
      </ol>
    } @else {
      <div class="empty-state">No verified contributions yet.</div>
    }
  `,
  styles: `
    .filter-bar { display: flex; gap: var(--space-6); margin-bottom: var(--space-6); flex-wrap: wrap; }
    .filter-bar .field { margin-bottom: 0; min-width: 200px; }
    .contributors-heading { margin-top: var(--space-8); }
    .contributors-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: var(--space-3); }
    .contributors-list li { justify-content: flex-start; }
    .rank { font-weight: 800; color: var(--brand-600); width: 2.5rem; }
    .name { flex: 1; font-weight: 600; }
    .amount { font-weight: 700; }
  `,
})
export class LeaderboardComponent {
  private leaderboardApi = inject(LeaderboardService);
  protected readonly toppers = signal<TopperEntry[]>([]);
  protected readonly contributors = signal<TopContributor[]>([]);
  protected city = '';
  protected category = '';

  constructor() {
    this.reload();
    this.leaderboardApi.getTopContributors().subscribe((list) => this.contributors.set(list));
  }

  reload(): void {
    this.leaderboardApi
      .getToppers({ city: this.city || undefined, category: this.category || undefined })
      .subscribe((list) => this.toppers.set(list));
  }
}
