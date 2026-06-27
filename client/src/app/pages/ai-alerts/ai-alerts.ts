import { ChangeDetectorRef, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
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
  readonly lateRiskVisibleAlertCount = 6;
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
  private suggestionsRequestId = 0;

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
    return this.lateRisks.length > this.lateRiskVisibleAlertCount;
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
      : this.lateRisks.slice(0, this.lateRiskVisibleAlertCount);
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
    document.body.classList.add('ai-alert-modal-open');
    this.cdr.detectChanges();
  }

  closeBottleneckAlert(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.selectedBottleneck = null;
    document.body.classList.remove('ai-alert-modal-open');
    this.cdr.detectChanges();
  }

  onModalContentClick(event: Event): void {
    event.stopPropagation();
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.selectedBottleneck) {
      this.closeBottleneckAlert();
    }
  }

  getRiskPercent(item: LateRisk): number {
    return Math.round(item.risk);
  }

  getLateRiskLabel(item: LateRisk): string {
    const risk = this.getRiskPercent(item);
    if (risk >= 85) return 'Cần xử lý ngay hôm nay';
    if (risk >= 75) return 'Ưu tiên cao';
    if (risk >= 55) return 'Cần theo dõi';
    return 'Rủi ro thấp';
  }

  getBottleneckTitle(item: BottleneckResult): string {
    if (item.tenCongViec?.trim()) return item.tenCongViec.trim();
    return `Task #${item.maCongViec || 'N/A'}`;
  }

  goToAssignmentCompare(): void {
    this.router.navigate(['/assignment-compare'], { queryParams: this.buildRouteParams() });
    this.closeBottleneckAlert();
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


  countCriticalLateRisks(): number {
    return this.lateRisks.filter((item) => item.risk >= 75).length;
  }

  getHighestLateRisk(): number {
    return this.lateRisks.length ? Math.max(...this.lateRisks.map((item) => this.getRiskPercent(item))) : 0;
  }

  getTopLateRiskTaskName(): string {
    return this.lateRisks[0]?.task || 'Chưa có dữ liệu';
  }

  getMaxBottleneckImpact(): number {
    return this.bottleneckResults.length
      ? Math.max(...this.bottleneckResults.map((item) => this.getBottleneckImpact(item)))
      : 0;
  }

  getTopBottleneckScore(): string {
    return this.bottleneckResults.length ? this.formatBottleneckScore(this.bottleneckResults[0]) : '0.00';
  }

  getBestSuggestionScore(): number {
    return this.suggestions.length ? Math.max(...this.suggestions.map((item) => item.score)) : 0;
  }

  getBestSuggestionDeveloper(): string {
    return this.suggestions[0]?.developer || 'Chưa có dữ liệu';
  }

  getTaskNumber(item: BottleneckResult): string {
    return String(item.maCongViec || 'N/A');
  }

  getBottleneckShortTitle(item: BottleneckResult | null): string {
    if (!item) return 'Task';
    const title = this.getBottleneckTitle(item).replace(/^Task\s*#?\d+$/i, `Task ${this.getTaskNumber(item)}`);
    return title.length > 22 ? `${title.slice(0, 19)}...` : title;
  }

  getImpactedTasksLabel(item: BottleneckResult | null): string {
    if (!item) return '0 task';
    const impact = this.getBottleneckImpact(item);
    return `${impact} task`;
  }

  getExtraImpactedTasks(item: BottleneckResult): number {
    return Math.max(0, this.getBottleneckImpact(item) - 1);
  }

  getPrimaryImpactedLabel(item: BottleneckResult): string {
    return this.getBottleneckImpact(item) > 0 ? '1 task' : '0 task';
  }

  getRemainingImpactedLabel(item: BottleneckResult): string {
    const remaining = this.getExtraImpactedTasks(item);
    return remaining > 0 ? `+${remaining} task` : 'không thêm';
  }

  formatBottleneckScore(item: BottleneckResult): string {
    const raw = Number(item.bottleneckScore ?? 0);
    if (!Number.isFinite(raw) || raw <= 0) return 'N/A';
    return raw <= 1 ? raw.toFixed(3) : raw.toFixed(1);
  }

  formatDelayDays(item: BottleneckResult): string {
    const days = Number(item.soNgayTreDuBao ?? 0);
    if (!Number.isFinite(days) || days <= 0) return 'Chưa xác định';
    return `${Math.round(days)} ngày`;
  }

  normalizeBar(value?: number | null): number {
    const numericValue = Number(value ?? 0);
    if (!Number.isFinite(numericValue)) return 0;
    return Math.max(0, Math.min(100, Math.round(numericValue)));
  }

  normalizeExperienceBar(value?: number | null): number {
    const numericValue = Number(value ?? 0);
    if (!Number.isFinite(numericValue)) return 0;
    return Math.max(8, Math.min(100, Math.round(numericValue * 10)));
  }

  getInitials(name?: string | null): string {
    const cleanName = (name || 'AI').trim();
    const words = cleanName.split(/\s+/).filter(Boolean);
    if (!words.length) return 'AI';
    return words.slice(-2).map((word) => word.charAt(0).toUpperCase()).join('');
  }

  private loadAlerts(): void {
    this.lateRisks = [];
    this.visibleLateRisks = [];
    this.bottleneckResults = [];
    this.visibleBottlenecks = [];
    this.suggestions = [];
    this.visibleSuggestions = [];
    this.suggestionsMessage = '';
    this.isSuggestionsLoading = false;
    this.suggestionsRequestId += 1;

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
    const requestId = ++this.suggestionsRequestId;

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
    this.showAllSuggestions = false;
    this.isSuggestionsLoading = true;
    this.suggestionsMessage = `Đang tải gợi ý giao việc 0/${topTasks.length} task rủi ro cao...`;
    this.cdr.detectChanges();

    let completed = 0;
    let failed = 0;

    topTasks.forEach((task) => {
      this.aiService.suggestAssignees(task.id).subscribe({
        next: (results) => {
          if (requestId !== this.suggestionsRequestId) return;

          const best = results[0];

          if (best) {
            this.suggestions = [...this.suggestions, this.mapStaffMatchToSuggestion(best, task)].sort(
              (a, b) => b.score - a.score
            );
            this.updateVisibleSuggestions();
          }

          completed += 1;
          this.updateSuggestionsProgress(topTasks.length, completed, failed);
        },
        error: () => {
          if (requestId !== this.suggestionsRequestId) return;

          failed += 1;
          completed += 1;
          this.updateSuggestionsProgress(topTasks.length, completed, failed);
        },
      });
    });
  }

  private updateSuggestionsProgress(total: number, completed: number, failed: number): void {
    if (completed < total) {
      this.suggestionsMessage = `Đang tải gợi ý giao việc ${completed}/${total} task...`;
      this.cdr.detectChanges();
      return;
    }

    this.isSuggestionsLoading = false;

    if (this.suggestions.length) {
      this.suggestionsMessage = failed
        ? `Đã tải ${this.suggestions.length} gợi ý AI. ${failed} task chưa gọi được AI server.`
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


