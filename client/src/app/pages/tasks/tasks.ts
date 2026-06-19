import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Task, TaskPriority, TaskStatus } from '../../models/task';
import { TaskService } from '../../services/task';

interface DeveloperSuggestion {
  id: number;
  name: string;
  score: number;
  workload: number;
  reason: string;
}

@Component({
  selector: 'app-tasks',
  imports: [FormsModule],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks {
  tasks: Task[] = [];
  selectedTask: Task | null = null;
  suggestionTask: Task | null = null;

  searchText = '';
  statusFilter: TaskStatus | 'All' = 'All';
  priorityFilter: TaskPriority | 'All' = 'All';
  formMessage = '';
  successMessage = '';

  isAddTaskOpen = false;

  newTask: Omit<Task, 'id'> = this.createEmptyTask();

  suggestedDevelopers: DeveloperSuggestion[] = [
    {
      id: 1,
      name: 'Bình',
      score: 91,
      workload: 55,
      reason: 'Có kinh nghiệm xử lý API và workload còn phù hợp.',
    },
    {
      id: 2,
      name: 'Oanh',
      score: 88,
      workload: 60,
      reason: 'Phù hợp với UI/Kanban và đã làm frontend chính.',
    },
    {
      id: 3,
      name: 'An',
      score: 74,
      workload: 80,
      reason: 'Có kinh nghiệm nhưng tải công việc hiện tại cao.',
    },
  ];

  constructor(private taskService: TaskService) {
    this.tasks = this.taskService.getTasks();
  }

  get filteredTasks(): Task[] {
    return this.tasks.filter((task) => {
      const matchesSearch = (task.name?.toLowerCase() ?? '').includes(
        this.searchText.toLowerCase()
      );

      const matchesStatus =
        this.statusFilter === 'All' || task.status === this.statusFilter;

      const matchesPriority =
        this.priorityFilter === 'All' || task.priority === this.priorityFilter;

      return matchesSearch && matchesStatus && matchesPriority;
    });
  }

  selectTask(task: Task): void {
    this.selectedTask = task;
  }

  showAiSuggestion(task: Task): void {
    this.suggestionTask = task;
  }

  closeSuggestion(): void {
    this.suggestionTask = null;
  }

  assignDeveloper(developer: DeveloperSuggestion): void {
    if (this.suggestionTask) {
      this.suggestionTask.assignee = developer.name;
      this.successMessage = `Đã gán task "${this.suggestionTask.name}" cho ${developer.name}.`;
    }

    this.closeSuggestion();
  }

  openAddTask(): void {
    this.formMessage = '';
    this.isAddTaskOpen = true;
  }

  closeAddTask(): void {
    this.isAddTaskOpen = false;
  }

  addTask(): void {
    const validationError = this.validateTask();

    if (validationError) {
      this.formMessage = validationError;
      return;
    }

    const nextId = Math.max(0, ...this.tasks.map((task) => task.id)) + 1;
    this.tasks.push({ id: nextId, ...this.newTask });
    this.successMessage = `Đã thêm task "${this.newTask.name}".`;
    this.newTask = this.createEmptyTask();
    this.closeAddTask();
  }

  private validateTask(): string {
    const validStatuses: TaskStatus[] = ['Todo', 'In Progress', 'Review', 'Done'];
    const validPriorities: TaskPriority[] = ['High', 'Medium', 'Low'];
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const deadline = new Date(this.newTask.deadline);

    if (!this.newTask.name.trim()) {
      return 'Tên task không được để trống.';
    }

    if (this.newTask.name.trim().length > 80) {
      return 'Tên task không được dài quá 80 ký tự.';
    }

    if (!this.newTask.deadline || Number.isNaN(deadline.getTime())) {
      return 'Deadline không hợp lệ.';
    }

    if (deadline < today) {
      return 'Deadline phải là ngày hiện tại hoặc tương lai.';
    }

    if (this.newTask.riskScore < 0 || this.newTask.riskScore > 100) {
      return 'Risk AI phải nằm trong khoảng 0 đến 100.';
    }

    if (!validStatuses.includes(this.newTask.status)) {
      return 'Trạng thái task không hợp lệ.';
    }

    if (!validPriorities.includes(this.newTask.priority)) {
      return 'Độ ưu tiên task không hợp lệ.';
    }

    return '';
  }

  private createEmptyTask(): Omit<Task, 'id'> {
    return {
      name: '',
      project: 'AI Project Risk',
      sprint: 'Sprint 1',
      assignee: '',
      status: 'Todo',
      priority: 'Medium',
      deadline: '',
      riskScore: 50,
    };
  }
}
