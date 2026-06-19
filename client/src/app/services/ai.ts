import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getLateRisks() {
    return [
      {
        task: 'Thiết kế database',
        risk: 82,
        reason: 'Deadline gần, độ ưu tiên cao.',
      },
      {
        task: 'Màn hình Kanban',
        risk: 68,
        reason: 'Khối lượng công việc lớn.',
      },
    ];
  }

  getBottlenecks() {
    return [
      {
        task: 'Database Schema',
        blockedTasks: 5,
      },
      {
        task: 'API Authentication',
        blockedTasks: 3,
      },
    ];
  }

  getAssigneeSuggestions() {
    return [
      {
        task: 'API đăng nhập',
        developer: 'Bình',
        score: 91,
      },
      {
        task: 'Kanban UI',
        developer: 'Oanh',
        score: 88,
      },
    ];
  }

  getLateRisksApi() {
    return this.http.get(`${this.apiUrl}/ai/late-risk`);
  }

  getBottlenecksApi() {
    return this.http.get(`${this.apiUrl}/ai/bottlenecks`);
  }

  getAssigneeSuggestionsApi() {
    return this.http.get(`${this.apiUrl}/ai/assignee-suggestions`);
  }
}