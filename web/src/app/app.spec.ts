import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Overview } from './overview.component';
describe('Dashboard', () => {
  it('labels synthetic data and does not imply a live connection', async () => {
    TestBed.configureTestingModule({
      imports: [Overview],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(Overview);
    fixture.detectChanges();
    TestBed.tick();
    await Promise.resolve();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/session').flush({ authenticated: false, name: null, loginAvailable: false });
    TestBed.tick();
    await Promise.resolve();
    http.expectOne('/api/public/dashboard').flush({
      mode: 'Demo',
      collectorStatus: 'Demo mode',
      intervalMinutes: 60,
      retentionDays: 90,
      players: [],
      clans: [],
      wars: [],
      attempts: [],
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('DEMO DATA');
    expect(fixture.nativeElement.textContent).toContain('No player observations yet');
  });
});
