import { Service, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
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
  }[];
  joined: string[];
  left: string[];
  rosterAvailable: boolean;
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
  readonly revision = signal(0);
  refresh() {
    this.revision.update((n) => n + 1);
    this.data.reload();
  }
  readonly data = httpResource<Dashboard>(() => '/api/dashboard');
}
