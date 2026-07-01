import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, switchMap, timeout } from 'rxjs/operators';
import { RiskPredictionResult, StaffMatchResult } from '../../models/ai';
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
import { AiService } from '../../services/ai';
import { TaskService } from '../../services/task';

interface DeveloperSuggestion {
  id: number;
  name: string;
  score: number;
  workload: number;
  skillScore?: number | null;
  experienceScore?: number | null;
  model?: string;
  reason: string;
  isAssigned?: boolean;
}

@Component({
  selector: 'app-tasks',
  imports: [FormsModule],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks implements OnInit {
  tasks: Task[] = [];
  filteredTasks: Task[] = [];
  visibleTasks: Task[] = [];
  selectedProjectFilterId: number | 'All' = 'All';
  currentPage = 1;
  pageSize = 10;
  readonly pageSizeOptions = [10, 20, 50];
  totalTasks = 0;
  totalPages = 1;
  hasPreviousPage = false;
  hasNextPage = false;
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

  get emptyTaskMessage(): string {
    if (this.tasks.length && !this.filteredTasks.length) {
      return 'Không có task nào khớp với bộ lọc hiện tại.';
    }

    return this.taskDataMessage;
  }

  isAddTaskOpen = false;

  newTask: Omit<Task, 'id'> = this.createEmptyTask();
  suggestedDevelopers: DeveloperSuggestion[] = [];
  riskResult: RiskPredictionResult | null = null;
  aiSuggestionMessage = '';
  isSuggestionLoading = false;
  private assignedDeveloperByTask = new Map<number, number>();

  constructor(
    private lookupService: LookupService,
    private aiService: AiService,
    private taskService: TaskService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadLookups();
    this.loadTasks();
  }

  get startItemIndex(): number {
    return this.totalTasks ? (this.currentPage - 1) * this.pageSize + 1 : 0;
  }

  get endItemIndex(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalTasks);
  }

  get visiblePages(): number[] {
    const maxVisible = 5;
    const half = Math.floor(maxVisible / 2);
    let start = Math.max(1, this.currentPage - half);
    let end = Math.min(this.totalPages, start + maxVisible - 1);
    start = Math.max(1, end - maxVisible + 1);

    return Array.from({ length: Math.max(end - start + 1, 0) }, (_, index) => start + index);
  }

  onTaskFiltersChanged(): void {
    this.currentPage = 1;
    this.loadTasks();
  }

  onPageSizeChanged(): void {
    this.currentPage = 1;
    this.loadTasks();
  }

  goToPage(page: number): void {
    const nextPage = Math.min(Math.max(page, 1), this.totalPages);
    if (nextPage === this.currentPage) return;
    this.currentPage = nextPage;
    this.loadTasks();
  }

  previousPage(): void {
    if (!this.hasPreviousPage) return;
    this.goToPage(this.currentPage - 1);
  }

  nextPage(): void {
    if (!this.hasNextPage) return;
    this.goToPage(this.currentPage + 1);
  }

  private applyTaskFilters(): void {
    this.filteredTasks = this.tasks;
    this.visibleTasks = this.tasks;
  }

  loadTasks(): void {
    this.taskDataMessage = 'Đang tải danh sách task từ backend...';

    const params = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchText.trim() || undefined,
      projectId: this.selectedProjectFilterId === 'All' ? undefined : this.selectedProjectFilterId,
      status: this.statusFilter === 'All' ? undefined : this.statusFilter,
      priority: this.priorityFilter === 'All' ? undefined : this.priorityFilter,
    };

    this.taskService.getTaskViewsPage(params).subscribe({
      next: (page) => {
        this.tasks = page.items;
        this.currentPage = page.pageNumber || this.currentPage;
        this.pageSize = page.pageSize || this.pageSize;
        this.totalTasks = page.totalItems ?? page.totalCount ?? page.items.length;
        this.totalPages = Math.max(page.totalPages || Math.ceil(this.totalTasks / this.pageSize), 1);
        this.hasPreviousPage = page.hasPreviousPage ?? this.currentPage > 1;
        this.hasNextPage = page.hasNextPage ?? this.currentPage < this.totalPages;

        if (this.currentPage > this.totalPages) {
          this.currentPage = this.totalPages;
          this.loadTasks();
          return;
        }

        this.applyTaskFilters();
        this.taskDataMessage = page.items.length
          ? ''
          : 'Không có task nào trong trang hoặc bộ lọc hiện tại.';
        this.cdr.detectChanges();
      },
      error: () => {
        this.tasks = [];
        this.totalTasks = 0;
        this.totalPages = 1;
        this.hasPreviousPage = false;
        this.hasNextPage = false;
        this.applyTaskFilters();
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

  formatSuggestionReason(reason: string): string {
    return reason || 'AI chưa trả lý do chi tiết.';
  }

  getAssigneeDisplayName(task: Task): string {
    if (task.assigneeId) {
      const user = this.users.find((item) => item.id === task.assigneeId);
      if (user?.hoTen) return user.hoTen;
    }

    return task.assignee || 'Chưa phân công';
  }

  getCreatorDisplayName(task: Task): string {
    if (task.creatorId) {
      const user = this.users.find((item) => item.id === task.creatorId);
      if (user?.hoTen) return user.hoTen;
    }

    return task.creator || 'Chưa rõ';
  }

  getProjectShortLabel(task: Task): string {
    return [task.project, task.sprint].filter(Boolean).join(' · ');
  }

  getDeadlineDate(value?: string | null): string {
    if (!value) return '--';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleDateString('vi-VN');
  }

  getDeadlineTime(value?: string | null): string {
    if (!value) return '--';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
  }

  isOverdue(task: Task): boolean {
    if (!task.deadline) return false;
    const deadline = new Date(task.deadline);
    return !Number.isNaN(deadline.getTime()) && deadline.getTime() < Date.now() && !/hoan thanh|done|complete/i.test(task.status ?? '');
  }
  onProjectChange(projectName: string): void {
    const selectedProject = this.projects.find((project) => project.name === projectName);

    this.filteredSprintsForForm = selectedProject
      ? this.sprints.filter((sprint) => sprint.maDuAn === selectedProject.id)
      : this.sprints;

    this.newTask.projectId = selectedProject?.id ?? null;
    this.newTask.sprintId = null;
    this.newTask.sprint = '';
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
    if (task.assigneeId) {
      this.assignedDeveloperByTask.set(task.id, task.assigneeId);
    }
    this.riskResult = null;
    this.suggestedDevelopers = [];
    this.aiSuggestionMessage = 'AI đang phân tích năng lực và dự báo rủi ro...';
    this.isSuggestionLoading = true;
    this.cdr.detectChanges();

    forkJoin({
      suggestions: this.aiService
        .suggestAssignees(task.id)
        .pipe(catchError(() => of([] as StaffMatchResult[]))),
      risk: this.aiService
        .predictRisk(task.id)
        .pipe(catchError(() => of(null as RiskPredictionResult | null))),
    }).subscribe({
      next: ({ suggestions, risk }) => {
        this.suggestedDevelopers = suggestions.length
          ? suggestions.map((item) => this.mapStaffMatchToSuggestion(item))
          : this.buildDeveloperSuggestions();
        this.aiSuggestionMessage = suggestions.length
          ? ''
          : 'AI chưa tìm được nhân sự phù hợp cho task này.';
        this.riskResult = risk;
        this.isSuggestionLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.suggestedDevelopers = this.buildDeveloperSuggestions();
        this.aiSuggestionMessage =
          'AI server chưa xử lý được yêu cầu này. Đang hiển thị gợi ý dự phòng.';
        this.isSuggestionLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  closeSuggestion(): void {
    this.suggestionTask = null;
    this.riskResult = null;
    this.aiSuggestionMessage = '';
    this.isSuggestionLoading = false;
    this.cdr.detectChanges();
  }

  isDeveloperAssigned(developer: DeveloperSuggestion): boolean {
    if (!this.suggestionTask) return false;

    const assignedId =
      this.assignedDeveloperByTask.get(this.suggestionTask.id) ?? this.suggestionTask.assigneeId;

    return !!developer.isAssigned || (!!assignedId && assignedId === developer.id);
  }

  getAssignButtonLabel(developer: DeveloperSuggestion): string {
    if (this.isDeveloperAssigned(developer)) return 'Đã chọn';
    return this.isSuggestionLoading ? 'Đang xử lý' : 'Chọn';
  }

  assignDeveloper(developer: DeveloperSuggestion): void {
    if (!this.suggestionTask || this.isDeveloperAssigned(developer)) return;

    const taskId = this.suggestionTask.id;
    const taskName = this.suggestionTask.name;

    this.aiSuggestionMessage = `Đang gán task cho ${developer.name}...`;
    this.isSuggestionLoading = true;
    this.cdr.detectChanges();

    this.taskService
      .assignTask(taskId, { maNguoiPhuTrach: developer.id })
      .pipe(
        switchMap(() => {
          // Xóa cache để backend tính lại PhanTramTai/diemKhoiLuong sau khi giao việc
          this.aiService.clearSuggestAssigneesCache(taskId);
          return forkJoin({
            suggestions: this.aiService
              .suggestAssignees(taskId)
              .pipe(catchError(() => of([] as StaffMatchResult[]))),
            risk: this.aiService
              .predictRisk(taskId)
              .pipe(catchError(() => of(null as RiskPredictionResult | null))),
          });
        })
      )
      .subscribe({
        next: ({ suggestions, risk }) => {
          this.successMessage = `Đã gán task "${taskName}" cho ${developer.name}.`;
          this.assignedDeveloperByTask.set(taskId, developer.id);
          if (this.suggestionTask?.id === taskId) {
            this.suggestionTask = {
              ...this.suggestionTask,
              assigneeId: developer.id,
              assignee: developer.name,
            };
          }
          this.suggestedDevelopers = suggestions.length
            ? suggestions.map((item) => this.mapStaffMatchToSuggestion(item))
            : this.suggestedDevelopers;
          this.riskResult = risk;
          this.aiSuggestionMessage = suggestions.length
            ? ''
            : 'Đã gán task. Chưa tải lại được gợi ý AI mới, đang giữ danh sách hiện tại.';
          this.isSuggestionLoading = false;
          this.loadTasks();
          this.cdr.detectChanges();
        },
        error: () => {
          this.aiSuggestionMessage = 'Chưa phân công được task. Kiểm tra backend rồi thử lại.';
          this.isSuggestionLoading = false;
          this.cdr.detectChanges();
        },
      });
  }

  openAddTask(): void {
    this.formMessage = '';
    this.newTask = this.createEmptyTask();
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
    const creatorId = this.getCurrentUserId(assignee?.id);

    return {
      maDuAn: project?.id ?? 1,
      maSprint: sprint?.id ?? null,
      maCongViecCode: null,
      tenCongViec: this.newTask.name.trim(),
      moTa: this.newTask.description ?? null,
      maNguoiTao: creatorId,
      maNguoiPhuTrach: assignee?.id ?? null,
      doUuTien: this.newTask.priority,
      trangThai: this.newTask.status,
      ngayBatDau: this.newTask.startDate || this.getTodayString(),
      hanChot: this.newTask.deadline,
      soGioUocTinh: this.newTask.estimatedHours ?? 8,
      tienDo: this.newTask.progress ?? 0,
    };
  }

  private getCurrentUserId(fallbackUserId?: number | null): number {
    const storedUserId = Number(localStorage.getItem('userId') ?? localStorage.getItem('maNguoiDung'));

    if (Number.isInteger(storedUserId) && storedUserId > 0) {
      return storedUserId;
    }

    return fallbackUserId ?? 1;
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
      isAssigned: false,
    }));
  }

  private mapStaffMatchToSuggestion(item: StaffMatchResult): DeveloperSuggestion {
    return {
      id: item.maNguoiDuocDeXuat,
      name: item.hoTenNguoiDuocDeXuat,
      score: this.toPercent(item.diemPhuHop),
      workload: this.toPercent(item.diemKhoiLuong ?? 0),
      skillScore: item.diemKyNang ?? null,
      experienceScore: item.diemKinhNghiem ?? null,
      model: item.tenMoHinh,
      reason: this.formatSuggestionReason(item.lyDo || 'AI đề xuất dựa trên kỹ năng, mức độ còn rảnh và kinh nghiệm lịch sử.'),
      isAssigned: !!item.daChapNhan,
    };
  }

  getRiskPercent(item: RiskPredictionResult): number {
    return this.toPercent(item.xacSuatRuiRo);
  }

  private toPercent(value: number): number {
    if (value <= 1) return Math.round(value * 100);
    return Math.round(value);
  }

  private getTodayString(): string {
    return new Date().toISOString().slice(0, 10);
  }
}








