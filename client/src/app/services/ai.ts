import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
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

  constructor(private http: HttpClient) {}

  getLateRisks(): LateRisk[] {
    return [
      {
        id: 1,
        task: 'Thiết kế database',
        risk: 82,
        reason: 'Deadline gần, độ ưu tiên cao.',
      },
      {
        id: 2,
        task: 'Màn hình Kanban',
        risk: 68,
        reason: 'Khối lượng công việc lớn.',
      },
    ];
  }

  getBottlenecks(): Bottleneck[] {
    return [
      {
        id: 1,
        task: 'Database Schema',
        blockedTasks: 5,
      },
      {
        id: 2,
        task: 'API Authentication',
        blockedTasks: 3,
      },
    ];
  }

  getAssigneeSuggestions(): AssigneeSuggestion[] {
    return [
      {
        id: 1,
        task: 'API đăng nhập',
        developer: 'Bình',
        score: 91,
      },
      {
        id: 2,
        task: 'Kanban UI',
        developer: 'Oanh',
        score: 88,
      },
    ];
  }

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
    return this.http.post<StaffMatchResult[]>(
      `${this.apiUrl}/tasks/${taskId}/suggest-assignees`,
      {}
    );
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
    const params = new HttpParams().set('topN', topN);

    return this.http
      .post<BottleneckResult[] | { value?: BottleneckResult[] }>(
        `${this.apiUrl}/bottlenecks/analyze`,
        {},
        { params }
      )
      .pipe(map((response) => (Array.isArray(response) ? response : response.value ?? [])));
  }
}



