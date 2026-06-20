import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Gantt } from './gantt';

describe('Gantt', () => {
  let component: Gantt;
  let fixture: ComponentFixture<Gantt>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Gantt],
    }).compileComponents();

    fixture = TestBed.createComponent(Gantt);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
