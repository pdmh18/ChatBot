import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
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

  constructor(private http: HttpClient) {}

  getProjects(): Observable<LookupItemDto[]> {
    return this.http.get<LookupItemDto[]>(`${this.apiUrl}/projects`);
  }

  getUsers(): Observable<UserLookupDto[]> {
    return this.http.get<UserLookupDto[]>(`${this.apiUrl}/users`);
  }

  getSprints(projectId?: number): Observable<SprintLookupDto[]> {
    let params = new HttpParams();

    if (projectId) {
      params = params.set('projectId', projectId);
    }

    return this.http.get<SprintLookupDto[]>(`${this.apiUrl}/sprints`, { params });
  }

  getRoles(): Observable<LookupItemDto[]> {
    return this.http.get<LookupItemDto[]>(`${this.apiUrl}/roles`);
  }

  getSkills(): Observable<LookupItemDto[]> {
    return this.http.get<LookupItemDto[]>(`${this.apiUrl}/skills`);
  }

  getTaskStatuses(): Observable<LookupItemDto[]> {
    return this.http.get<LookupItemDto[]>(`${this.apiUrl}/task-statuses`);
  }

  getTaskPriorities(): Observable<LookupItemDto[]> {
    return this.http.get<LookupItemDto[]>(`${this.apiUrl}/task-priorities`);
  }
}
