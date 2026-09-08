import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { DashboardService, Player } from './dashboard.service';
import { HistoryChart } from './history-chart';
import { WorkspaceNav } from './workspace-nav';
@Component({
  selector: 'app-overview',
  imports: [DatePipe, DecimalPipe, HistoryChart, WorkspaceNav],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class Overview {
  readonly service = inject(DashboardService);
  readonly section = signal('Overview');
  readonly selectedTag = signal('');
  readonly data = computed(() =>
    this.service.data.hasValue() ? this.service.data.value() : undefined,
  );
  readonly player = computed(
    () => this.data()?.players.find((p) => p.tag === this.selectedTag()) ?? this.data()?.players[0],
  );
  readonly totalDonations = computed(() => {
    const p = this.data()?.players ?? [];
    return p.some((x) => x.donations === null)
      ? null
      : p.reduce((n, p) => n + (p.donations ?? 0), 0);
  });
  selectPlayer(player: Player) {
    this.selectedTag.set(player.tag);
    this.section.set('Players');
  }
  stale(at: string) {
    return Date.now() - new Date(at).getTime() > (this.data()?.intervalMinutes ?? 60) * 120000;
  }
}
