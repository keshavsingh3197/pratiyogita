import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { TopContributor, TopperEntry } from '../../core/models/domain.models';

@Component({
  selector: 'app-leaderboard',
  imports: [FormsModule],
  template: `
    <h1>Toppers</h1>
    <label>City <input [(ngModel)]="city" (change)="reload()" /></label>
    <label>Category <input [(ngModel)]="category" (change)="reload()" /></label>
    <table>
      <thead><tr><th>Rank</th><th>Student</th><th>School</th><th>City</th><th>Competition</th><th>Score</th></tr></thead>
      <tbody>
        @for (t of toppers(); track t.studentProfileId + t.competitionId) {
          <tr>
            <td>{{ t.rank }}</td><td>{{ t.studentName }}</td><td>{{ t.schoolName }}</td>
            <td>{{ t.city }}</td><td>{{ t.competitionName }}</td><td>{{ t.score }}</td>
          </tr>
        }
      </tbody>
    </table>

    <h2>Top contributors</h2>
    <ol>
      @for (c of contributors(); track c.name) {
        <li>{{ c.name }} — ₹{{ c.total }} ({{ c.contributionCount }} contribution(s))</li>
      }
    </ol>
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
