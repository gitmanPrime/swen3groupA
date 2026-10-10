import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
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
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('HelloWorld.pdf');
    expect(text).toContain('Example document');
    expect(text).toContain('123 bytes');
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/documents');
  });

  it('shows an error for a missing document', async () => {
    const fixture = TestBed.createComponent(Details);
    await fixture.whenStable();
    TestBed.inject(HttpTestingController).expectOne('/api/documents/document-1')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('Could not load document details');
  });
});
