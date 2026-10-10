import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentTags } from './document-tags';

describe('DocumentTags', () => {
  let component: DocumentTags;
  let fixture: ComponentFixture<DocumentTags>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentTags],
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentTags);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
