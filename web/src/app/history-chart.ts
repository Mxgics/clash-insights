import { Component, computed, linkedSignal, input, signal, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Preferences } from './preferences';
import { Player, DashboardService } from './dashboard.service';
export interface HistoryPoint {
  at: string;
  trophies: number | null;
  donations: number | null;
}
export function chartPath(history: HistoryPoint[], interval: number): string {
  const values = history.filter((h) => h.trophies !== null);
  if (!values.length) return '';
  const low = Math.min(...values.map((h) => h.trophies!)) - 20,
    high = Math.max(...values.map((h) => h.trophies!)) + 20;
  const start = Date.parse(history[0].at),
    end = Date.parse(history[history.length - 1].at);
  let previous: HistoryPoint | undefined;
  return history
    .map((h) => {
      if (h.trophies === null) {
        previous = undefined;
        return '';
      }
      const gap = !previous || Date.parse(h.at) - Date.parse(previous.at) > interval * 120000;
      previous = h;
      return (
        (gap ? 'M' : 'L') +
        (20 + ((Date.parse(h.at) - start) * 660) / Math.max(end - start, 1)) +
        ',' +
        (160 - ((h.trophies - low) / (high - low)) * 130)
      );
    })
    .join(' ');
}
@Component({
  selector: 'app-history-chart',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './history-chart.html',
  styleUrl: './history-chart.scss',
})
export class HistoryChart {
  readonly service = inject(DashboardService);
  readonly player = input<Player>();
  readonly intervalMinutes = input(60);
  readonly preferences = inject(Preferences);
  readonly range = linkedSignal(() => this.preferences.settings().historyDays as number);
  readonly resource = httpResource<HistoryPoint[]>(() =>
    this.player()
      ? '/api/players/' +
        encodeURIComponent(this.player()!.tag) +
        '/history?days=' +
        this.range() +
        '&revision=' +
        this.service.revision()
      : undefined,
  );
  readonly history = computed(() => (this.resource.hasValue() ? this.resource.value() : []));
  readonly chart = computed(() => chartPath(this.history(), this.intervalMinutes()));
  readonly change = computed(() => {
    const h = this.history();
    return h.length > 1 && h[0].trophies !== null && h[h.length - 1].trophies !== null
      ? h[h.length - 1].trophies! - h[0].trophies!
      : null;
  });
  readonly donationChange = computed(() => {
    const h = this.history();
    if (h.length < 2 || h.some((p) => p.donations === null)) return null;
    let total = 0;
    for (let i = 1; i < h.length; i++) {
      if (h[i].donations! < h[i - 1].donations!) return null;
      total += h[i].donations! - h[i - 1].donations!;
    }
    return total;
  });
}
