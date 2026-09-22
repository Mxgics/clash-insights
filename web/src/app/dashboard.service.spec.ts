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
    http.expectOne('/api/session').flush({ authenticated: false, name: null, loginAvailable: true });
    TestBed.tick();
    await Promise.resolve();
    const request = http.expectOne('/api/public/dashboard');
    expect(request.request.method).toBe('GET');
    request.flush({});
    http.verify();
  });

  it('switches to the private API only after an authenticated server session', async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.inject(DashboardService);
    TestBed.tick();
    await Promise.resolve();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/session').flush({ authenticated: true, name: 'Owner', loginAvailable: true });
    TestBed.tick();
    await Promise.resolve();
    const request = http.expectOne('/api/private/dashboard');
    expect(request.request.method).toBe('GET');
    request.flush({});
    http.verify();
  });
});
