import { Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, pattern } from '@angular/forms/signals';
import { Preferences, Theme } from './preferences';
@Component({
  selector: 'app-my-profile',
  imports: [FormField],
  templateUrl: './my-profile.html',
  styleUrl: './my-profile.scss',
})
export class MyProfile {
  readonly themes: Theme[] = ['light', 'dark', 'system'];
  readonly preferences = inject(Preferences);
  readonly draft = signal({
    displayName: this.preferences.settings().displayName,
    primaryTag: this.preferences.settings().primaryTag,
    historyDays: String(this.preferences.settings().historyDays),
  });
  readonly fields = form(this.draft, (p) => {
    maxLength(p.displayName, 60);
    pattern(p.primaryTag, /^(?:#?[0289PYLQGRJCUV]{3,15})?$/i, {
      message: 'Enter a valid player tag, or leave this blank.',
    });
  });
  readonly message = signal('');
  save(event: Event) {
    event.preventDefault();
    if (this.fields().invalid()) {
      this.message.set('Check the player tag before saving.');
      return;
    }
    const p = this.draft();
    this.preferences.save({
      ...this.preferences.settings(),
      displayName: p.displayName.trim(),
      primaryTag: p.primaryTag ? '#' + p.primaryTag.replace(/^#/, '').toUpperCase() : '',
      historyDays: Number(p.historyDays) as 7 | 14 | 90,
    });
    this.message.set('Profile preferences saved.');
  }
  reset() {
    this.preferences.reset();
    this.draft.set({ displayName: '', primaryTag: '', historyDays: '14' });
    this.message.set('Local preferences reset.');
  }
}
