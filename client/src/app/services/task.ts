import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  getTasks() {
    return [
      {
        name: 'Thiết kế database',
        project: 'AI Project Risk',
        sprint: 'Sprint 1',
        assignee: 'An',
        status: 'Todo',
        priority: 'High',
        deadline: '2026-06-25',
        riskScore: 82,
      },
      {
        name: 'API đăng nhập',
        project: 'AI Project Risk',
        sprint: 'Sprint 1',
        assignee: 'Bình',
        status: 'In Progress',
        priority: 'Medium',
        deadline: '2026-06-27',
        riskScore: 45,
      },
      {
        name: 'Màn hình Kanban',
        project: 'AI Project Risk',
        sprint: 'Sprint 2',
        assignee: 'Oanh',
        status: 'Todo',
        priority: 'High',
        deadline: '2026-06-30',
        riskScore: 68,
      },
    ];
  }
}