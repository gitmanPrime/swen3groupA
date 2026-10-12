import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const auth = {
    getAccessToken: vi.fn<() => Promise<string | null>>()
  };

  beforeEach(() => {
    auth.getAccessToken.mockReset();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth }
      ]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  it('attaches the access token to API requests', async () => {
    auth.getAccessToken.mockResolvedValue('test-token');

    const response = firstValueFrom(
      TestBed.inject(HttpClient).get('/api/documents')
    );

    await Promise.resolve();

    const request = TestBed.inject(HttpTestingController)
      .expectOne('/api/documents');

    expect(request.request.headers.get('Authorization'))
      .toBe('Bearer test-token');

    request.flush([]);
    await response;
  });

  it('does not add a header when logged out', async () => {
    auth.getAccessToken.mockResolvedValue(null);

    const response = firstValueFrom(
      TestBed.inject(HttpClient).get('/api/documents')
    );

    await Promise.resolve();

    const request = TestBed.inject(HttpTestingController)
      .expectOne('/api/documents');

    expect(request.request.headers.has('Authorization')).toBe(false);

    request.flush([]);
    await response;
  });

  it('does not retrieve or attach tokens for external URLs', async () => {
    const url = 'https://example.com/data';

    const response = firstValueFrom(
      TestBed.inject(HttpClient).get(url)
    );

    const request = TestBed.inject(HttpTestingController).expectOne(url);

    expect(auth.getAccessToken).not.toHaveBeenCalled();
    expect(request.request.headers.has('Authorization')).toBe(false);

    request.flush({});
    await response;
  });

  it('does not send the API request when token refresh fails', async () => {
    auth.getAccessToken.mockRejectedValue(new Error('Refresh failed'));

    const response = firstValueFrom(
      TestBed.inject(HttpClient).get('/api/documents')
    );

    await expect(response).rejects.toThrow('Refresh failed');

    TestBed.inject(HttpTestingController).expectNone('/api/documents');
  });
});
