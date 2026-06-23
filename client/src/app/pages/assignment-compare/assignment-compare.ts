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
  message = 'Đang tải dữ liệu từ backend API...';

  developerLoads: DeveloperLoad[] = [];
  compareResults: CompareResult[] = [];
  aiAssignments: AiAssignment[] = [];

  constructor(
    private lookupService: LookupService,
    private taskService: TaskService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
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
        this.message = 'Đã tải dữ liệu từ backend API.';
        window.setTimeout(() => (this.message = ''), 2500);
      },
      error: () => {
        this.message = 'Chưa tải được dữ liệu API. Kiểm tra backend port 49261 rồi thử lại.';
        this.recalculate();
      },
    });
  }

  recalculate(): void {
    const projectTasks = this.getProjectTasks();
    const developers = this.users.length ? this.users : this.getFallbackUsers();
    const totalNewHours = this.newTaskCount * this.hoursPerTask;

    this.developerLoads = developers.map((user) => {
      const assignedTasks = projectTasks.filter((task) => task.assigneeId === user.id || task.assignee === user.hoTen);
      const currentHours = assignedTasks.reduce((sum, task) => sum + (task.estimatedHours ?? 8), 0);
      const currentRisk = assignedTasks.length
        ? Math.round(assignedTasks.reduce((sum, task) => sum + task.riskScore, 0) / assignedTasks.length)
        : 0;

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
        predictedDelayDays: Math.ceil(traditionalRisk / 18),
        description: 'PM giao theo cảm tính hoặc người quen xử lý, dễ dồn việc cho một vài lập trình viên.',
      },
      {
        method: 'Gợi ý giao task bằng AI',
        totalHours: totalNewHours,
        averageLoad: this.getAverageLoad(totalNewHours, true),
        riskScore: aiRisk,
        predictedDelayDays: Math.ceil(aiRisk / 28),
        description: 'AI xét tải hiện tại, số giờ còn lại và rủi ro để chia việc cân bằng hơn.',
      },
    ];

    this.aiAssignments = this.buildAiAssignments();
  }

  get selectedProjectName(): string {
    return this.projects.find((project) => project.id === Number(this.selectedProjectId))?.name ?? 'Dự án demo';
  }

  private getProjectTasks(): Task[] {
    if (!this.selectedProjectId) return this.tasks;
    return this.tasks.filter((task) => task.projectId === Number(this.selectedProjectId));
  }

  private calculateTraditionalRisk(totalNewHours: number): number {
    const maxLoad = Math.max(0, ...this.developerLoads.map((developer) => developer.currentHours));
    const urgentBonus = Math.round(this.urgentPercent * 0.45);
    return Math.min(95, Math.round(maxLoad / 2 + totalNewHours / 4 + urgentBonus));
  }

  private calculateAiRisk(totalNewHours: number): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const balancedHours = totalNewHours / activeDevelopers;
    const avgCurrentRisk = this.developerLoads.length
      ? this.developerLoads.reduce((sum, developer) => sum + developer.currentRisk, 0) / this.developerLoads.length
      : 20;

    return Math.max(5, Math.min(80, Math.round(avgCurrentRisk * 0.45 + balancedHours * 1.2 + this.urgentPercent * 0.18)));
  }

  private getAverageLoad(totalNewHours: number, isAi: boolean): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const currentHours = this.developerLoads.reduce((sum, developer) => sum + developer.currentHours, 0);
    const adjustedNewHours = isAi ? totalNewHours * 0.82 : totalNewHours;
    return Math.round((currentHours + adjustedNewHours) / activeDevelopers);
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

  private getFallbackUsers(): UserLookupDto[] {
    return [
      { id: 1, hoTen: 'Lập trình viên A', email: '', vaiTro: 'Lập trình viên' },
      { id: 2, hoTen: 'Lập trình viên B', email: '', vaiTro: 'Lập trình viên' },
      { id: 3, hoTen: 'Lập trình viên C', email: '', vaiTro: 'Lập trình viên' },
    ];
  }
}
