import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AssigneeSuggestion,
  Bottleneck,
  BottleneckResult,
  LateRisk,
  RiskPredictionResult,
  StaffMatchResult,
} from '../models/ai';

@Injectable({
  providedIn: 'root',
})
export class AiService {
  private readonly apiUrl = `${environment.apiUrl}/ai`;
  private readonly suggestAssigneesCache = new Map<number, Observable<StaffMatchResult[]>>();
  private readonly bottleneckCache = new Map<number, Observable<BottleneckResult[]>>();

  constructor(private http: HttpClient) {}

  getLateRisksApi(): Observable<LateRisk[]> {
    return this.http.get<LateRisk[]>(`${this.apiUrl}/late-risk`);
  }

  getBottlenecksApi(): Observable<Bottleneck[]> {
    return this.http.get<Bottleneck[]>(`${this.apiUrl}/bottlenecks`);
  }

  getAssigneeSuggestionsApi(): Observable<AssigneeSuggestion[]> {
    return this.http.get<AssigneeSuggestion[]>(`${this.apiUrl}/assignee-suggestions`);
  }

  suggestAssignees(taskId: number): Observable<StaffMatchResult[]> {
    const cached = this.suggestAssigneesCache.get(taskId);
    if (cached) return cached;

    const request$ = this.http
      .post<StaffMatchResult[]>(`${this.apiUrl}/tasks/${taskId}/suggest-assignees`, {})
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));

    this.suggestAssigneesCache.set(taskId, request$);
    return request$;
  }

  matchStaff(taskId: number, userId: number): Observable<StaffMatchResult> {
    return this.http.post<StaffMatchResult>(
      `${this.apiUrl}/tasks/${taskId}/match-staff/${userId}`,
      {}
    );
  }

  predictRisk(taskId: number): Observable<RiskPredictionResult> {
    return this.http.post<RiskPredictionResult>(
      `${this.apiUrl}/tasks/${taskId}/predict-risk`,
      {}
    );
  }

  analyzeBottlenecks(topN = 10): Observable<BottleneckResult[]> {
    const cached = this.bottleneckCache.get(topN);
    if (cached) return cached;

    const params = new HttpParams().set('topN', topN);
    const request$ = this.http
      .post<BottleneckResult[] | { value?: BottleneckResult[] }>(
        `${this.apiUrl}/bottlenecks/analyze`,
        {},
        { params }
      )
      .pipe(
        map((response) => (Array.isArray(response) ? response : response.value ?? [])),
        shareReplay({ bufferSize: 1, refCount: false })
      );

    this.bottleneckCache.set(topN, request$);
    return request$;
  }
}
