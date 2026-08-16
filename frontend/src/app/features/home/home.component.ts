import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <section class="hero">
      <span class="badge">School exams &middot; Olympiads &middot; Sports tournaments</span>
      <h1>One place for every competition, every result, every topper.</h1>
      <p class="hero-subtitle">
        Schedules, live registrations, published results and location/school-wise leaderboards —
        for academic exams and sports tournaments alike.
      </p>
      <div class="row">
        <a routerLink="/competitions" class="btn btn-primary">Browse competitions</a>
        <a routerLink="/leaderboard" class="btn btn-outline">See toppers</a>
      </div>
    </section>

    <section class="grid grid-cards">
      <a routerLink="/competitions" class="card card-hover link-card">
        <h3>Competitions &amp; Tournaments</h3>
        <p>Academic olympiads and sports tournaments — schedules and registration.</p>
      </a>
      <a routerLink="/leaderboard" class="card card-hover link-card">
        <h3>Toppers &amp; Contributors</h3>
        <p>Rankings by school, city and category — plus who's supporting the platform.</p>
      </a>
      <a routerLink="/news" class="card card-hover link-card">
        <h3>Latest news</h3>
        <p>Results announcements, deadlines and upcoming events.</p>
      </a>
      <a routerLink="/contribute" class="card card-hover link-card">
        <h3>Contribute</h3>
        <p>Support via UPI — Google Pay, PhonePe, Paytm and more.</p>
      </a>
    </section>
  `,
  styles: `
    .hero {
      text-align: center;
      padding: var(--space-8) 0 var(--space-8);
      max-width: 720px;
      margin: 0 auto var(--space-8);
    }
    .hero .badge { margin-bottom: var(--space-4); }
    .hero-subtitle { font-size: 1.05rem; }
    .hero .row { justify-content: center; }
    .link-card { color: inherit; display: block; }
    .link-card:hover { text-decoration: none; }
    .link-card h3 { color: var(--brand-600); }
    .link-card p { margin-bottom: 0; }
  `,
})
export class HomeComponent {}
