import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FloatingAiBot } from './floating-ai-bot';

describe('FloatingAiBot', () => {
  let component: FloatingAiBot;
  let fixture: ComponentFixture<FloatingAiBot>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloatingAiBot],
    }).compileComponents();

    fixture = TestBed.createComponent(FloatingAiBot);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
