import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'competitions',
    loadComponent: () =>
      import('./features/competitions/competitions.component').then((m) => m.CompetitionsComponent),
  },
  {
    path: 'leaderboard',
    loadComponent: () =>
      import('./features/leaderboard/leaderboard.component').then((m) => m.LeaderboardComponent),
  },
  {
    path: 'news',
    loadComponent: () => import('./features/news/news.component').then((m) => m.NewsComponent),
  },
  {
    path: 'contribute',
    loadComponent: () =>
      import('./features/contribute/contribute.component').then((m) => m.ContributeComponent),
  },
  {
    path: 'register-school',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/schools/register-school.component').then((m) => m.RegisterSchoolComponent),
  },
  {
    path: 'settings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/settings/settings.component').then((m) => m.SettingsComponent),
  },
  { path: '**', redirectTo: '' },
];
