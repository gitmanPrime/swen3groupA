import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Collections } from './collections';

describe('Collections', () => {
  let component: Collections;
  let fixture: ComponentFixture<Collections>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Collections],
    }).compileComponents();

    fixture = TestBed.createComponent(Collections);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
