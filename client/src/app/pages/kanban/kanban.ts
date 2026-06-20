import { Component } from '@angular/core';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { Task } from '../../models/task';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-kanban',
  imports: [DragDropModule],
  templateUrl: './kanban.html',
  styleUrl: './kanban.scss',
})
export class Kanban {
  todo: Task[] = [];
  inProgress: Task[] = [];
  review: Task[] = [];
  done: Task[] = [];

  constructor(private taskService: TaskService) {
    const tasks = this.taskService.getTasks();
    this.todo = tasks.filter((task) => task.status === 'Todo');
    this.inProgress = tasks.filter((task) => task.status === 'In Progress');
    this.review = tasks.filter((task) => task.status === 'Review');
    this.done = tasks.filter((task) => task.status === 'Done');
  }

  drop(event: CdkDragDrop<Task[]>): void {
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
