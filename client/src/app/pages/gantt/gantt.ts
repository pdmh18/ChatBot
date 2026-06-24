import { AfterViewInit, ChangeDetectorRef, Component } from '@angular/core';
import FrappeGantt from 'frappe-gantt';
import { Task } from '../../models/task';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-gantt',
  imports: [],
  templateUrl: './gantt.html',
  styleUrl: './gantt.scss',
})
export class Gantt implements AfterViewInit {
  emptyMessage = 'Đang tải dữ liệu Gantt từ backend API...';

  constructor(
    private taskService: TaskService,
    private changeDetector: ChangeDetectorRef,
  ) {}

  ngAfterViewInit(): void {
    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => this.renderGantt(tasks.slice(0, 30)),
      error: () => this.renderEmptyState('Chưa tải được dữ liệu Gantt. Kiểm tra backend có đang chạy không.'),
    });
  }

  private renderGantt(tasks: Task[]): void {
    const ganttTasks = tasks
      .filter((task) => this.isValidDate(task.deadline))
      .map((task) => ({
        id: String(task.id),
        name: task.name,
        start: this.isValidDate(task.startDate) ? task.startDate! : this.getFallbackStart(task.deadline),
        end: task.deadline,
        progress: task.progress ?? 0,
        dependencies: '',
      }));

    if (!ganttTasks.length) {
      this.renderEmptyState('Chưa có task hợp lệ để vẽ Gantt Chart. Task cần có deadline đúng định dạng ngày.');
      return;
    }

    this.emptyMessage = '';
    this.changeDetector.detectChanges();

    new FrappeGantt('#gantt', ganttTasks);
  }

  private renderEmptyState(message: string): void {
    this.emptyMessage = message;
    const ganttElement = document.querySelector('#gantt');
    if (ganttElement) ganttElement.innerHTML = '';
  }

  private getFallbackStart(deadline: string): string {
    const date = this.parseLocalDate(deadline);
    if (!date) return this.getTodayString();

    date.setDate(date.getDate() - 3);
    return this.formatLocalDate(date);
  }

  private isValidDate(value?: string | null): value is string {
    return !!this.parseLocalDate(value);
  }

  private parseLocalDate(value?: string | null): Date | null {
    if (!value) return null;

    const [year, month, day] = value.split('T')[0].split('-').map(Number);
    if (!year || !month || !day) return null;

    const date = new Date(year, month - 1, day);

    if (
      Number.isNaN(date.getTime()) ||
      date.getFullYear() !== year ||
      date.getMonth() !== month - 1 ||
      date.getDate() !== day
    ) {
      return null;
    }

    return date;
  }

  private formatLocalDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private getTodayString(): string {
    return this.formatLocalDate(new Date());
  }
}