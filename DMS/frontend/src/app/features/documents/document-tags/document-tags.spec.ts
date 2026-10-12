import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { DocumentTags } from './document-tags';

describe('DocumentTags', () => {
  let fixture: ComponentFixture<DocumentTags>;
  let http: HttpTestingController;

  const allTags = [
    { id: 'tag-1', name: 'invoice' },
    { id: 'tag-2', name: 'contract' },
  ];

  /** Creates the component with the given set of already-assigned tag ids. */
  async function createWith(tagIds: string[]) {
    fixture = TestBed.createComponent(DocumentTags);
    fixture.componentRef.setInput('documentId', 'document-1');
    fixture.componentRef.setInput('tagIds', tagIds);
    await fixture.whenStable();
    // DocumentTags loads the full tag list as soon as it's created.
    http.expectOne('/api/tags').flush(allTags);
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentTags],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('displays the tags already assigned to the document', async () => {
    await createWith(['tag-1']);
    expect(fixture.nativeElement.querySelector('[aria-label="Remove tag invoice"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[aria-label="Remove tag contract"]')).toBeFalsy();
  });

  it('shows a message when no tags are assigned', async () => {
    await createWith([]);
    expect(fixture.nativeElement.textContent).toContain('No tags have been assigned');
  });

  it('creates a new tag and adds it to the selectable list', async () => {
    await createWith([]);
    fixture.componentInstance.newTagName.set('urgent');
    fixture.componentInstance.createTag();

    const request = http.expectOne('/api/tags');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'urgent' });
    request.flush({ id: 'tag-3', name: 'urgent' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Tag created.');
    const options: string[] = Array.from(fixture.nativeElement.querySelectorAll('option'))
      .map((option) => (option as HTMLOptionElement).textContent?.trim() ?? '');
    expect(options).toContain('urgent');
  });

  it('assigns the selected tag to the document', async () => {
    await createWith([]);
    fixture.componentInstance.selectedTagId.set('tag-2');
    fixture.componentInstance.assignTag();

    const request = http.expectOne('/api/documents/document-1/tags/tag-2');
    expect(request.request.method).toBe('POST');
    request.flush({});
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('contract');
    expect(fixture.nativeElement.textContent).toContain('Tag assigned to the document.');
  });

  it('does not assign a tag that is already assigned', async () => {
    await createWith(['tag-1']);
    fixture.componentInstance.selectedTagId.set('tag-1');
    fixture.componentInstance.assignTag();

    // no HTTP call should be made for a tag that's already assigned
    http.expectNone('/api/documents/document-1/tags/tag-1');
    expect(fixture.componentInstance.errorMessage()).toContain('already assigned');
  });

  it('removes an assigned tag without reloading the page', async () => {
    await createWith(['tag-1']);
    fixture.componentInstance.removeTag(allTags[0]);

    const request = http.expectOne('/api/documents/document-1/tags/tag-1');
    expect(request.request.method).toBe('DELETE');
    request.flush({});
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[aria-label="Remove tag invoice"]')).toBeFalsy();
    expect(fixture.nativeElement.textContent).toContain('Tag removed.');
  });

  it('shows an error when assigning a tag fails', async () => {
    await createWith([]);
    fixture.componentInstance.selectedTagId.set('tag-2');
    fixture.componentInstance.assignTag();

    http.expectOne('/api/documents/document-1/tags/tag-2')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Could not assign the tag.');
  });

  it('shows an error when removing a tag fails', async () => {
    await createWith(['tag-1']);
    fixture.componentInstance.removeTag(allTags[0]);

    http.expectOne('/api/documents/document-1/tags/tag-1')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    // the tag stays assigned since the removal failed
    expect(fixture.nativeElement.textContent).toContain('invoice');
    expect(fixture.nativeElement.textContent).toContain('Could not remove the tag.');
  });
});
