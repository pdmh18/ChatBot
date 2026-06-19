import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AiAlerts } from './ai-alerts';

describe('AiAlerts', () => {
  let component: AiAlerts;
  let fixture: ComponentFixture<AiAlerts>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AiAlerts],
    }).compileComponents();

    fixture = TestBed.createComponent(AiAlerts);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
