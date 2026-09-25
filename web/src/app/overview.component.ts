import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Clan, DashboardService, Player } from './dashboard.service';
import { HistoryChart } from './history-chart';
import { MyProfile } from './my-profile';
import { Preferences } from './preferences';
import { WorkspaceNav } from './workspace-nav';
@Component({
  selector: 'app-overview',
  imports: [DatePipe, DecimalPipe, HistoryChart, WorkspaceNav, MyProfile],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss',
})
export class Overview {
  readonly preferences = inject(Preferences);
  readonly service = inject(DashboardService);
  readonly access = this.service.access;
  readonly section = signal('Overview');
  readonly selectedTag = signal('');
  readonly memberSearch = signal('');
  readonly memberSort = signal('rank');
  readonly data = computed(() =>
    this.service.data.hasValue() ? this.service.data.value() : undefined,
  );
  readonly player = computed(
    () =>
      this.data()?.players.find(
        (p) => p.tag === (this.selectedTag() || this.preferences.settings().primaryTag),
      ) ?? this.data()?.players[0],
  );
  readonly totalDonations = computed(() => {
    const clans = this.data()?.clans ?? [];
    const members = clans.flatMap((c) => c.members);
    return !clans.length || !members.length || clans.some((c) => !c.rosterAvailable) || members.some((x) => x.donations === null)
      ? null
      : members.reduce((n, m) => n + (m.donations ?? 0), 0);
  });
  readonly memberCount = computed(() => {
    const clans = this.data()?.clans ?? [];
    return !clans.length || clans.some((c) => !c.rosterAvailable) ? null : clans.reduce((n, c) => n + c.members.length, 0);
  });
  readonly latestObservation = computed(() => {
    const dates = [...(this.data()?.players ?? []), ...(this.data()?.clans ?? [])].map((x) => x.observedAt).sort();
    return dates.at(-1);
  });
  readonly privateClans = computed(() => this.data()?.clans.filter((c) => c.warLogPublic === false) ?? []);
  searchChanged(event: Event) { this.memberSearch.set((event.target as HTMLInputElement).value); }
  sortChanged(event: Event) { this.memberSort.set((event.target as HTMLSelectElement).value); }
  members(clan: Clan) {
    const preview = this.section() === 'Overview';
    const query = preview ? '' : this.memberSearch().trim().toLowerCase();
    const sort = preview ? 'donations' : this.memberSort();
    const members = [...clan.members].filter((m) => !query || (m.name + ' ' + m.tag).toLowerCase().includes(query));
    members.sort((a, b) => {
      if (sort === 'name') return a.name.localeCompare(b.name);
      if (sort === 'rank') return (a.rank ?? Infinity) - (b.rank ?? Infinity) || a.name.localeCompare(b.name);
      const key = sort === 'trophies' ? 'trophies' : sort === 'townHall' ? 'townHall' : 'donations';
      return (b[key] ?? -1) - (a[key] ?? -1) || a.name.localeCompare(b.name);
    });
    return preview ? members.slice(0, 5) : members;
  }
  role(value: string) {
    return ({leader: 'Leader', coLeader: 'Co-leader', admin: 'Elder', member: 'Member'} as Record<string, string>)[value] ?? 'Unavailable';
  }
  watched(tag: string) { return this.data()?.players.some((p) => p.tag === tag) ?? false; }
  warFailure(tag: string) { return this.data()?.attempts.find((a) => a.kind === 'war' && a.tag === tag && a.status !== 'Collected'); }
  selectPlayer(player: Player) {
    this.selectedTag.set(player.tag);
    this.section.set('Players');
  }
  stale(at: string) {
    return Date.now() - new Date(at).getTime() > (this.data()?.intervalMinutes ?? 60) * 120000;
  }
}
