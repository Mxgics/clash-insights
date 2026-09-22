import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';

export interface Session {
  authenticated: boolean;
  name: string | null;
  loginAvailable: boolean;
}

@Service()
export class AccessService {
  private readonly http = inject(HttpClient);
  readonly session = signal<Session | undefined>(undefined);
  readonly authenticated = computed(() => this.session()?.authenticated === true);
  readonly loginAvailable = computed(() => this.session()?.loginAvailable === true);
  readonly displayName = computed(() =>
    this.session()?.name || 'Private workspace',
  );
  readonly apiScope = computed(() => {
    if (!this.session()) return undefined;
    return this.authenticated() ? 'private' : 'public';
  });

  constructor() {
    this.loadSession();
  }

  private loadSession() {
    this.http.get<Session>('/api/session').subscribe({
      next: (session) => this.session.set(session),
      error: () => this.session.set({ authenticated: false, name: null, loginAvailable: false }),
    });
  }

  logout() {
    this.http.post('/auth/logout', null).subscribe({
      next: () => window.location.assign('/'),
      error: () => this.loadSession(),
    });
  }
}
