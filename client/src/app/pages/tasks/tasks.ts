import { Component } from '@angular/core';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-tasks',
  imports: [],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks {
  tasks: any[] = [];
  selectedTask: any = null;
  aiSuggestion = '';

  constructor(private taskService: TaskService) {
    this.tasks = this.taskService.getTasks();
  }

  selectTask(task: any) {
    this.selectedTask = task;
  }

  showAiSuggestion(task: any) {
    this.aiSuggestion =
      'AI đề xuất kiểm tra task "' +
      task.name +
      '" vì có nguy cơ trễ hạn ' +
      task.riskScore +
      '%.';
  }
}