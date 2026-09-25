import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Overview } from './overview.component';
describe('Overview failure', () => {
  it('shows an actionable error without fixture fallback', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const f = TestBed.createComponent(Overview);
    f.detectChanges();
    TestBed.tick();
    await Promise.resolve();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/session').flush({ authenticated: false, name: null, loginAvailable: false });
    TestBed.tick();
    await Promise.resolve();
    http
      .expectOne('/api/public/dashboard')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    await f.whenStable();
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('Unable to load your dashboard');
    expect(f.nativeElement.textContent).not.toContain('DEMO DATA');
  });

  it('sorts a roster copy without mutating the server response', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(Overview);
    const clan = {
      tag: '#P0Y28', name: 'Clan', level: 1, points: 1, observedAt: new Date().toISOString(),
      members: [
        { tag: '#P0Y29', name: 'Low', role: 'member', trophies: 1, donations: 1, rank: 2 },
        { tag: '#P0Y2L', name: 'High', role: 'leader', trophies: 2, donations: 2, rank: 1 },
      ],
      joined: [], left: [], rosterAvailable: true,
    };
    const original = clan.members.map((member) => member.tag);
    expect(fixture.componentInstance.members(clan)[0].name).toBe('High');
    expect(clan.members.map((member) => member.tag)).toEqual(original);
  });
});
