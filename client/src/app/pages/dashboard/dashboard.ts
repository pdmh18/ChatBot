import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { Task, getTaskStatusLabel } from '../../models/task';
import { BottleneckResult } from '../../models/ai';
import { DashboardSummary, DashboardTaskAlert, DashboardWorkload, getWorkloadLabel } from '../../models/dashboard';
import { AiService } from '../../services/ai';
import { DashboardService } from '../../services/dashboard';
import { TaskService } from '../../services/task';

interface DashboardStat {
  label: string;
  value: number;
  type?: 'warning' | 'danger' | 'success';
  description?: string;
  target?: 'late-risk' | 'bottleneck' | 'unassigned' | 'completed' | 'active';
}

interface HighRiskTask {
  id: number;
  name: string;
  riskScore: number;
  deadline: string;
  assignee: string;
  reason: string;
}

interface BottleneckSummary {
  id: number;
  name: string;
  blockedTasks: number;
  severity: 'High' | 'Medium' | 'Low';
  reason: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  stats: DashboardStat[] = this.buildEmptyStats();
  dashboardMessage = 'Đang tải dữ liệu dashboard từ backend API...';
  workloadItems: DashboardWorkload[] = [];

  taskStatusChart: ChartConfiguration<'doughnut'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        backgroundColor: ['#94a3b8', '#2563eb', '#f59e0b', '#16a34a', '#dc2626'],
      },
    ],
  };

  riskChart: ChartConfiguration<'bar'>['data'] = {
    labels: ['Low', 'Medium', 'High'],
    datasets: [
      {
        label: 'Số task',
        data: [0, 0, 0],
        backgroundColor: ['#16a34a', '#f59e0b', '#dc2626'],
      },
    ],
  };

  highRiskTasks: HighRiskTask[] = [];
  topBottlenecks: BottleneckSummary[] = [];
  bottleneckSourceMessage = '';

  private selectedProjectId?: number;
  private selectedSprintId?: number;

  constructor(
    private route: ActivatedRoute,
    private taskService: TaskService,
    private aiService: AiService,
    private dashboardService: DashboardService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.selectedProjectId = this.readNumberParam('projectId');
    this.selectedSprintId = this.readNumberParam('sprintId');
    this.loadDashboard();
  }

  runResourceOptimization(): void {
    this.router.navigate(['/assignment-compare'], { queryParams: this.buildRouteParams() });
  }

  openStat(stat: DashboardStat): void {
    if (!stat.target) return;

    if (stat.target === 'late-risk') {
      this.openHighRiskDetails();
      return;
    }

    if (stat.target === 'bottleneck') {
      this.openBottleneckDetails();
      return;
    }

    if (stat.target === 'unassigned') {
      this.router.navigate(['/tasks'], { queryParams: { ...this.buildRouteParams(), assignee: 'unassigned' } });
      return;
    }

    if (stat.target === 'completed') {
      this.router.navigate(['/tasks'], { queryParams: { ...this.buildRouteParams(), status: 'Hoan thanh' } });
      return;
    }

    this.router.navigate(['/tasks'], { queryParams: { ...this.buildRouteParams(), active: true } });
  }

  openHighRiskDetails(): void {
    this.router.navigate(['/ai-alerts'], {
      queryParams: { ...this.buildRouteParams(), type: 'late-risk', focus: 'late-risk' },
    });
  }

  openBottleneckDetails(): void {
    this.router.navigate(['/ai-alerts'], {
      queryParams: { ...this.buildRouteParams(), type: 'bottleneck', focus: 'bottleneck' },
    });
  }

  getWorkloadLabel(value?: string | null): string {
    return getWorkloadLabel(value);
  }

  getWorkloadClass(item: DashboardWorkload): string {
    const level = (item.mucDoTai || '').toLowerCase();
    if (level.includes('qua') || item.phanTramTai >= 100) return 'overload';
    if (level.includes('cao') || item.phanTramTai >= 75) return 'high';
    if (level.includes('trung') || item.phanTramTai >= 45) return 'medium';
    return 'low';
  }

  private loadDashboard(): void {
    this.dashboardMessage = 'Đang tải dữ liệu dashboard từ backend API...';

    forkJoin({
      summary: this.dashboardService
        .getSummary(this.selectedProjectId, this.selectedSprintId)
        .pipe(catchError(() => of(this.getEmptySummary()))),
      workload: this.dashboardService
        .getWorkload(this.selectedProjectId, this.selectedSprintId)
        .pipe(catchError(() => of([] as DashboardWorkload[]))),
      lateAlerts: this.dashboardService
        .getAlerts('late-risk', this.selectedProjectId, this.selectedSprintId)
        .pipe(catchError(() => of([] as DashboardTaskAlert[]))),
      bottlenecks: this.aiService
        .analyzeBottlenecks(10)
        .pipe(catchError(() => of([] as BottleneckResult[]))),
      tasks: this.taskService
        .getTaskViews({ projectId: this.selectedProjectId, sprintId: this.selectedSprintId, pageNumber: 1, pageSize: 10000 })
        .pipe(catchError(() => of([] as Task[]))),
    }).subscribe(({ summary, workload, lateAlerts, bottlenecks, tasks }) => {
      this.applyDashboard(summary, workload, lateAlerts, bottlenecks, tasks);
      this.dashboardMessage = 'Dashboard đã lấy số liệu từ API backend và AI.';
      this.hideMessageLater();
    });
  }

  private applyDashboard(
    summary: DashboardSummary,
    workload: DashboardWorkload[],
    lateAlerts: DashboardTaskAlert[],
    bottleneckResults: BottleneckResult[],
    tasks: Task[]
  ): void {
    this.workloadItems = workload;

    this.stats = [
      { label: 'Tổng công việc', value: summary.tongCongViec || tasks.length, description: 'Tổng số task theo dashboard API' },
      {
        label: 'Task nguy cơ trễ hạn',
        value: this.getSummaryNumber(summary.taskNguyCoTreHan, lateAlerts.length),
        type: 'warning',
        description: 'Click để xem radar cảnh báo',
        target: 'late-risk',
      },
      {
        label: 'Điểm nghẽn',
        value: this.getSummaryNumber(summary.diemNghen, bottleneckResults.length),
        type: 'danger',
        description: 'Click để xem cảnh báo GNN',
        target: 'bottleneck',
      },
    ];

    this.applyCharts(summary, tasks);
    this.highRiskTasks = lateAlerts.slice(0, 5).map((item) => ({
      id: item.maCongViec,
      name: item.tenCongViec,
      riskScore: Math.round(item.riskPercent ?? 0),
      deadline: item.hanChot || 'Chưa có hạn chót',
      assignee: item.nguoiPhuTrach || 'Chưa phân công',
      reason: item.nguyenNhan || 'Backend chưa trả nguyên nhân chi tiết.',
    }));

    this.topBottlenecks = bottleneckResults.slice(0, 5).map((item) => ({
      id: item.maDiemNghen,
      name: item.maCongViec ? `Task #${item.maCongViec}` : item.khuVucPhatHien,
      blockedTasks: this.getBottleneckImpact(item),
      severity: this.normalizeSeverity(item.mucDoNghiemTrong),
      reason: item.nguyenNhan || item.khuVucPhatHien,
    }));

    this.bottleneckSourceMessage = bottleneckResults.length
      ? 'Dữ liệu lấy từ API AI phân tích điểm nghẽn.'
      : 'Chưa có dữ liệu GNN từ API AI.';

    this.cdr.detectChanges();
  }

  private applyCharts(summary: DashboardSummary, tasks: Task[]): void {
    const statusChart = this.getStatusChartFromSummary(summary, tasks);

    this.taskStatusChart = {
      labels: statusChart.labels,
      datasets: [
        {
          data: statusChart.data,
          backgroundColor: ['#16a34a', '#2563eb', '#94a3b8'],
        },
      ],
    };

    const riskCounts = this.getRiskCountsFromTasks(tasks);

    this.riskChart = {
      labels: ['Low', 'Medium', 'High'],
      datasets: [
        {
          label: 'Số task',
          data: riskCounts,
          backgroundColor: ['#16a34a', '#f59e0b', '#dc2626'],
        },
      ],
    };
  }

  private getBottleneckImpact(item: BottleneckResult): number {
    const value =
      item.soTaskBiAnhHuongPhiaSau ??
      item.soTaskBiAnhHuong ??
      item.soTaskDangChan ??
      item.blockedTasks ??
      this.parseBlockedTaskCount(item.nguyenNhan) ??
      0;

    return Math.max(0, Number(value) || 0);
  }

  private parseBlockedTaskCount(reason?: string | null): number | null {
    const match = reason?.match(/(\d+)\s*task/i);
    return match ? Number(match[1]) : null;
  }

  private normalizeSeverity(value: string): 'High' | 'Medium' | 'Low' {
    const severity = value?.toLowerCase() ?? '';

    if (severity.includes('cao') || severity.includes('high')) return 'High';
    if (severity.includes('trung') || severity.includes('medium')) return 'Medium';
    return 'Low';
  }

  private getStatusLabels(tasks: Task[]): string[] {
    return [...new Set(tasks.map((task) => task.status || 'Can lam'))].map((status) => getTaskStatusLabel(status));
  }

  private getStatusCounts(tasks: Task[]): number[] {
    const statuses = [...new Set(tasks.map((task) => task.status || 'Can lam'))];
    return statuses.map((status) => tasks.filter((task) => (task.status || 'Can lam') === status).length);
  }

  private getStatusChartFromSummary(summary: DashboardSummary, tasks: Task[]): { labels: string[]; data: number[] } {
    const completed = this.getSummaryNumber(summary.taskHoanThanh, this.countTasksByStatus(tasks, 'completed'));
    const active = this.getSummaryNumber(summary.taskDangLam, this.countTasksByStatus(tasks, 'active'));
    const unassigned = this.getSummaryNumber(summary.taskChuaPhanCong, this.countTasksByStatus(tasks, 'unassigned'));

    return {
      labels: ['Hoàn thành', 'Đang làm', 'Chưa phân công'],
      data: [completed, active, unassigned],
    };
  }

  private countTasksByStatus(tasks: Task[], group: 'completed' | 'active' | 'unassigned'): number {
    return tasks.filter((task) => this.getStatusGroup(task) === group).length;
  }

  private getStatusGroup(task: Task): 'completed' | 'active' | 'unassigned' {
    if (!task.assigneeId || task.assignee === 'Chưa phân công') return 'unassigned';

    const normalizedStatus = this.normalizeText(task.status);
    const progress = Number(task.progress ?? 0);

    if (normalizedStatus.includes('hoan thanh') || normalizedStatus.includes('done') || normalizedStatus.includes('completed') || progress >= 100) {
      return 'completed';
    }

    return 'active';
  }

  private normalizeText(value?: string | null): string {
    return (value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .toLowerCase()
      .trim();
  }


  private getRiskCountsFromTasks(tasks: Task[]): number[] {
    return [
      tasks.filter((task) => this.getRiskBucket(task) === 'Low').length,
      tasks.filter((task) => this.getRiskBucket(task) === 'Medium').length,
      tasks.filter((task) => this.getRiskBucket(task) === 'High').length,
    ];
  }

  private getRiskBucket(task: Task): 'Low' | 'Medium' | 'High' {
    if (task.riskLevel === 'High' || task.riskScore >= 70) return 'High';
    if (task.riskLevel === 'Medium' || task.riskScore >= 40) return 'Medium';
    return 'Low';
  }

  private getSummaryNumber(value: number | null | undefined, fallback: number): number {
    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : fallback;
  }

  private buildEmptyStats(): DashboardStat[] {
    return [
      { label: 'Tổng công việc', value: 0, description: 'Tổng số task theo dashboard API' },
      { label: 'Task nguy cơ trễ hạn', value: 0, type: 'warning', description: 'Click để xem radar cảnh báo', target: 'late-risk' },
      { label: 'Điểm nghẽn', value: 0, type: 'danger', description: 'Click để xem cảnh báo GNN', target: 'bottleneck' },
    ];
  }

  private getEmptySummary(): DashboardSummary {
    return {
      tongCongViec: 0,
      taskNguyCoTreHan: 0,
      diemNghen: 0,
      taskChuaPhanCong: 0,
      taskHoanThanh: 0,
      taskDangLam: 0,
    };
  }

  private readNumberParam(name: string): number | undefined {
    const value = Number(this.route.snapshot.queryParamMap.get(name));
    return Number.isFinite(value) && value > 0 ? value : undefined;
  }

  private buildRouteParams(): Record<string, number> {
    const params: Record<string, number> = {};
    if (this.selectedProjectId) params['projectId'] = this.selectedProjectId;
    if (this.selectedSprintId) params['sprintId'] = this.selectedSprintId;
    return params;
  }

  private hideMessageLater(): void {
    window.setTimeout(() => {
      this.dashboardMessage = '';
      this.cdr.detectChanges();
    }, 3000);
  }
}




