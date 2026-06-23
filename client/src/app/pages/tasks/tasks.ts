import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, of } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { LookupItemDto, SprintLookupDto, UserLookupDto } from '../../models/lookups.model';
import {
  CreateTaskRequest,
  Task,
  TaskPriority,
  TaskStatus,
  getTaskPriorityLabel,
  getTaskStatusLabel,
} from '../../models/task';
import { LookupService } from '../../services/lookup.service';
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
export class Tasks implements OnInit {
  tasks: Task[] = [];
  selectedTask: Task | null = null;
  suggestionTask: Task | null = null;

  projects: LookupItemDto[] = [];
  users: UserLookupDto[] = [];
  sprints: SprintLookupDto[] = [];
  filteredSprintsForForm: SprintLookupDto[] = [];
  statuses: LookupItemDto[] = [];
  priorities: LookupItemDto[] = [];
  roles: LookupItemDto[] = [];
  skills: LookupItemDto[] = [];

  searchText = '';
  statusFilter: TaskStatus | 'All' = 'All';
  priorityFilter: TaskPriority | 'All' = 'All';
  formMessage = '';
  successMessage = '';
  lookupMessage = '';
  taskDataMessage = 'Đang tải danh sách task từ backend...';

  isAddTaskOpen = false;

  newTask: Omit<Task, 'id'> = this.createEmptyTask();
  suggestedDevelopers: DeveloperSuggestion[] = [];

  constructor(
    private lookupService: LookupService,
    private taskService: TaskService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadLookups();
    this.loadTasks();
  }

  get filteredTasks(): Task[] {
    return this.tasks.filter((task) => {
      const matchesSearch = (task.name?.toLowerCase() ?? '').includes(
        this.searchText.toLowerCase()
      );
      const matchesStatus = this.statusFilter === 'All' || task.status === this.statusFilter;
      const matchesPriority = this.priorityFilter === 'All' || task.priority === this.priorityFilter;
      return matchesSearch && matchesStatus && matchesPriority;
    });
  }

  loadTasks(): void {
    this.taskDataMessage = 'Đang tải danh sách task từ backend...';

    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => {
        this.tasks = tasks;
        this.taskDataMessage = tasks.length
          ? ''
          : 'Backend chưa có task nào. Bấm + Thêm task để tạo task đầu tiên.';
        this.cdr.detectChanges();
      },
      error: () => {
        this.tasks = [];
        this.taskDataMessage = 'Chưa tải được API danh sách task. Kiểm tra backend có đang chạy ở port 49261 không.';
        this.cdr.detectChanges();
      },
    });
  }

  loadLookups(): void {
    this.lookupMessage = 'Đang tải lookup từ backend API...';

    let completedRequests = 0;
    let failedRequests = 0;
    const totalRequests = 7;

    const markDone = () => {
      completedRequests += 1;

      if (completedRequests === totalRequests) {
        this.filteredSprintsForForm = this.sprints;
        this.suggestedDevelopers = this.buildDeveloperSuggestions();
        this.newTask = this.createEmptyTask();
        this.lookupMessage =
          failedRequests > 0
            ? `Đã tải ${totalRequests - failedRequests}/${totalRequests} lookup API.`
            : 'Đã tải đủ 7 lookup API từ backend.';

        window.setTimeout(() => {
          this.lookupMessage = '';
          this.cdr.detectChanges();
        }, 3000);
      }

      this.cdr.detectChanges();
    };

    this.loadLookup(this.lookupService.getProjects(), (data) => (this.projects = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getUsers(), (data) => (this.users = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getSprints(), (data) => (this.sprints = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getTaskStatuses(), (data) => (this.statuses = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getTaskPriorities(), (data) => (this.priorities = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getRoles(), (data) => (this.roles = data), markDone, () => failedRequests++);
    this.loadLookup(this.lookupService.getSkills(), (data) => (this.skills = data), markDone, () => failedRequests++);
  }

  private loadLookup<T>(
    request: Observable<T[]>,
    setData: (data: T[]) => void,
    markDone: () => void,
    markFailed: () => void
  ): void {
    request
      .pipe(
        timeout(5000),
        catchError(() => {
          markFailed();
          return of([] as T[]);
        })
      )
      .subscribe((data) => {
        setData(data);
        markDone();
      });
  }

  getStatusLabel(value?: string | null): string {
    return getTaskStatusLabel(value);
  }

  getPriorityLabel(value?: string | null): string {
    return getTaskPriorityLabel(value);
  }

  onProjectChange(projectName: string): void {
    const selectedProject = this.projects.find((project) => project.name === projectName);

    this.filteredSprintsForForm = selectedProject
      ? this.sprints.filter((sprint) => sprint.maDuAn === selectedProject.id)
      : this.sprints;

    this.newTask.sprint = this.filteredSprintsForForm[0]?.tenSprint ?? '';
  }

  selectTask(task: Task): void {
    this.selectedTask = null;
    this.taskService.getTaskViewById(task.id).subscribe({
      next: (detail) => {
        this.selectedTask = detail;
        this.cdr.detectChanges();
      },
      error: () => {
        this.selectedTask = task;
        this.cdr.detectChanges();
      },
    });
  }

  closeTaskDetail(): void {
    this.selectedTask = null;
    this.cdr.detectChanges();
  }

  showAiSuggestion(task: Task): void {
    this.suggestionTask = task;
    this.suggestedDevelopers = this.buildDeveloperSuggestions();
    this.cdr.detectChanges();
  }

  closeSuggestion(): void {
    this.suggestionTask = null;
    this.cdr.detectChanges();
  }

  assignDeveloper(developer: DeveloperSuggestion): void {
    if (!this.suggestionTask) return;

    this.taskService.assignTask(this.suggestionTask.id, { maNguoiPhuTrach: developer.id }).subscribe({
      next: () => {
        this.successMessage = `Đã gán task "${this.suggestionTask?.name}" cho ${developer.name}.`;
        this.closeSuggestion();
        this.loadTasks();
      },
      error: () => {
        this.successMessage = 'Chưa phân công được task. Kiểm tra backend rồi thử lại.';
        this.closeSuggestion();
      },
    });
  }

  openAddTask(): void {
    this.formMessage = '';
    this.isAddTaskOpen = true;
  }

  closeAddTask(): void {
    this.isAddTaskOpen = false;
    this.cdr.detectChanges();
  }

  addTask(): void {
    const validationError = this.validateTask();

    if (validationError) {
      this.formMessage = validationError;
      return;
    }

    this.taskService.createTask(this.buildCreatePayload()).subscribe({
      next: (result) => {
        this.successMessage = result.message || `Đã thêm task "${this.newTask.name}".`;
        this.newTask = this.createEmptyTask();
        this.closeAddTask();
        this.loadTasks();
      },
      error: (error) => {
        this.formMessage = error?.error?.message || 'Chưa thêm được task. Kiểm tra dữ liệu hoặc backend rồi thử lại.';
      },
    });
  }

  deleteTask(task: Task): void {
    const confirmed = window.confirm(`Xóa task "${task.name}"?`);
    if (!confirmed) return;

    this.taskService.deleteTask(task.id).subscribe({
      next: () => {
        this.successMessage = `Đã xóa task "${task.name}".`;
        this.loadTasks();
      },
      error: () => {
        this.successMessage = 'Chưa xóa được task. Task có thể đang có dữ liệu phụ thuộc.';
      },
    });
  }

  private buildCreatePayload(): CreateTaskRequest {
    const project = this.projects.find((item) => item.name === this.newTask.project);
    const sprint = this.sprints.find((item) => item.tenSprint === this.newTask.sprint && item.maDuAn === project?.id);
    const assignee = this.users.find((item) => item.hoTen === this.newTask.assignee);
    const creator = this.users[0];

    return {
      maDuAn: project?.id ?? 1,
      maSprint: sprint?.id ?? null,
      maCongViecCode: null,
      tenCongViec: this.newTask.name.trim(),
      moTa: this.newTask.description ?? null,
      maNguoiTao: creator?.id ?? assignee?.id ?? 1,
      maNguoiPhuTrach: assignee?.id ?? null,
      doUuTien: this.newTask.priority,
      trangThai: this.newTask.status,
      ngayBatDau: this.newTask.startDate || this.getTodayString(),
      hanChot: this.newTask.deadline,
      soGioUocTinh: this.newTask.estimatedHours ?? 8,
      tienDo: this.newTask.progress ?? 0,
    };
  }

  private validateTask(): string {
    const validStatuses = this.statuses.map((status) => status.name);
    const validPriorities = this.priorities.map((priority) => priority.name);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const deadline = new Date(this.newTask.deadline);

    if (!this.newTask.name.trim()) return 'Tên task không được để trống.';
    if (this.newTask.name.trim().length > 80) return 'Tên task không được dài quá 80 ký tự.';
    if (!this.newTask.project) return 'Vui lòng chọn dự án.';
    if (!this.newTask.assignee) return 'Vui lòng chọn người phụ trách.';
    if (!this.newTask.deadline || Number.isNaN(deadline.getTime())) return 'Deadline không hợp lệ.';
    if (deadline < today) return 'Deadline phải là ngày hiện tại hoặc tương lai.';
    if (!validStatuses.includes(this.newTask.status)) return 'Trạng thái task không hợp lệ.';
    if (!validPriorities.includes(this.newTask.priority)) return 'Độ ưu tiên task không hợp lệ.';

    return '';
  }

  private createEmptyTask(): Omit<Task, 'id'> {
    const selectedProject = this.projects[0];
    const availableSprints = selectedProject
      ? this.sprints.filter((sprint) => sprint.maDuAn === selectedProject.id)
      : this.sprints;

    this.filteredSprintsForForm = availableSprints;

    return {
      name: '',
      description: '',
      projectId: selectedProject?.id ?? null,
      project: selectedProject?.name ?? '',
      sprintId: availableSprints[0]?.id ?? null,
      sprint: availableSprints[0]?.tenSprint ?? '',
      assigneeId: this.users[0]?.id ?? null,
      assignee: this.users[0]?.hoTen ?? '',
      status: this.statuses[0]?.name ?? 'Can lam',
      priority: this.priorities[1]?.name ?? this.priorities[0]?.name ?? 'Trung binh',
      startDate: this.getTodayString(),
      deadline: '',
      estimatedHours: 8,
      progress: 0,
      riskScore: 0,
      riskLevel: 'Low',
    };
  }

  private buildDeveloperSuggestions(): DeveloperSuggestion[] {
    if (!this.users.length) {
      return [
        { id: 1, name: 'Lập trình viên phù hợp', score: 88, workload: 50, reason: 'Chưa tải được danh sách users từ backend, đang hiển thị gợi ý tạm.' },
      ];
    }

    return this.users.slice(0, 3).map((user, index) => ({
      id: user.id,
      name: user.hoTen,
      score: 92 - index * 7,
      workload: 45 + index * 10,
      reason: `Vai trò: ${user.vaiTro}. Phù hợp để nhận task theo dữ liệu người dùng từ backend.`,
    }));
  }

  private getTodayString(): string {
    return new Date().toISOString().slice(0, 10);
  }
}



