import { TestBed } from '@angular/core/testing';
import { MyProfile } from './my-profile';
describe('My profile', () => {
  it('does not accept an invalid player tag', () => {
    const f = TestBed.createComponent(MyProfile);
    f.componentInstance.draft.set({
      displayName: 'Test',
      primaryTag: 'invalid!',
      historyDays: '14',
    });
    f.detectChanges();
    expect(f.componentInstance.fields().invalid()).toBe(true);
  });
});
