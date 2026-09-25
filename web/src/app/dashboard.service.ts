import { inject, Service, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { AccessService } from './access.service';
export interface Player {
  tag: string;
  name: string;
  townHall: number | null;
  trophies: number | null;
  donations: number | null;
  received: number | null;
  trophyChange: number | null;
  observedAt: string;
  history: { at: string; trophies: number | null; donations: number | null }[];
  league?: string | null;
  bestTrophies?: number | null;
  builderBaseTrophies?: number | null;
  warStars?: number | null;
}
export interface Clan {
  tag: string;
  name: string;
  level: number | null;
  points: number | null;
  observedAt: string;
  members: {
    tag: string;
    name: string;
    role: string;
    trophies: number | null;
    donations: number | null;
    townHall?: number | null;
    received?: number | null;
    league?: string | null;
    rank?: number | null;
  }[];
  joined: string[];
  left: string[];
  rosterAvailable: boolean;
  rosterComparisonAvailable?: boolean;
  warLogPublic?: boolean | null;
  warWins?: number | null;
  warWinStreak?: number | null;
  warLeague?: string | null;
}
export interface Dashboard {
  mode: string;
  collectorStatus: string;
  intervalMinutes: number;
  retentionDays: number;
  players: Player[];
  clans: Clan[];
  wars: {
    tag: string;
    state: string;
    opponent: string;
    stars: number | null;
    opponentStars: number | null;
    attacks: number | null;
    teamSize: number | null;
    observedAt: string;
  }[];
  attempts: { id: number; at: string; kind: string; tag: string; status: string }[];
}
@Service()
export class DashboardService {
  readonly access = inject(AccessService);
  readonly revision = signal(0);
  refresh() {
    this.revision.update((n) => n + 1);
    this.data.reload();
  }
  readonly data = httpResource<Dashboard>(() => {
    const scope = this.access.apiScope();
    return scope ? `/api/${scope}/dashboard` : undefined;
  });
  historyUrl(tag: string, days: number) {
    const scope = this.access.apiScope();
    return scope ? `/api/${scope}/players/${encodeURIComponent(tag)}/history?days=${days}&revision=${this.revision()}` : undefined;
  }
}
