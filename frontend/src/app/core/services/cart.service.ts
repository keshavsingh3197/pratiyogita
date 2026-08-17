import { Injectable, computed, effect, signal } from '@angular/core';
import { ContributionItem } from '../models/domain.models';

export interface CartLine {
  itemId: string;
  name: string;
  unitAmount: number;
  quantity: number;
}

const STORAGE_KEY = 'pratiyogita.cart';

/** The Contribute page's "add to cart" state — a suggested-amount shopping cart, not a real
 *  checkout: the total is what a single UPI payment is made for, there's no per-item settlement.
 *  Persisted to sessionStorage only (cleared when the tab closes / after a successful contribution). */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly lines = signal<CartLine[]>(this.readStored());

  readonly items = this.lines.asReadonly();
  readonly total = computed(() => this.lines().reduce((sum, l) => sum + l.unitAmount * l.quantity, 0));
  readonly isEmpty = computed(() => this.lines().length === 0);

  constructor() {
    effect(() => sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.lines())));
  }

  add(item: ContributionItem): void {
    const existing = this.lines().find((l) => l.itemId === item.id);
    if (existing) {
      this.lines.update((list) =>
        list.map((l) => (l.itemId === item.id ? { ...l, quantity: l.quantity + 1 } : l))
      );
      return;
    }
    this.lines.update((list) => [
      ...list,
      { itemId: item.id, name: item.name, unitAmount: item.amount, quantity: 1 },
    ]);
  }

  remove(itemId: string): void {
    this.lines.update((list) => list.filter((l) => l.itemId !== itemId));
  }

  clear(): void {
    this.lines.set([]);
  }

  /** e.g. ["Sponsor a trophy x2"] — stored on the Contribution record for reference. */
  summaryLines(): string[] {
    return this.lines().map((l) => `${l.name} x${l.quantity}`);
  }

  private readStored(): CartLine[] {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as CartLine[]) : [];
    } catch {
      return [];
    }
  }
}
