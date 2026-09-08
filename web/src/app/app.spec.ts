import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { App } from './app';
describe('Dashboard', () => {
  it('labels synthetic data and does not imply a live connection', async () => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await Promise.resolve();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/dashboard')
      .flush({
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
