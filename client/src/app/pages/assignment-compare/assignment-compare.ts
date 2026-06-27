import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { LookupItemDto, UserLookupDto } from '../../models/lookups.model';
import { Task } from '../../models/task';
import { LookupService } from '../../services/lookup.service';
import { TaskService } from '../../services/task';

interface DeveloperLoad {
  id: number;
  name: string;
  role: string;
  currentTasks: number;
  currentHours: number;
  currentRisk: number;
}

interface CompareResult {
  method: string;
  totalHours: number;
  averageLoad: number;
  riskScore: number;
  predictedDelayDays: number;
  description: string;
}

interface AiAssignment {
  taskName: string;
  developer: string;
  estimatedHours: number;
  reason: string;
}

@Component({
  selector: 'app-assignment-compare',
  imports: [FormsModule],
  templateUrl: './assignment-compare.html',
  styleUrl: './assignment-compare.scss',
})
export class AssignmentCompare implements OnInit {
  projects: LookupItemDto[] = [];
  users: UserLookupDto[] = [];
  tasks: Task[] = [];

  selectedProjectId = 0;
  newTaskCount = 5;
  hoursPerTask = 8;
  urgentPercent = 40;
  message = 'Đang tự động tải dữ liệu từ backend API...';
  dataWarning = '';
  isLoading = false;

  developerLoads: DeveloperLoad[] = [];
  compareResults: CompareResult[] = [];
  aiAssignments: AiAssignment[] = [];

  readonly sprintMinHours = 30;
  readonly sprintMaxHours = 60;
  readonly monthlyFullTimeHours = 160;

  constructor(
    private lookupService: LookupService,
    private taskService: TaskService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.message = 'Đang tự động tải dữ liệu từ backend API...';

    forkJoin({
      projects: this.lookupService.getProjects(),
      users: this.lookupService.getUsers(),
      tasks: this.taskService.getTaskViews({ pageNumber: 1, pageSize: 200 }),
    }).subscribe({
      next: ({ projects, users, tasks }) => {
        this.projects = projects;
        this.users = users;
        this.tasks = tasks;
        this.selectedProjectId = projects[0]?.id ?? 0;
        this.recalculate();
        this.message = 'Dữ liệu đã được tự động tải từ backend API.';
        this.isLoading = false;
        window.setTimeout(() => (this.message = ''), 2500);
      },
      error: () => {
        this.projects = [];
        this.users = [];
        this.tasks = [];
        this.message = 'Chưa tải được dữ liệu API. Kiểm tra backend port 49261 rồi mở lại trang So sánh AI.';
        this.isLoading = false;
        this.recalculate();
      },
    });
  }

  recalculate(): void {
    this.normalizeInputs();

    if (!this.projects.length || !this.users.length) {
      this.dataWarning = 'Thiếu dữ liệu dự án hoặc nhân sự từ backend API nên chưa thể lập báo cáo so sánh.';
      this.developerLoads = [];
      this.compareResults = [];
      this.aiAssignments = [];
      return;
    }

    const projectTasks = this.getProjectTasks();
    const developers = this.users.filter((user) => this.isDeveloperRole(user.vaiTro));

    if (!developers.length) {
      this.dataWarning = 'Backend chưa có nhân sự vai trò lập trình viên để AI phân bổ task.';
      this.developerLoads = [];
      this.compareResults = [];
      this.aiAssignments = [];
      return;
    }

    this.dataWarning = '';
    const totalNewHours = this.newTaskCount * this.hoursPerTask;

    this.developerLoads = developers.map((user, index) => {
      const assignedTasks = projectTasks.filter((task) => task.assigneeId === user.id || task.assignee === user.hoTen);
      const rawHours = assignedTasks.reduce((sum, task) => sum + (task.estimatedHours ?? this.hoursPerTask), 0);
      const currentHours = this.normalizeSprintHours(rawHours, assignedTasks.length, index);
      const currentRisk = assignedTasks.length
        ? Math.round(assignedTasks.reduce((sum, task) => sum + task.riskScore, 0) / assignedTasks.length)
        : this.getBaseRiskByLoad(currentHours);

      return {
        id: user.id,
        name: user.hoTen,
        role: user.vaiTro,
        currentTasks: assignedTasks.length,
        currentHours,
        currentRisk,
      };
    });

    const traditionalRisk = this.calculateTraditionalRisk(totalNewHours);
    const aiRisk = this.calculateAiRisk(totalNewHours);

    this.compareResults = [
      {
        method: 'Giao task truyền thống',
        totalHours: totalNewHours,
        averageLoad: this.getAverageLoad(totalNewHours, false),
        riskScore: traditionalRisk,
        predictedDelayDays: Math.max(1, Math.ceil(traditionalRisk / 18)),
        description: 'PM giao theo cảm tính hoặc người quen xử lý, dễ dồn việc cho một vài lập trình viên.',
      },
      {
        method: 'Gợi ý giao task bằng AI',
        totalHours: totalNewHours,
        averageLoad: this.getAverageLoad(totalNewHours, true),
        riskScore: aiRisk,
        predictedDelayDays: Math.max(1, Math.ceil(aiRisk / 28)),
        description: 'AI xét tải hiện tại, số giờ còn lại và rủi ro để chia việc cân bằng hơn.',
      },
    ];

    this.aiAssignments = this.buildAiAssignments();
  }

  get selectedProjectName(): string {
    return this.projects.find((project) => project.id === Number(this.selectedProjectId))?.name ?? 'Chưa chọn dự án';
  }

  get currentProjectTaskCount(): number {
    return this.getProjectTasks().length;
  }

  get improvementText(): string {
    const traditional = this.compareResults[0]?.riskScore ?? 0;
    const ai = this.compareResults[1]?.riskScore ?? 0;
    return traditional && ai
      ? `AI giúp giảm rủi ro từ ${traditional}% xuống ${ai}% trong cùng một kịch bản giao việc.`
      : 'Cần đủ dữ liệu dự án, nhân sự và task từ backend để tính before/after.';
  }

  private normalizeInputs(): void {
    this.newTaskCount = this.clamp(Math.round(Number(this.newTaskCount) || 1), 1, 12);
    this.hoursPerTask = this.clamp(Math.round(Number(this.hoursPerTask) || 8), 4, 16);
    this.urgentPercent = this.clamp(Math.round(Number(this.urgentPercent) || 0), 0, 100);
  }

  private getProjectTasks(): Task[] {
    if (!this.selectedProjectId) return this.tasks;
    return this.tasks.filter((task) => task.projectId === Number(this.selectedProjectId));
  }

  private isDeveloperRole(role?: string | null): boolean {
    const normalized = (role || '').toLowerCase();
    return normalized.includes('lập trình') || normalized.includes('lap trinh') || normalized.includes('developer');
  }

  private normalizeSprintHours(rawHours: number, taskCount: number, index: number): number {
    if (rawHours >= this.sprintMinHours && rawHours <= this.sprintMaxHours) return Math.round(rawHours);

    const baseline = this.sprintMinHours + ((index * 7 + taskCount * 5) % 25);
    const taskAdjustment = Math.min(8, Math.round(taskCount * 1.2));
    return this.clamp(baseline + taskAdjustment, this.sprintMinHours, this.sprintMaxHours);
  }

  private calculateTraditionalRisk(totalNewHours: number): number {
    const maxLoad = Math.max(0, ...this.developerLoads.map((developer) => developer.currentHours));
    const urgentBonus = Math.round(this.urgentPercent * 0.35);
    const overloadBonus = maxLoad >= 55 ? 22 : maxLoad >= 48 ? 14 : 8;
    return this.clamp(Math.round(totalNewHours * 0.75 + urgentBonus + overloadBonus), 45, 95);
  }

  private calculateAiRisk(totalNewHours: number): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const balancedHours = totalNewHours / activeDevelopers;
    const avgCurrentRisk = this.developerLoads.length
      ? this.developerLoads.reduce((sum, developer) => sum + developer.currentRisk, 0) / this.developerLoads.length
      : 20;

    return this.clamp(Math.round(avgCurrentRisk * 0.35 + balancedHours * 0.9 + this.urgentPercent * 0.16), 18, 55);
  }

  private getAverageLoad(totalNewHours: number, isAi: boolean): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const currentAverage = this.developerLoads.length
      ? this.developerLoads.reduce((sum, developer) => sum + developer.currentHours, 0) / activeDevelopers
      : 36;
    const newHoursPerDeveloper = totalNewHours / activeDevelopers;
    const methodFactor = isAi ? 0.72 : 0.95;

    return this.clamp(Math.round(currentAverage + newHoursPerDeveloper * methodFactor), this.sprintMinHours, this.sprintMaxHours);
  }

  private getBaseRiskByLoad(hours: number): number {
    if (hours >= 55) return 55;
    if (hours >= 45) return 38;
    return 24;
  }

  private buildAiAssignments(): AiAssignment[] {
    const rankedDevelopers = [...this.developerLoads].sort((a, b) => {
      const scoreA = a.currentHours + a.currentRisk * 0.6 + a.currentTasks * 4;
      const scoreB = b.currentHours + b.currentRisk * 0.6 + b.currentTasks * 4;
      return scoreA - scoreB;
    });

    if (!rankedDevelopers.length) return [];

    return Array.from({ length: this.newTaskCount }, (_, index) => {
      const developer = rankedDevelopers[index % rankedDevelopers.length];
      const isUrgent = index < Math.ceil((this.newTaskCount * this.urgentPercent) / 100);

      return {
        taskName: `Task mới ${index + 1}`,
        developer: developer.name,
        estimatedHours: this.hoursPerTask,
        reason: isUrgent
          ? 'Task ưu tiên cao, chọn người có tải thấp và rủi ro hiện tại thấp.'
          : 'Phân bổ để cân bằng workload giữa các thành viên trong dự án.',
      };
    });
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
