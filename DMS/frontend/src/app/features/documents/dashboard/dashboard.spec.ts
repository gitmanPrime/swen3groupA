import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('should create', () => {
    http.expectOne('/api/documents').flush([]);
    expect(component).toBeTruthy();
  });

  it('displays documents returned by the API', async () => {
    const request = http.expectOne('/api/documents');
    expect(request.request.method).toBe('GET');
    request.flush([{ id: 'document-1', fileName: 'HelloWorld.pdf' }]);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('HelloWorld.pdf');
    expect(fixture.nativeElement.querySelector('a[href*="/documents/"]').getAttribute('href'))
      .toBe('/documents/document-1');
    expect(fixture.nativeElement.textContent).not.toContain('Loading documents');
  });

  it('displays an error when the API request fails', async () => {
    http.expectOne('/api/documents').flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
      .toContain('Could not load documents');
  });

  it('displays an empty state when there are no documents', async () => {
    http.expectOne('/api/documents').flush([]);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('No documents yet.');
  });
});
