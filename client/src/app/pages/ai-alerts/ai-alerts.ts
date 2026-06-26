import { ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssigneeSuggestion, BottleneckResult, LateRisk, StaffMatchResult } from '../../models/ai';
import { DashboardTaskAlert } from '../../models/dashboard';
import { AiService } from '../../services/ai';
import { DashboardService } from '../../services/dashboard';

interface ExplainableLateRisk extends LateRisk {
  explanation: string;
  pmAction: string;
  severity: 'danger' | 'warning';
}

interface SuggestionSourceTask {
  id: number;
  name: string;
  riskScore: number;
}

type AlertFilter = 'late-risk' | 'bottleneck';

@Component({
  selector: 'app-ai-alerts',
  imports: [],
  templateUrl: './ai-alerts.html',
  styleUrl: './ai-alerts.scss',
})
export class AiAlerts implements OnInit {
  @ViewChild('lateRiskSection') lateRiskSection?: ElementRef<HTMLElement>;
  @ViewChild('bottleneckSection') bottleneckSection?: ElementRef<HTMLElement>;

  readonly defaultVisibleAlertCount = 5;
  lateRisks: ExplainableLateRisk[] = [];
  visibleLateRisks: ExplainableLateRisk[] = [];
  showAllLateRisks = false;
  bottleneckResults: BottleneckResult[] = [];
  visibleBottlenecks: BottleneckResult[] = [];
  showAllBottlenecks = false;
  suggestions: AssigneeSuggestion[] = [];
  visibleSuggestions: AssigneeSuggestion[] = [];
  showAllSuggestions = false;
  suggestionsMessage = '';
  isSuggestionsLoading = false;
  selectedBottleneck: BottleneckResult | null = null;
  aiMessage = 'Đang phân tích AI từ backend...';
  focus: string | null = null;
  filter: AlertFilter | null = null;

  private selectedProjectId?: number;
  private selectedSprintId?: number;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private aiService: AiService,
    private dashboardService: DashboardService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.focus = params.get('focus');
      this.filter = this.normalizeFilter(params.get('type') || params.get('filter'));
      this.selectedProjectId = this.readNumberParam(params.get('projectId'));
      this.selectedSprintId = this.readNumberParam(params.get('sprintId'));
      this.loadAlerts();
    });
  }

  get showLateRiskSection(): boolean {
    return !this.filter || this.filter === 'late-risk';
  }

  get showBottleneckSection(): boolean {
    return !this.filter || this.filter === 'bottleneck';
  }

  get showSuggestionsSection(): boolean {
    return !this.filter;
  }

  get pageModeTitle(): string {
    if (this.filter === 'late-risk') return `Đang lọc ${this.lateRisks.length} task nguy cơ trễ hạn`;
    if (this.filter === 'bottleneck') return `Đang lọc ${this.bottleneckResults.length} điểm nghẽn GNN`;
    return 'Radar tổng hợp rủi ro dự án';
  }

  get hasHiddenLateRisks(): boolean {
    return this.lateRisks.length > this.defaultVisibleAlertCount;
  }

  get hasHiddenBottlenecks(): boolean {
    return this.bottleneckResults.length > this.defaultVisibleAlertCount;
  }

  get hasHiddenSuggestions(): boolean {
    return this.suggestions.length > this.defaultVisibleAlertCount;
  }

  toggleLateRisks(): void {
    this.showAllLateRisks = !this.showAllLateRisks;
    this.updateVisibleLateRisks();
  }

  toggleBottlenecks(): void {
    this.showAllBottlenecks = !this.showAllBottlenecks;
    this.updateVisibleBottlenecks();
  }

  toggleSuggestions(): void {
    this.showAllSuggestions = !this.showAllSuggestions;
    this.updateVisibleSuggestions();
  }

  private updateVisibleLateRisks(): void {
    this.visibleLateRisks = this.showAllLateRisks
      ? this.lateRisks
      : this.lateRisks.slice(0, this.defaultVisibleAlertCount);
  }

  private updateVisibleBottlenecks(): void {
    this.visibleBottlenecks = this.showAllBottlenecks
      ? this.bottleneckResults
      : this.bottleneckResults.slice(0, this.defaultVisibleAlertCount);
  }

  private updateVisibleSuggestions(): void {
    this.visibleSuggestions = this.showAllSuggestions
      ? this.suggestions
      : this.suggestions.slice(0, this.defaultVisibleAlertCount);
  }

  clearFilter(): void {
    this.router.navigate(['/ai-alerts'], { queryParams: this.buildRouteParams() });
  }

  openBottleneckAlert(item: BottleneckResult): void {
    this.selectedBottleneck = item;
  }

  closeBottleneckAlert(): void {
    this.selectedBottleneck = null;
  }

  getRiskPercent(item: LateRisk): number {
    return Math.round(item.risk);
  }

  getBottleneckImpact(item: BottleneckResult): number {
    const value =
      item.soTaskBiAnhHuongPhiaSau ??
      item.soTaskBiAnhHuong ??
      item.soTaskDangChan ??
      item.blockedTasks ??
      this.parseBlockedTaskCount(item.nguyenNhan) ??
      0;

    return Math.max(0, Number(value) || 0);
  }

  private loadAlerts(): void {
    this.lateRisks = [];
    this.visibleLateRisks = [];
    this.bottleneckResults = [];
    this.visibleBottlenecks = [];
    this.suggestions = [];
    this.visibleSuggestions = [];

    if (this.filter === 'late-risk') {
      this.loadLateRisks();
      return;
    }

    if (this.filter === 'bottleneck') {
      this.loadBottlenecks();
      return;
    }

    this.loadLateRisks();
    this.loadBottlenecks();
  }

  private loadLateRisks(): void {
    this.dashboardService.getAlerts('late-risk', this.selectedProjectId, this.selectedSprintId).subscribe({
      next: (items) => {
        this.lateRisks = items
          .map((item) => this.mapDashboardAlert(item))
          .sort((a, b) => b.risk - a.risk);
        this.showAllLateRisks = false;
        this.updateVisibleLateRisks();

        if (this.showSuggestionsSection) {
          this.loadSuggestionsForTopRiskyTasks(this.lateRisks.map((item) => ({ id: item.id, name: item.task, riskScore: item.risk })));
        }

        this.cdr.detectChanges();
        this.scrollToFocus();
      },
      error: () => {
        this.lateRisks = [];
        this.visibleLateRisks = [];
        this.suggestions = [];
        this.visibleSuggestions = [];
        this.isSuggestionsLoading = false;
        this.suggestionsMessage = 'Chưa gọi được API cảnh báo trễ hạn từ backend.';
        this.cdr.detectChanges();
      },
    });
  }

  private mapDashboardAlert(item: DashboardTaskAlert): ExplainableLateRisk {
    const riskPercent = Math.round(item.riskPercent ?? 0);

    return {
      id: item.maCongViec,
      task: item.tenCongViec,
      risk: riskPercent,
      reason: `Nguy cơ trễ hạn ${riskPercent}% - ${item.riskLevel || 'AI cảnh báo'}.`,
      explanation: item.nguyenNhan || 'Backend chưa trả nguyên nhân/Feature Importance cho cảnh báo này.',
      pmAction: item.khuyenNghi || 'PM cần kiểm tra nhân sự, phụ thuộc và deadline của task này.',
      severity: riskPercent >= 75 ? 'danger' : 'warning',
    };
  }

  private loadSuggestionsForTopRiskyTasks(riskyTasks: SuggestionSourceTask[]): void {
    const topTasks = [...riskyTasks].sort((a, b) => b.riskScore - a.riskScore).slice(0, 5);

    if (!topTasks.length) {
      this.suggestions = [];
      this.visibleSuggestions = [];
      this.isSuggestionsLoading = false;
      this.suggestionsMessage = 'Không có task rủi ro nào để AI gợi ý phân công.';
      this.cdr.detectChanges();
      return;
    }

    this.suggestions = [];
    this.visibleSuggestions = [];
    this.isSuggestionsLoading = true;
    this.suggestionsMessage = 'AI đang phân tích gợi ý phân công cho các task rủi ro cao...';
    this.cdr.detectChanges();

    let completed = 0;
    let failed = 0;
    const allSuggestions: AssigneeSuggestion[] = [];

    topTasks.forEach((task) => {
      this.aiService.suggestAssignees(task.id).subscribe({
        next: (results) => {
          const best = results[0];

          if (best) {
            allSuggestions.push(this.mapStaffMatchToSuggestion(best, task));
          }

          completed += 1;
          this.checkSuggestionsComplete(topTasks.length, completed, failed, allSuggestions);
        },
        error: () => {
          failed += 1;
          completed += 1;
          this.checkSuggestionsComplete(topTasks.length, completed, failed, allSuggestions);
        },
      });
    });
  }

  private checkSuggestionsComplete(
    total: number,
    completed: number,
    failed: number,
    allSuggestions: AssigneeSuggestion[]
  ): void {
    if (completed < total) return;

    this.suggestions = allSuggestions.sort((a, b) => b.score - a.score);
    this.showAllSuggestions = false;
    this.updateVisibleSuggestions();
    this.isSuggestionsLoading = false;

    if (this.suggestions.length) {
      this.suggestionsMessage = failed
        ? `Đã lấy được ${this.suggestions.length} gợi ý AI. ${failed} task chưa gọi được AI server.`
        : '';
    } else {
      this.suggestionsMessage = failed
        ? 'Không thể gọi AI server để lấy gợi ý phân công. Kiểm tra AI server port 8000 và backend port 49261.'
        : 'AI chưa tìm được gợi ý phân công phù hợp.';
    }

    this.cdr.detectChanges();
  }

  private mapStaffMatchToSuggestion(item: StaffMatchResult, task: SuggestionSourceTask): AssigneeSuggestion {
    return {
      id: item.maDeXuat || task.id * 100000 + (item.maNguoiDuocDeXuat || 0),
      taskId: task.id,
      task: task.name,
      developer: item.hoTenNguoiDuocDeXuat || 'Chưa rõ nhân sự',
      score: this.toPercent(item.diemPhuHop),
      model: item.tenMoHinh || undefined,
      reason: item.lyDo || item.giaiThich || 'API chưa trả lý do chi tiết cho gợi ý này.',
      skillScore: item.diemKyNang ?? null,
      workloadScore: item.diemKhoiLuong != null ? this.toPercent(item.diemKhoiLuong) : null,
      expScore: item.diemKinhNghiem ?? null,
    };
  }

  private parseBlockedTaskCount(reason?: string | null): number | null {
    const match = reason?.match(/(\d+)\s*task/i);
    return match ? Number(match[1]) : null;
  }

  private toPercent(value?: number | null): number {
    const numericValue = Number(value ?? 0);
    if (numericValue <= 1) return Math.round(numericValue * 100);
    return Math.round(numericValue);
  }

  private loadBottlenecks(): void {
    this.aiMessage = 'Đang gọi API AI phân tích điểm nghẽn...';

    this.aiService.analyzeBottlenecks(10).subscribe({
      next: (items) => {
        this.bottleneckResults = items.sort((a, b) => this.getBottleneckImpact(b) - this.getBottleneckImpact(a));
        this.showAllBottlenecks = false;
        this.updateVisibleBottlenecks();
        this.aiMessage = items.length ? '' : 'AI chưa phát hiện điểm nghẽn nào hoặc model GNN chưa trả dữ liệu.';
        this.cdr.detectChanges();
        this.scrollToFocus();
      },
      error: (error) => {
        this.bottleneckResults = [];
        this.visibleBottlenecks = [];
        this.aiMessage =
          error?.error?.message ||
          'Chưa gọi được API AI điểm nghẽn. Kiểm tra backend và AI server rồi thử lại.';
        this.cdr.detectChanges();
      },
    });
  }

  private scrollToFocus(): void {
    window.setTimeout(() => {
      if (this.focus === 'late-risk') this.lateRiskSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      if (this.focus === 'bottleneck') this.bottleneckSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 100);
  }

  private normalizeFilter(value: string | null): AlertFilter | null {
    return value === 'late-risk' || value === 'bottleneck' ? value : null;
  }

  private readNumberParam(value: string | null): number | undefined {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) && numericValue > 0 ? numericValue : undefined;
  }

  private buildRouteParams(): Record<string, number> {
    const params: Record<string, number> = {};
    if (this.selectedProjectId) params['projectId'] = this.selectedProjectId;
    if (this.selectedSprintId) params['sprintId'] = this.selectedSprintId;
    return params;
  }
}


