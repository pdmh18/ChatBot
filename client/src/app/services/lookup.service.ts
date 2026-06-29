import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  LookupItemDto,
  SprintLookupDto,
  UserLookupDto,
} from '../models/lookups.model';

@Injectable({
  providedIn: 'root',
})
export class LookupService {
  private readonly apiUrl = `${environment.apiUrl}/lookups`;
  private projects$?: Observable<LookupItemDto[]>;
  private users$?: Observable<UserLookupDto[]>;
  private roles$?: Observable<LookupItemDto[]>;
  private skills$?: Observable<LookupItemDto[]>;
  private taskStatuses$?: Observable<LookupItemDto[]>;
  private taskPriorities$?: Observable<LookupItemDto[]>;
  private sprintsCache = new Map<string, Observable<SprintLookupDto[]>>();

  constructor(private http: HttpClient) {}

  getProjects(): Observable<LookupItemDto[]> {
    this.projects$ ??= this.http
      .get<LookupItemDto[]>(`${this.apiUrl}/projects`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.projects$;
  }

  getUsers(): Observable<UserLookupDto[]> {
    this.users$ ??= this.http
      .get<UserLookupDto[]>(`${this.apiUrl}/users`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.users$;
  }

  getSprints(projectId?: number): Observable<SprintLookupDto[]> {
    let params = new HttpParams();

    if (projectId) {
      params = params.set('projectId', projectId);
    }

    const cacheKey = String(projectId ?? 'all');
    const cached = this.sprintsCache.get(cacheKey);
    if (cached) return cached;

    const request$ = this.http
      .get<SprintLookupDto[]>(`${this.apiUrl}/sprints`, { params })
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    this.sprintsCache.set(cacheKey, request$);
    return request$;
  }

  getRoles(): Observable<LookupItemDto[]> {
    this.roles$ ??= this.http
      .get<LookupItemDto[]>(`${this.apiUrl}/roles`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.roles$;
  }

  getSkills(): Observable<LookupItemDto[]> {
    this.skills$ ??= this.http
      .get<LookupItemDto[]>(`${this.apiUrl}/skills`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.skills$;
  }

  getTaskStatuses(): Observable<LookupItemDto[]> {
    this.taskStatuses$ ??= this.http
      .get<LookupItemDto[]>(`${this.apiUrl}/task-statuses`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.taskStatuses$;
  }

  getTaskPriorities(): Observable<LookupItemDto[]> {
    this.taskPriorities$ ??= this.http
      .get<LookupItemDto[]>(`${this.apiUrl}/task-priorities`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.taskPriorities$;
  }
}
