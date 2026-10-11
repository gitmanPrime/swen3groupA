import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CollectionService } from './collection.service';

describe('CollectionService', () => {
  let service: CollectionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CollectionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists all collections', () => {
    let result: unknown;
    service.list().subscribe((collections) => (result = collections));

    const request = http.expectOne('/api/collections');
    expect(request.request.method).toBe('GET');
    request.flush([{ id: 'col-1', name: 'Invoices', createdAt: '2026-10-01T00:00:00Z', documentCount: 2 }]);

    expect(result).toEqual([
      { id: 'col-1', name: 'Invoices', createdAt: '2026-10-01T00:00:00Z', documentCount: 2 },
    ]);
  });

  it('gets a collection by id', () => {
    service.getById('col-1').subscribe();

    const request = http.expectOne('/api/collections/col-1');
    expect(request.request.method).toBe('GET');
    request.flush({ id: 'col-1', name: 'Invoices', createdAt: '2026-10-01T00:00:00Z', documentCount: 0 });
  });

  it('creates a collection', () => {
    service.create({ name: 'Contracts', description: 'Signed contracts' }).subscribe();

    const request = http.expectOne('/api/collections');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Contracts', description: 'Signed contracts' });
    request.flush({ id: 'col-2', name: 'Contracts', createdAt: '2026-10-11T00:00:00Z', documentCount: 0 });
  });

  it('renames a collection', () => {
    service.rename('col-1', { name: 'Renamed' }).subscribe();

    const request = http.expectOne('/api/collections/col-1');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ name: 'Renamed' });
    request.flush({ id: 'col-1', name: 'Renamed', createdAt: '2026-10-01T00:00:00Z', documentCount: 0 });
  });

  it('deletes a collection', () => {
    service.delete('col-1').subscribe();

    const request = http.expectOne('/api/collections/col-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
