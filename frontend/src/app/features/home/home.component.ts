import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <h1>Pratiyogita</h1>
    <p>School exams, olympiads and sports tournaments — schedules, results and toppers, in one place.</p>
    <ul>
      <li><a routerLink="/competitions">Browse competitions &amp; tournaments</a></li>
      <li><a routerLink="/leaderboard">See toppers &amp; top contributors</a></li>
      <li><a routerLink="/news">Latest news</a></li>
      <li><a routerLink="/contribute">Contribute / sponsor</a></li>
    </ul>
  `,
})
export class HomeComponent {}
