import { Component } from '@angular/core';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';

interface KanbanTask {
  name: string;
  assignee: string;
  priority: string;
  deadline: string;
  riskScore: number;
}

@Component({
  selector: 'app-kanban',
  imports: [DragDropModule],
  templateUrl: './kanban.html',
  styleUrl: './kanban.scss',
})
export class Kanban {
  todo: KanbanTask[] = [
    {
      name: 'Thiết kế database',
      assignee: 'An',
      priority: 'High',
      deadline: '2026-06-25',
      riskScore: 82,
    },
    {
      name: 'Màn hình AI Alerts',
      assignee: 'Oanh',
      priority: 'High',
      deadline: '2026-06-29',
      riskScore: 68,
    },
  ];

  inProgress: KanbanTask[] = [
    {
      name: 'API đăng nhập',
      assignee: 'Bình',
      priority: 'Medium',
      deadline: '2026-06-27',
      riskScore: 45,
    },
  ];

  review: KanbanTask[] = [
    {
      name: 'Dashboard thống kê',
      assignee: 'Oanh',
      priority: 'Medium',
      deadline: '2026-06-26',
      riskScore: 35,
    },
  ];

  done: KanbanTask[] = [
    {
      name: 'Khởi tạo Angular project',
      assignee: 'Oanh',
      priority: 'Low',
      deadline: '2026-06-20',
      riskScore: 10,
    },
  ];

  drop(event: CdkDragDrop<KanbanTask[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }
  }
}