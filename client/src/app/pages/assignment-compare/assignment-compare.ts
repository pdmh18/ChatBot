import { ChangeDetectorRef, Component, OnInit, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, forkJoin, from, map, mergeMap, of } from 'rxjs';
import { StaffMatchResult } from '../../models/ai';
import { LookupItemDto, UserLookupDto } from '../../models/lookups.model';
import { Task } from '../../models/task';
import { LookupService } from '../../services/lookup.service';
import { AiService } from '../../services/ai';
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
  id: 'traditional' | 'ai';
  method: string;
  subtitle: string;
  totalHours: number;
  averageLoad: number;
  riskScore: number;
  predictedDelayDays: number;
  balanceScore: number;
  description: string;
}

interface WorkloadBar {
  id: number;
  name: string;
  role: string;
  currentHours: number;
  projectedHours: number;
  percent: number;
  statusLabel: string;
  statusClass: 'low' | 'medium' | 'high' | 'danger';
}

interface AiAssignment {
  taskName: string;
  developer: string;
  developerRole: string;
  estimatedHours: number;
  riskAfterAssign: number;
  confidence: number;
  workloadBefore: number;
  workloadAfter: number;
  reason: string;
  factors: string[];
  matchScore?: number;
  modelName?: string;
  aiReason?: string | null;
  skillScore?: number | null;
  workloadScore?: number | null;
  experienceScore?: number | null;
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
  message = 'Đang tải dữ liệu nền từ backend API...';
  dataWarning = '';
  isLoading = false;
  isAiSuggestionLoading = false;
  selectedAssignment: AiAssignment | null = null;
  hasLoadedInitialData = false;
  hasLoadedAiSuggestions = false;

  developerLoads: DeveloperLoad[] = [];
  compareResults: CompareResult[] = [];
  aiAssignments: AiAssignment[] = [];
  traditionalWorkloads: WorkloadBar[] = [];
  aiWorkloads: WorkloadBar[] = [];

  readonly sprintMinHours = 30;
  readonly sprintMaxHours = 60;
  readonly monthlyFullTimeHours = 160;

  private suggestionDebounceHandle: ReturnType<typeof setTimeout> | null = null;
  private suggestionRequestSeq = 0;

  constructor(
    private lookupService: LookupService,
    private aiService: AiService,
    private taskService: TaskService,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
  ) {}

  ngOnInit(): void {
    // Vừa mở trang là tự tải toàn bộ dữ liệu nền từ API.
    // Người dùng không cần kéo slider "Số task mới" thì dữ liệu mới hiện.
    this.loadData();
  }


  onScenarioChanged(): void {
    // Slider/chọn dự án chỉ mô phỏng lại kịch bản.
    // Không gọi API AI tự động để tránh chậm và tránh request liên tục.
    this.recalculate();
    this.hasLoadedAiSuggestions = false;
    this.isAiSuggestionLoading = false;
    this.message = 'Đã cập nhật kịch bản mô phỏng. Bấm “Phân tích bằng AI” để tải gợi ý giao task từ API.';
    this.refreshUi();
  }

  loadData(): void {
    this.isLoading = true;
    this.hasLoadedInitialData = false;
    this.message = 'Đang tự động tải dữ liệu nền từ backend API...';

    // Projects, Users, Tasks được gọi song song và có cache ở service.
    // Khi dữ liệu nền xong, UI render ngay; AI suggestions chạy tiếp ở nền.
    forkJoin({
      projects: this.lookupService.getProjects(),
      users: this.lookupService.getUsers(),
      tasks: this.taskService.getTaskViews({ pageNumber: 1, pageSize: 300 }),
    }).subscribe({
      next: ({ projects, users, tasks }) => {
        this.projects = projects;
        this.users = users;
        this.tasks = tasks;
        this.selectedProjectId = this.selectedProjectId || projects[0]?.id || 0;
        this.hasLoadedInitialData = true;

        // Render dữ liệu nền ngay lập tức: KPI, workload, bảng nhân sự.
        this.recalculate();
        this.isLoading = false;
        this.message = 'Đã tải dữ liệu nền. Bấm “Phân tích bằng AI” để tải gợi ý giao task.';
        this.refreshUi();
      },
      error: () => {
        this.projects = [];
        this.users = [];
        this.tasks = [];
        this.hasLoadedInitialData = false;
        this.message = 'Chưa tải được dữ liệu API. Kiểm tra backend port 49261 rồi mở lại trang So sánh AI.';
        this.isLoading = false;
        this.recalculate();
        this.refreshUi();
      },
    });
  }

  recalculate(): void {
    this.normalizeInputs();
    this.selectedAssignment = null;

    if (!this.projects.length || !this.users.length) {
      this.dataWarning = 'Thiếu dữ liệu dự án hoặc nhân sự từ backend API nên chưa thể lập báo cáo so sánh.';
      this.resetComputedData();
      this.refreshUi();
      return;
    }

    const projectTasks = this.getProjectTasks();
    const developers = this.users.filter((user) => this.isDeveloperRole(user.vaiTro));

    if (!developers.length) {
      this.dataWarning = 'Backend chưa có nhân sự vai trò lập trình viên để AI phân bổ task.';
      this.resetComputedData();
      this.refreshUi();
      return;
    }

    this.dataWarning = '';
    const totalNewHours = this.newTaskCount * this.hoursPerTask;

    this.developerLoads = developers.map((user) => {
      const assignedTasks = projectTasks.filter((task) => task.assigneeId === user.id || task.assignee === user.hoTen);
      // Dùng trực tiếp tổng giờ ước tính từ backend, không normalize giả tạo
      const currentHours = this.clamp(
        assignedTasks.reduce((sum, task) => sum + (task.estimatedHours ?? this.hoursPerTask), 0),
        0,
        this.sprintMaxHours * 2
      );
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

    this.traditionalWorkloads = this.buildWorkloads('traditional');
    this.aiWorkloads = this.buildWorkloads('ai');

    const traditionalRisk = this.calculateTraditionalRisk(totalNewHours);
    const aiRisk = this.calculateAiRisk(totalNewHours);

    this.compareResults = [
      {
        id: 'traditional',
        method: 'Phân công truyền thống',
        subtitle: 'PM tự chia theo kinh nghiệm',
        totalHours: totalNewHours,
        averageLoad: this.getAverageLoad(totalNewHours, false),
        riskScore: traditionalRisk,
        predictedDelayDays: Math.max(1, Math.ceil(traditionalRisk / 18)),
        balanceScore: this.getBalanceScore(this.traditionalWorkloads),
        description: 'Dễ dồn việc vào một vài người quen xử lý, tạo chênh lệch tải và tăng nguy cơ trễ deadline.',
      },
      {
        id: 'ai',
        method: 'AI tối ưu nguồn lực',
        subtitle: 'AI xét workload, risk và sức chứa sprint',
        totalHours: totalNewHours,
        averageLoad: this.getAverageLoad(totalNewHours, true),
        riskScore: aiRisk,
        predictedDelayDays: Math.max(1, Math.ceil(aiRisk / 28)),
        balanceScore: this.getBalanceScore(this.aiWorkloads),
        description: 'AI phân bổ đều hơn dựa trên tải hiện tại, rủi ro trung bình và số giờ còn lại của từng lập trình viên.',
      },
    ];

    if (!this.isAiSuggestionLoading) {
      this.aiAssignments = [];
    }

    this.refreshUi();
  }


  get displayedTraditionalWorkloads(): WorkloadBar[] {
    return this.getCompactWorkloads(this.traditionalWorkloads);
  }

  get displayedAiWorkloads(): WorkloadBar[] {
    return this.getCompactWorkloads(this.aiWorkloads);
  }

  get hiddenTraditionalWorkloadCount(): number {
    return Math.max(0, this.traditionalWorkloads.length - this.displayedTraditionalWorkloads.length);
  }

  get hiddenAiWorkloadCount(): number {
    return Math.max(0, this.aiWorkloads.length - this.displayedAiWorkloads.length);
  }


  get displayedDeveloperLoads(): DeveloperLoad[] {
    return this.getTopDevelopers(this.developerLoads);
  }

  get hiddenDeveloperCount(): number {
    return Math.max(0, this.developerLoads.length - this.displayedDeveloperLoads.length);
  }

  get selectedProjectName(): string {
    return this.projects.find((project) => project.id === Number(this.selectedProjectId))?.name ?? 'Chưa chọn dự án';
  }

  get currentProjectTaskCount(): number {
    return this.getProjectTasks().length;
  }

  get traditionalResult(): CompareResult | undefined {
    return this.compareResults.find((result) => result.id === 'traditional');
  }

  get aiResult(): CompareResult | undefined {
    return this.compareResults.find((result) => result.id === 'ai');
  }

  get improvementText(): string {
    const traditional = this.traditionalResult?.riskScore ?? 0;
    const ai = this.aiResult?.riskScore ?? 0;
    const savedDays = Math.max(0, (this.traditionalResult?.predictedDelayDays ?? 0) - (this.aiResult?.predictedDelayDays ?? 0));

    return traditional && ai
      ? `AI giúp giảm rủi ro từ ${traditional}% xuống ${ai}% và giảm khoảng ${savedDays} ngày trễ dự báo trong cùng một kịch bản giao việc.`
      : 'Cần đủ dữ liệu dự án, nhân sự và task từ backend để tính before/after.';
  }

  get riskReduction(): number {
    const traditional = this.traditionalResult?.riskScore ?? 0;
    const ai = this.aiResult?.riskScore ?? 0;
    return Math.max(0, traditional - ai);
  }

  get delaySaved(): number {
    return Math.max(0, (this.traditionalResult?.predictedDelayDays ?? 0) - (this.aiResult?.predictedDelayDays ?? 0));
  }

  loadAiAnalysis(): void {
    if (this.isLoading || this.isAiSuggestionLoading) return;

    if (!this.hasLoadedInitialData) {
      this.message = 'Dữ liệu nền chưa sẵn sàng. Vui lòng chờ hệ thống tải xong Projects, Tasks và Nhân sự.';
      return;
    }

    this.message = 'Đang phân tích AI và tải gợi ý giao task từ API...';
    this.hasLoadedAiSuggestions = true;
    this.refreshUi();
    this.loadAiAssignmentSuggestions();
  }

  openExplanation(assignment?: AiAssignment): void {
    this.selectedAssignment = assignment ?? this.aiAssignments[0] ?? null;
    this.refreshUi();
  }

  closeExplanation(): void {
    this.selectedAssignment = null;
    this.refreshUi();
  }

  applyAiPlan(): void {
    this.message = 'Đã chọn phương án phân công AI trong bản demo. Khi tích hợp nghiệp vụ thật, nút này sẽ gọi API cập nhật người phụ trách task.';
    window.setTimeout(() => (this.message = ''), 3500);
  }

  getRiskLevel(score: number): string {
    if (score >= 70) return 'Cao';
    if (score >= 45) return 'Trung bình';
    return 'Thấp';
  }

  getRiskClass(score: number): 'low' | 'medium' | 'high' | 'danger' {
    if (score >= 75) return 'danger';
    if (score >= 55) return 'high';
    if (score >= 35) return 'medium';
    return 'low';
  }

  getGaugeBackground(score: number): string {
    const safeScore = this.clamp(Math.round(score), 0, 100);
    const color = safeScore >= 75 ? '#dc2626' : safeScore >= 55 ? '#f97316' : safeScore >= 35 ? '#f59e0b' : '#16a34a';
    return `conic-gradient(${color} 0deg ${safeScore * 1.8}deg, #e5e7eb ${safeScore * 1.8}deg 180deg, transparent 180deg 360deg)`;
  }

  getWorkloadWidth(percent: number): string {
    return `${this.clamp(Math.round(percent), 4, 100)}%`;
  }

  trackByAssignment(index: number, assignment: AiAssignment): string {
    return `${assignment.taskName}-${assignment.developer}-${index}`;
  }


  private queueAiAssignmentSuggestions(delayMs = 350): void {
    if (this.suggestionDebounceHandle) {
      clearTimeout(this.suggestionDebounceHandle);
    }

    this.suggestionDebounceHandle = setTimeout(() => {
      this.loadAiAssignmentSuggestions();
    }, delayMs);
  }

  private loadAiAssignmentSuggestions(): void {
    const tasksForSuggestion = this.getTasksForAiSuggestion();
    const requestSeq = ++this.suggestionRequestSeq;

    if (!tasksForSuggestion.length || !this.developerLoads.length) {
      this.aiAssignments = [];
      this.isAiSuggestionLoading = false;
      this.hasLoadedAiSuggestions = true;
      this.message = 'Chưa có đủ task hoặc nhân sự để AI đề xuất giao việc.';
      this.cdr.detectChanges();
      return;
    }

    // Không chờ toàn bộ API xong mới render. Kết quả nào về trước sẽ hiện trước.
    // Giới hạn 3 request song song để backend/AI server không bị nghẽn.
    this.aiAssignments = [];
    this.isAiSuggestionLoading = true;

    from(tasksForSuggestion)
      .pipe(
        mergeMap(
          (task) =>
            this.aiService.suggestAssignees(task.id).pipe(
              map((suggestions) => this.mapBestAiSuggestion(task, suggestions)),
              catchError(() => of(null))
            ),
          3
        ),
        finalize(() => {
          if (requestSeq !== this.suggestionRequestSeq) return;

          // Bảo đảm Angular render lại ngay khi API AI chạy xong.
          // Trước đó có trường hợp API trả kết quả nhưng UI chỉ hiện sau khi click sang control khác.
          this.ngZone.run(() => {
            this.isAiSuggestionLoading = false;
            this.hasLoadedAiSuggestions = true;

            if (!this.aiAssignments.length) {
              this.message = 'API AI chưa trả được dữ liệu đề xuất giao task cho dự án này.';
              window.setTimeout(() => {
                this.ngZone.run(() => {
                  this.message = '';
                  this.cdr.detectChanges();
                });
              }, 3500);
            } else {
              this.message = '';
            }

            this.cdr.detectChanges();
          });
        })
      )
      .subscribe((assignment) => {
        if (requestSeq !== this.suggestionRequestSeq || !assignment) return;

        this.ngZone.run(() => {
          // Luôn gán mảng mới để Angular cập nhật @for ngay lập tức.
          this.aiAssignments = [...this.aiAssignments, assignment]
            .sort((a, b) => b.confidence - a.confidence);
          this.hasLoadedAiSuggestions = true;
          this.cdr.detectChanges();
        });
      });
  }

  private getTasksForAiSuggestion(): Task[] {
    return [...this.getProjectTasks()]
      .filter((task) => task.status !== 'Hoan thanh' && task.status !== 'Da huy')
      .sort((a, b) => (b.riskScore ?? 0) - (a.riskScore ?? 0))
      .slice(0, Math.min(this.newTaskCount, 6));
  }

  private mapBestAiSuggestion(task: Task, suggestions: StaffMatchResult[]): AiAssignment | null {
    if (!suggestions?.length) return null;

    const best = [...suggestions].sort((a, b) => Number(b.diemPhuHop ?? 0) - Number(a.diemPhuHop ?? 0))[0];
    const developer = this.developerLoads.find((item) => item.id === best.maNguoiDuocDeXuat);
    const workloadBefore = developer?.currentHours ?? 0;
    const workloadAfter = workloadBefore + (task.estimatedHours ?? this.hoursPerTask);
    const matchScore = this.toPercent(best.diemPhuHop);
    const skillScore = best.diemKyNang == null ? null : this.toPercent(best.diemKyNang);
    const workloadScore = best.diemKhoiLuong == null ? null : this.toPercent(best.diemKhoiLuong);
    const experienceScore = best.diemKinhNghiem == null ? null : this.toPercent(best.diemKinhNghiem);
    const riskAfterAssign = this.clamp(
      Math.round((task.riskScore ?? 35) * 0.45 + (100 - matchScore) * 0.25 + (workloadAfter / this.sprintMaxHours) * 18),
      8,
      75
    );

    const factors = [
      `Điểm phù hợp AI: ${matchScore}%`,
      skillScore == null ? 'Điểm kỹ năng: API chưa trả dữ liệu' : `Điểm kỹ năng: ${skillScore}%`,
      workloadScore == null ? 'Điểm workload: API chưa trả dữ liệu' : `Điểm workload: ${workloadScore}%`,
      experienceScore == null ? 'Điểm kinh nghiệm: API chưa trả dữ liệu' : `Điểm kinh nghiệm: ${experienceScore}%`,
      `Workload trước/sau: ${workloadBefore}h → ${workloadAfter}h`,
    ];

    if (best.lyDo) {
      factors.unshift(best.lyDo);
    }

    return {
      taskName: task.name,
      developer: best.hoTenNguoiDuocDeXuat || developer?.name || `Nhân sự #${best.maNguoiDuocDeXuat}`,
      developerRole: developer?.role ?? 'Nhân sự được AI đề xuất',
      estimatedHours: task.estimatedHours ?? this.hoursPerTask,
      workloadBefore,
      workloadAfter,
      riskAfterAssign,
      confidence: matchScore,
      matchScore,
      modelName: best.tenMoHinh,
      aiReason: best.lyDo,
      skillScore,
      workloadScore,
      experienceScore,
      reason: best.lyDo || 'Đề xuất được lấy trực tiếp từ API AI dựa trên điểm phù hợp nhân sự, workload và kinh nghiệm.',
      factors,
    };
  }

  private toPercent(value: number | string): number {
    const numeric = Number(value) || 0;
    const percent = numeric <= 1 ? numeric * 100 : numeric;
    return this.clamp(Math.round(percent), 0, 100);
  }

  private resetComputedData(): void {
    this.developerLoads = [];
    this.compareResults = [];
    this.aiAssignments = [];
    this.traditionalWorkloads = [];
    this.aiWorkloads = [];
    this.refreshUi();
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

  // normalizeSprintHours đã bị xóa: hàm cũ bỏ qua dữ liệu giờ thực từ backend khi
  // rawHours nằm ngoài [sprintMinHours, sprintMaxHours], thay bằng giá trị giả dựa vào index.
  // Nay developerLoads dùng trực tiếp tổng soGioUocTinh từ backend.

  private calculateTraditionalRisk(totalNewHours: number): number {
    const maxLoad = Math.max(0, ...this.traditionalWorkloads.map((developer) => developer.projectedHours));
    const urgentBonus = Math.round(this.urgentPercent * 0.35);
    const overloadBonus = maxLoad >= 75 ? 26 : maxLoad >= 65 ? 20 : maxLoad >= 55 ? 14 : 8;
    return this.clamp(Math.round(totalNewHours * 0.65 + urgentBonus + overloadBonus), 45, 95);
  }

  private calculateAiRisk(totalNewHours: number): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const balancedHours = totalNewHours / activeDevelopers;
    const avgCurrentRisk = this.developerLoads.length
      ? this.developerLoads.reduce((sum, developer) => sum + developer.currentRisk, 0) / this.developerLoads.length
      : 20;

    return this.clamp(Math.round(avgCurrentRisk * 0.32 + balancedHours * 0.82 + this.urgentPercent * 0.14), 18, 55);
  }

  private getAverageLoad(totalNewHours: number, isAi: boolean): number {
    const activeDevelopers = Math.max(1, this.developerLoads.length);
    const currentAverage = this.developerLoads.length
      ? this.developerLoads.reduce((sum, developer) => sum + developer.currentHours, 0) / activeDevelopers
      : 36;
    const newHoursPerDeveloper = totalNewHours / activeDevelopers;
    const methodFactor = isAi ? 0.72 : 0.95;

    return this.clamp(Math.round(currentAverage + newHoursPerDeveloper * methodFactor), this.sprintMinHours, this.sprintMaxHours + 15);
  }

  private buildWorkloads(method: 'traditional' | 'ai'): WorkloadBar[] {
    if (!this.developerLoads.length) return [];

    const plannedHours = new Map<number, number>();
    this.developerLoads.forEach((developer) => plannedHours.set(developer.id, 0));

    if (method === 'traditional') {
      const ordered = [...this.developerLoads].sort((a, b) => b.currentTasks - a.currentTasks || b.currentHours - a.currentHours);
      for (let index = 0; index < this.newTaskCount; index++) {
        const target = ordered[index < Math.ceil(this.newTaskCount * 0.65) ? 0 : index % ordered.length];
        plannedHours.set(target.id, (plannedHours.get(target.id) ?? 0) + this.hoursPerTask);
      }
    } else {
      const ordered = [...this.developerLoads].sort((a, b) => {
        const scoreA = a.currentHours + a.currentRisk * 0.6 + a.currentTasks * 4;
        const scoreB = b.currentHours + b.currentRisk * 0.6 + b.currentTasks * 4;
        return scoreA - scoreB;
      });

      for (let index = 0; index < this.newTaskCount; index++) {
        const target = ordered[index % ordered.length];
        plannedHours.set(target.id, (plannedHours.get(target.id) ?? 0) + this.hoursPerTask);
      }
    }

    return this.developerLoads.map((developer) => {
      const projectedHours = developer.currentHours + (plannedHours.get(developer.id) ?? 0);
      const percent = this.clamp(Math.round((projectedHours / this.sprintMaxHours) * 100), 0, 100);
      const statusClass = this.getWorkloadStatusClass(percent);

      return {
        id: developer.id,
        name: developer.name,
        role: developer.role,
        currentHours: developer.currentHours,
        projectedHours,
        percent,
        statusClass,
        statusLabel: this.getWorkloadStatusLabel(percent),
      };
    });
  }



  private getTopDevelopers(developers: DeveloperLoad[]): DeveloperLoad[] {
    if (developers.length <= 10) return developers;

    return [...developers]
      .sort((a, b) => b.currentHours - a.currentHours || b.currentTasks - a.currentTasks || b.currentRisk - a.currentRisk)
      .slice(0, 10);
  }

  private getCompactWorkloads(workloads: WorkloadBar[]): WorkloadBar[] {
    if (workloads.length <= 5) return workloads;

    return [...workloads]
      .sort((a, b) => b.projectedHours - a.projectedHours || b.percent - a.percent)
      .slice(0, 5);
  }

  private getBalanceScore(workloads: WorkloadBar[]): number {
    if (!workloads.length) return 0;
    const hours = workloads.map((item) => item.projectedHours);
    const average = hours.reduce((sum, value) => sum + value, 0) / hours.length;
    const variance = hours.reduce((sum, value) => sum + Math.abs(value - average), 0) / hours.length;
    return this.clamp(Math.round(100 - variance * 2.4), 0, 100);
  }

  private getWorkloadStatusClass(percent: number): 'low' | 'medium' | 'high' | 'danger' {
    if (percent >= 95) return 'danger';
    if (percent >= 78) return 'high';
    if (percent >= 55) return 'medium';
    return 'low';
  }

  private getWorkloadStatusLabel(percent: number): string {
    if (percent >= 95) return 'Quá tải';
    if (percent >= 78) return 'Cao';
    if (percent >= 55) return 'Ổn định';
    return 'Còn rảnh';
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

    const sourceTasks = [...this.getProjectTasks()]
      .sort((a, b) => (b.riskScore ?? 0) - (a.riskScore ?? 0))
      .slice(0, this.newTaskCount);

    return Array.from({ length: this.newTaskCount }, (_, index) => {
      const developer = rankedDevelopers[index % rankedDevelopers.length];
      const workloadAfter = developer.currentHours + this.hoursPerTask;
      const riskAfterAssign = this.clamp(
        Math.round(developer.currentRisk * 0.55 + (workloadAfter / this.sprintMaxHours) * 22 + this.urgentPercent * 0.12),
        12,
        65
      );
      const confidence = this.clamp(100 - riskAfterAssign + Math.round((this.sprintMaxHours - developer.currentHours) * 0.35), 62, 96);
      const sourceTask = sourceTasks[index];
      const taskName = sourceTask?.name || `Task mới ${index + 1}`;
      const isUrgent = index < Math.ceil((this.newTaskCount * this.urgentPercent) / 100);

      return {
        taskName,
        developer: developer.name,
        developerRole: developer.role,
        estimatedHours: this.hoursPerTask,
        workloadBefore: developer.currentHours,
        workloadAfter,
        riskAfterAssign,
        confidence,
        reason: isUrgent
          ? 'Task ưu tiên cao, AI chọn nhân sự còn sức chứa sprint và có rủi ro hiện tại thấp.'
          : 'AI phân bổ để cân bằng workload, tránh dồn việc vào một lập trình viên.',
        factors: [
          `Workload hiện tại: ${developer.currentHours}h/${this.sprintMaxHours}h sprint`,
          `Rủi ro hiện tại của nhân sự: ${developer.currentRisk}%`,
          `Sau khi nhận task: ${workloadAfter}h, vẫn trong ngưỡng quản lý`,
          `Độ tin cậy đề xuất: ${confidence}%`,
          `Mức khẩn cấp kịch bản: ${this.urgentPercent}%`,
        ],
      };
    });
  }

  private refreshUi(): void {
    // Đảm bảo toàn bộ dữ liệu trên trang So sánh AI render ngay sau khi state thay đổi.
    // Một số phần đang cập nhật từ subscribe/setTimeout nên nếu không ép change detection,
    // UI có thể chỉ hiện sau khi người dùng click qua lại control khác.
    this.cdr.markForCheck();
    window.setTimeout(() => {
      try {
        this.cdr.detectChanges();
      } catch {
        // Bỏ qua nếu component đã bị destroy khi người dùng chuyển trang.
      }
    }, 0);
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
