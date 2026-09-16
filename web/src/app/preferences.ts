import { Service, signal, computed, effect, inject, DestroyRef } from '@angular/core';
import { DOCUMENT } from '@angular/common';
export type Theme = 'light' | 'dark' | 'system';
export interface ProfileSettings {
  displayName: string;
  primaryTag: string;
  historyDays: 7 | 14 | 90;
  theme: Theme;
}
export const defaults: ProfileSettings = {
  displayName: '',
  primaryTag: '',
  historyDays: 14,
  theme: 'system',
};
export const preferenceKey = 'clash-insights.preferences.v1';
export function parseSettings(raw: string | null): ProfileSettings {
  try {
    const p = JSON.parse(raw ?? '{}');
    if (!p || typeof p !== 'object') return { ...defaults };
    return {
      displayName: typeof p.displayName === 'string' ? p.displayName.slice(0, 60) : '',
      primaryTag:
        typeof p.primaryTag === 'string' && /^#[0289PYLQGRJCUV]{3,15}$/.test(p.primaryTag)
          ? p.primaryTag
          : '',
      historyDays: [7, 14, 90].includes(p.historyDays) ? p.historyDays : 14,
      theme: ['light', 'dark', 'system'].includes(p.theme) ? p.theme : 'system',
    };
  } catch {
    return { ...defaults };
  }
}
@Service()
export class Preferences {
  private readonly document = inject(DOCUMENT);
  private readonly win = this.document.defaultView;
  private readonly media = this.win?.matchMedia?.('(prefers-color-scheme: dark)');
  readonly settings = signal<ProfileSettings>(this.load());
  readonly systemDark = signal(this.media?.matches ?? false);
  readonly dark = computed(
    () =>
      this.settings().theme === 'dark' || (this.settings().theme === 'system' && this.systemDark()),
  );
  readonly storageError = signal(false);
  constructor() {
    const changed = (event: MediaQueryListEvent) => this.systemDark.set(event.matches);
    this.media?.addEventListener('change', changed);
    inject(DestroyRef).onDestroy(() => this.media?.removeEventListener('change', changed));
    effect(() => {
      this.document.documentElement.dataset['theme'] = this.dark() ? 'dark' : 'light';
      this.document.documentElement.style.colorScheme = this.dark() ? 'dark' : 'light';
    });
  }
  private load() {
    try {
      return parseSettings(this.win?.localStorage.getItem(preferenceKey) ?? null);
    } catch {
      return { ...defaults };
    }
  }
  save(settings: ProfileSettings) {
    this.settings.set({ ...settings });
    try {
      this.win?.localStorage.setItem(preferenceKey, JSON.stringify(settings));
      this.storageError.set(false);
    } catch {
      this.storageError.set(true);
    }
  }
  setTheme(theme: Theme) {
    this.save({ ...this.settings(), theme });
  }
  toggleTheme() {
    this.setTheme(this.dark() ? 'light' : 'dark');
  }
  reset() {
    this.save({ ...defaults });
  }
}
