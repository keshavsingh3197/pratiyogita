import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { LoadingService } from './loading.service';
import { SILENT_HTTP, loadingInterceptor } from '../interceptors/loading.interceptor';

describe('LoadingService & loadingInterceptor', () => {
  let service: LoadingService;
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(LoadingService);
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  it('tracks in-flight HTTP calls and clears on completion', () => {
    expect(service.active()).toBe(false);
    http.get('/api/competitions').subscribe();
    expect(service.httpCount()).toBe(1);
    expect(service.active()).toBe(true);

    httpMock.expectOne('/api/competitions').flush([]);
    expect(service.httpCount()).toBe(0);
    expect(service.active()).toBe(false);
  });

  it('ignores SILENT_HTTP requests', () => {
    const context = new HttpContext().set(SILENT_HTTP, true);
    http.get('/api/ping', { context }).subscribe();
    expect(service.active()).toBe(false);
    httpMock.expectOne('/api/ping').flush({});
  });

  it('sets slow=true when requests exceed cold-start threshold and resets when done', () => {
    http.get('/api/slow').subscribe();
    expect(service.slow()).toBe(false);

    vi.advanceTimersByTime(2250);
    expect(service.slow()).toBe(true);

    httpMock.expectOne('/api/slow').flush({});
    expect(service.slow()).toBe(false);
    expect(service.active()).toBe(false);
  });
});
