import { Component } from '@angular/core';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-kanban',
  imports: [DragDropModule],
  templateUrl: './kanban.html',
  styleUrl: './kanban.scss',
})
export class Kanban {
  todo = ['Thiết kế database', 'Màn hình AI Alerts'];
  inProgress = ['API đăng nhập'];
  review = ['Dashboard thống kê'];
  done = ['Khởi tạo Angular project'];

  drop(event: CdkDragDrop<string[]>) {
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