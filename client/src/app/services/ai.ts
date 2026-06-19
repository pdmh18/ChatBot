import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AssigneeSuggestion, Bottleneck, LateRisk } from '../models/ai';

@Injectable({
  providedIn: 'root',
})
export class AiService {
  private apiUrl = environment.apiUrl;

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
    return this.http.get<LateRisk[]>(`${this.apiUrl}/ai/late-risk`);
  }

  getBottlenecksApi(): Observable<Bottleneck[]> {
    return this.http.get<Bottleneck[]>(`${this.apiUrl}/ai/bottlenecks`);
  }

  getAssigneeSuggestionsApi(): Observable<AssigneeSuggestion[]> {
    return this.http.get<AssigneeSuggestion[]>(`${this.apiUrl}/ai/assignee-suggestions`);
  }
}
