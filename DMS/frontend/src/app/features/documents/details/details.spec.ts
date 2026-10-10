import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { Details } from './details';

describe('Details', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [Details],
    providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'document-1' }) } } },
    ],
  }));

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('loads and displays the selected document', async () => {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();
    
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1').flush({
      id: 'document-1', fileName: 'HelloWorld.pdf', description: 'Example document',
      contentType: 'application/pdf', fileSize: 123, uploadedAt: '2026-10-10T10:00:00Z',
      status: 'Uploaded', collectionIds: [], tagIds: [],
    });

    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/tags').flush([]);
    await fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('/api/collections').flush([]);
    await fixture.whenStable();
    
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('HelloWorld.pdf');
    expect(text).toContain('Example document');
    expect(text).toContain('123 B');
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/documents');
    expect(fixture.nativeElement.querySelector('a[download]').getAttribute('href'))
      .toBe('/api/documents/document-1/download');
  });

  it('shows an error for a missing document', async () => {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();
    
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('/api/collections').flush([]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('Could not load document details');
  });

  async function loadedDetails() {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();
    
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1')
      .flush({ id: 'document-1', fileName: 'example.pdf', description: 'Original', tagIds: [] });
    
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/tags').flush([]);
    await fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('/api/collections').flush([]);
    await fixture.whenStable();

    return fixture;
  }

  it('saves and clears the description through PATCH', async () => {
    const fixture = await loadedDetails();
    fixture.componentInstance.saveDescription(new Event('submit'), '');
    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ description: '' });
    request.flush({ id: 'document-1', fileName: 'example.pdf', description: null });
    await fixture.whenStable();
    expect(fixture.componentInstance.document()?.description).toBeNull();
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('Description saved');
  });

  it('keeps the document visible when saving fails', async () => {
    const fixture = await loadedDetails();
    fixture.componentInstance.saveDescription(new Event('submit'), 'Changed');
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    expect(fixture.componentInstance.document()?.description).toBe('Original');
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Could not save');
  });

  it('deletes only after confirmation and returns to the list', async () => {
    const fixture = await loadedDetails();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance.deleteDocument();
    TestBed.inject(HttpTestingController).expectNone('/api/documents/document-1');
    fixture.componentInstance.confirmingDelete.set(true);
    fixture.componentInstance.deleteDocument();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    expect(navigate).toHaveBeenCalledWith(['/documents']);
  });

  it('reports failed deletion and permits a retry', async () => {
    const fixture = await loadedDetails();
    fixture.componentInstance.confirmingDelete.set(true);
    fixture.componentInstance.deleteDocument();
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    expect(fixture.componentInstance.busy()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Could not delete');
  });
});