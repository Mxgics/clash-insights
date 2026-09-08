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
    await Promise.resolve();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/dashboard')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    await f.whenStable();
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('Unable to load your dashboard');
    expect(f.nativeElement.textContent).not.toContain('DEMO DATA');
  });
});
