import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CompetitionsService } from '../../core/services/competitions.service';
import { Competition } from '../../core/models/domain.models';

@Component({
  selector: 'app-competitions',
  imports: [DatePipe],
  template: `
    <h1>Competitions &amp; Tournaments</h1>
    @for (c of competitions(); track c.id) {
      <article>
        <h3>{{ c.name }} <small>({{ c.type }} · {{ c.category }})</small></h3>
        <p>{{ c.level }} · Starts {{ c.startsAt | date: 'medium' }} · {{ c.status }}</p>
      </article>
    } @empty {
      <p>No competitions published yet.</p>
    }
  `,
})
export class CompetitionsComponent {
  private competitionsApi = inject(CompetitionsService);
  protected readonly competitions = signal<Competition[]>([]);

  constructor() {
    this.competitionsApi.getAll().subscribe((list) => this.competitions.set(list));
  }
}
