import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';
import { DashboardSummary, DashboardTaskAlert, DashboardWorkload } from '../models/dashboard';

interface ApiValueResponse<T> {
  value?: T;
  Value?: T;
}

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly apiUrl = `${environment.apiUrl}/dashboard`;
  private readonly alertsCache = new Map<string, Observable<DashboardTaskAlert[]>>();

  constructor(private http: HttpClient) {}

  getSummary(projectId?: number, sprintId?: number): Observable<DashboardSummary> {
    return this.http
      .get<DashboardSummary | ApiValueResponse<DashboardSummary>>(`${this.apiUrl}/summary`, {
        params: this.buildParams({ projectId, sprintId }),
      })
      .pipe(map((response) => this.unwrapValue(response)));
  }

  getWorkload(projectId?: number, sprintId?: number): Observable<DashboardWorkload[]> {
    return this.http
      .get<DashboardWorkload[] | ApiValueResponse<DashboardWorkload[]>>(`${this.apiUrl}/workload`, {
        params: this.buildParams({ projectId, sprintId }),
      })
      .pipe(map((response) => this.unwrapList(response)));
  }

  getAlerts(type: 'late-risk' | 'bottleneck', projectId?: number, sprintId?: number): Observable<DashboardTaskAlert[]> {
    const cacheKey = `${type}:${projectId ?? 'all'}:${sprintId ?? 'all'}`;
    const cached = this.alertsCache.get(cacheKey);
    if (cached) return cached;

    const request$ = this.http
      .get<DashboardTaskAlert[] | ApiValueResponse<DashboardTaskAlert[]>>(`${this.apiUrl}/alerts`, {
        params: this.buildParams({ projectId, sprintId, type }),
      })
      .pipe(
        map((response) => this.unwrapList(response)),
        shareReplay({ bufferSize: 1, refCount: false })
      );

    this.alertsCache.set(cacheKey, request$);
    return request$;
  }

  private unwrapValue<T>(response: T | ApiValueResponse<T>): T {
    if (response && typeof response === 'object' && ('value' in response || 'Value' in response)) {
      return ((response as ApiValueResponse<T>).value ?? (response as ApiValueResponse<T>).Value) as T;
    }

    return response as T;
  }

  private unwrapList<T>(response: T[] | ApiValueResponse<T[]>): T[] {
    if (Array.isArray(response)) return response;
    return response.value ?? response.Value ?? [];
  }

  private buildParams(values: Record<string, string | number | undefined | null>): HttpParams {
    let params = new HttpParams();

    Object.entries(values).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return params;
  }
}
