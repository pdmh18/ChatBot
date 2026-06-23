import { AfterViewInit, Component } from '@angular/core';
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
  constructor(private taskService: TaskService) {}

  ngAfterViewInit(): void {
    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => this.renderGantt(tasks.slice(0, 30)),
      error: () => this.renderGantt([]),
    });
  }

  private renderGantt(tasks: Task[]): void {
    const ganttTasks = tasks
      .filter((task) => task.deadline)
      .map((task) => ({
        id: String(task.id),
        name: task.name,
        start: task.startDate || this.getFallbackStart(task.deadline),
        end: task.deadline,
        progress: task.progress ?? 0,
        dependencies: '',
      }));

    if (!ganttTasks.length) return;

    new FrappeGantt('#gantt', ganttTasks);
  }

  private getFallbackStart(deadline: string): string {
    const date = new Date(deadline);
    date.setDate(date.getDate() - 3);
    return date.toISOString().slice(0, 10);
  }
}
