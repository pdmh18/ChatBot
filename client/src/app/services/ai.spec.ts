import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { AiService } from './ai';

describe('AiService', () => {
  let service: AiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()],
    });
    service = TestBed.inject(AiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
