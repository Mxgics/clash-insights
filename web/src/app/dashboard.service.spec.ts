import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';
describe('Dashboard reads', () => {
  it('only requests the local read API', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    TestBed.inject(DashboardService);
    TestBed.tick();
    await Promise.resolve();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/dashboard');
    expect(request.request.method).toBe('GET');
    request.flush({});
    http.verify();
  });
});
