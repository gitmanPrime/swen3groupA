import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Upload } from './upload';

describe('Upload', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [Upload],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup(file?: File) {
    const fixture = TestBed.createComponent(Upload);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: file ? [file] : [] });
    return { fixture, input };
  }

  it('rejects missing, non-PDF and empty files without sending requests', () => {
    for (const file of [undefined, new File(['text'], 'note.txt'), new File([], 'empty.pdf')]) {
      const { fixture, input } = setup(file);
      fixture.componentInstance.upload(new Event('submit'), input, '');
      expect(fixture.componentInstance.error()).toContain('non-empty PDF');
      TestBed.inject(HttpTestingController).expectNone('/api/documents');
    }
  });

  it('sends multipart data and reports success', async () => {
    const file = new File(['%PDF-1.4'], 'example.pdf', { type: 'application/pdf' });
    const { fixture, input } = setup(file);
    fixture.componentInstance.upload(new Event('submit'), input, ' Example ');
    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.get('file').name).toBe('example.pdf');
    expect(request.request.body.get('description')).toBe('Example');
    expect(fixture.componentInstance.uploading()).toBe(true);
    request.flush({ fileName: 'example.pdf' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent)
      .toContain('Uploaded example.pdf successfully');
    expect(fixture.componentInstance.uploading()).toBe(false);
  });

  it('allows an optional description and reports failure', async () => {
    const { fixture, input } = setup(new File(['%PDF-1.4'], 'example.pdf'));
    fixture.componentInstance.upload(new Event('submit'), input, '');
    const request = TestBed.inject(HttpTestingController).expectOne('/api/documents');
    expect(request.request.body.has('description')).toBe(false);
    request.flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('Upload failed');
    expect(fixture.componentInstance.uploading()).toBe(false);
  });
});
