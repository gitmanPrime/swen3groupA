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

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    TestBed.inject(HttpTestingController).verify();
  });

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
    expect(
      fixture.nativeElement.querySelector('#download-document').textContent
    ).toContain('Download file');
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

  it('downloads the file through the API with its original filename', async () => {
    const fixture = await loadedDetails();
    const file = new Blob(['%PDF-1.4'], { type: 'application/pdf' });
    const createObjectURL = vi.fn(() => 'blob:test-download');
    const revokeObjectURL = vi.fn();

    // Keep the normal URL functionality and mock only the object URL methods.
    vi.stubGlobal('URL', class extends URL {
      static override createObjectURL = createObjectURL;
      static override revokeObjectURL = revokeObjectURL;
    });

    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(function (this: HTMLAnchorElement) {
        expect(this.download).toBe('example.pdf');
        expect(this.href).toBe('blob:test-download');
      });

    vi.useFakeTimers();

    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('#download-document');

    button.click();
    fixture.detectChanges();

    expect(button.disabled).toBe(true);

    const request = TestBed.inject(HttpTestingController)
      .expectOne('/api/documents/document-1/download');

    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');

    request.flush(file);
    fixture.detectChanges();

    expect(createObjectURL).toHaveBeenCalledWith(file);
    expect(click).toHaveBeenCalledOnce();
    expect(button.disabled).toBe(false);

    vi.advanceTimersByTime(1000);

    expect(revokeObjectURL).toHaveBeenCalledWith('blob:test-download');
  });

  it('shows a download error and enables retry', async () => {
    const fixture = await loadedDetails();
    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('#download-document');

    button.click();

    const request = TestBed.inject(HttpTestingController)
      .expectOne('/api/documents/document-1/download');

    request.flush(null, {
      status: 500,
      statusText: 'Internal Server Error'
    });

    fixture.detectChanges();

    expect(button.disabled).toBe(false);
    expect(fixture.nativeElement.textContent)
      .toContain('Could not download the document. Please try again.');

    button.click();

    TestBed.inject(HttpTestingController)
      .expectOne('/api/documents/document-1/download')
      .flush(null, { status: 500, statusText: 'Internal Server Error' });
  });

  it('adds the document to a selected collection', async () => {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1').flush({
      id: 'document-1', fileName: 'example.pdf', description: 'Original',
      collectionIds: [], tagIds: [],
    });
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/tags').flush([]);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/collections').flush([
      { id: 'col-1', name: 'Marketing', createdAt: '2026-10-01T00:00:00Z', documentCount: 0 },
    ]);
    await fixture.whenStable();

    fixture.componentInstance.selectedCollectionId.set('col-1');
    fixture.componentInstance.addToCollection();

    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ collectionIds: ['col-1'] });
    request.flush({
      id: 'document-1', fileName: 'example.pdf', description: 'Original',
      collectionIds: ['col-1'], tagIds: [],
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Document added to collection successfully.');
    expect(fixture.nativeElement.textContent).toContain('Marketing');
    // the dropdown resets once the collection has been added
    expect(fixture.componentInstance.selectedCollectionId()).toBe('');
  });

  it('removes the document from a collection', async () => {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1').flush({
      id: 'document-1', fileName: 'example.pdf', description: 'Original',
      collectionIds: ['col-1'], tagIds: [],
    });
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/tags').flush([]);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/collections').flush([
      { id: 'col-1', name: 'Marketing', createdAt: '2026-10-01T00:00:00Z', documentCount: 1 },
    ]);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Marketing');

    fixture.componentInstance.removeFromCollection('col-1');

    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ collectionIds: [] });
    request.flush({
      id: 'document-1', fileName: 'example.pdf', description: 'Original',
      collectionIds: [], tagIds: [],
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Document removed from collection.');
    expect(fixture.nativeElement.textContent).toContain('Not assigned to any collection yet.');
  });
});
