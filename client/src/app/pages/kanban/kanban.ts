import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { Task, getTaskPriorityLabel } from '../../models/task';
import { TaskService } from '../../services/task';

type KanbanStatus = 'Can lam' | 'Dang lam' | 'Cho kiem tra' | 'Hoan thanh';

@Component({
  selector: 'app-kanban',
  imports: [DragDropModule],
  templateUrl: './kanban.html',
  styleUrl: './kanban.scss',
})
export class Kanban implements OnInit {
  todo: Task[] = [];
  inProgress: Task[] = [];
  review: Task[] = [];
  done: Task[] = [];

  message = 'Đang tải dữ liệu Kanban từ backend API...';

  constructor(
    private taskService: TaskService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.message = 'Đang tải dữ liệu Kanban từ backend API...';

    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 200 }).subscribe({
      next: (tasks) => {
        this.todo = [];
        this.inProgress = [];
        this.review = [];
        this.done = [];

        tasks.forEach((task) => {
          const status = this.normalizeStatus(task.status);
          task.status = status;

          if (status === 'Dang lam') this.inProgress.push(task);
          else if (status === 'Cho kiem tra') this.review.push(task);
          else if (status === 'Hoan thanh') this.done.push(task);
          else this.todo.push(task);
        });

        this.message = tasks.length
          ? `Đã tải ${tasks.length} task từ backend API.`
          : 'Backend chưa có task để hiển thị trên Kanban.';

        window.setTimeout(() => {
          this.message = '';
          this.cdr.detectChanges();
        }, 2500);

        this.cdr.detectChanges();
      },
      error: () => {
        this.todo = [];
        this.inProgress = [];
        this.review = [];
        this.done = [];
        this.message = 'Chưa tải được dữ liệu Kanban. Kiểm tra backend có đang chạy ở port 49261 không.';
        this.cdr.detectChanges();
      },
    });
  }

  drop(event: CdkDragDrop<Task[]>): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );

    const task = event.container.data[event.currentIndex];
    const nextStatus = this.getStatusByListId(event.container.id);
    task.status = nextStatus;
    task.progress = this.getProgressByStatus(nextStatus);

    this.taskService
      .updateTaskStatus(task.id, {
        trangThai: nextStatus,
        tienDo: task.progress,
      })
      .subscribe({
        next: () => {
          this.message = `Đã cập nhật "${task.name}" sang ${this.getStatusLabel(nextStatus)}.`;
          window.setTimeout(() => {
            this.message = '';
            this.cdr.detectChanges();
          }, 2200);
        },
        error: () => {
          this.message = 'Chưa cập nhật được trạng thái. Đang tải lại Kanban.';
          this.loadTasks();
        },
      });
  }

  getPriorityLabel(value?: string | null): string {
    return getTaskPriorityLabel(value);
  }

  getStatusLabel(status: KanbanStatus): string {
    if (status === 'Dang lam') return 'Đang làm';
    if (status === 'Cho kiem tra') return 'Chờ kiểm tra';
    if (status === 'Hoan thanh') return 'Hoàn thành';
    return 'Cần làm';
  }

  private getStatusByListId(listId: string): KanbanStatus {
    if (listId.includes('inProgress')) return 'Dang lam';
    if (listId.includes('review')) return 'Cho kiem tra';
    if (listId.includes('done')) return 'Hoan thanh';
    return 'Can lam';
  }

  private getProgressByStatus(status: KanbanStatus): number {
    if (status === 'Hoan thanh') return 100;
    if (status === 'Cho kiem tra') return 90;
    if (status === 'Dang lam') return 40;
    return 0;
  }

  private normalizeStatus(value?: string | null): KanbanStatus {
    const normalized = (value ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .trim();

    if (normalized.includes('dang') || normalized.includes('progress')) return 'Dang lam';
    if (normalized.includes('kiem tra') || normalized.includes('review') || normalized.includes('cho duyet')) {
      return 'Cho kiem tra';
    }
    if (normalized.includes('hoan thanh') || normalized.includes('done') || normalized.includes('complete')) {
      return 'Hoan thanh';
    }

    return 'Can lam';
  }
}
