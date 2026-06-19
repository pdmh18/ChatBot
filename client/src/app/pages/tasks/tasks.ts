import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-tasks',
  imports: [FormsModule],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks {
  tasks: any[] = [];
  selectedTask: any = null;
  suggestionTask: any = null;

  searchText = '';
  statusFilter = 'All';
  priorityFilter = 'All';

  isAddTaskOpen = false;

  newTask = {
    name: '',
    project: 'AI Project Risk',
    sprint: 'Sprint 1',
    assignee: '',
    status: 'Todo',
    priority: 'Medium',
    deadline: '',
    riskScore: 50,
  };

  suggestedDevelopers = [
    {
      name: 'Bình',
      score: 91,
      workload: 55,
      reason: 'Có kinh nghiệm xử lý API và workload còn phù hợp.',
    },
    {
      name: 'Oanh',
      score: 88,
      workload: 60,
      reason: 'Phù hợp với UI/Kanban và đã làm frontend chính.',
    },
    {
      name: 'An',
      score: 74,
      workload: 80,
      reason: 'Có kinh nghiệm nhưng tải công việc hiện tại cao.',
    },
  ];

  constructor(private taskService: TaskService) {
    this.tasks = this.taskService.getTasks();
  }

  get filteredTasks() {
    return this.tasks.filter((task) => {
      const matchesSearch = task.name
        .toLowerCase()
        .includes(this.searchText.toLowerCase());

      const matchesStatus =
        this.statusFilter === 'All' || task.status === this.statusFilter;

      const matchesPriority =
        this.priorityFilter === 'All' || task.priority === this.priorityFilter;

      return matchesSearch && matchesStatus && matchesPriority;
    });
  }

  selectTask(task: any) {
    this.selectedTask = task;
  }

  showAiSuggestion(task: any) {
    this.suggestionTask = task;
  }

  closeSuggestion() {
    this.suggestionTask = null;
  }

  assignDeveloper(developer: any) {
    if (this.suggestionTask) {
      this.suggestionTask.assignee = developer.name;
    }

    this.closeSuggestion();
  }

  openAddTask() {
    this.isAddTaskOpen = true;
  }

  closeAddTask() {
    this.isAddTaskOpen = false;
  }

  addTask() {
    if (!this.newTask.name.trim()) {
      return;
    }

    this.tasks.push({ ...this.newTask });

    this.newTask = {
      name: '',
      project: 'AI Project Risk',
      sprint: 'Sprint 1',
      assignee: '',
      status: 'Todo',
      priority: 'Medium',
      deadline: '',
      riskScore: 50,
    };

    this.closeAddTask();
  }
}