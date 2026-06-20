import { Injectable } from '@angular/core';
import { Task } from '../models/task';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  getTasks(): Task[] {
    return [
      {
        id: 1,
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
        id: 2,
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
        id: 3,
        name: 'Màn hình Kanban',
        project: 'AI Project Risk',
        sprint: 'Sprint 2',
        assignee: 'Oanh',
        status: 'Todo',
        priority: 'High',
        deadline: '2026-06-30',
        riskScore: 68,
      },
      {
        id: 4,
        name: 'Dashboard thống kê',
        project: 'AI Project Risk',
        sprint: 'Sprint 2',
        assignee: 'Oanh',
        status: 'Review',
        priority: 'Medium',
        deadline: '2026-06-26',
        riskScore: 35,
      },
      {
        id: 5,
        name: 'Khởi tạo Angular project',
        project: 'AI Project Risk',
        sprint: 'Sprint 1',
        assignee: 'Oanh',
        status: 'Done',
        priority: 'Low',
        deadline: '2026-06-20',
        riskScore: 10,
      },
    ];
  }
}
