import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { Task, getTaskStatusLabel } from '../../models/task';
import { TaskService } from '../../services/task';

interface DashboardStat {
  label: string;
  value: number;
  type?: 'warning' | 'danger';
}

interface HighRiskTask {
  id: number;
  name: string;
  riskScore: number;
  deadline: string;
  assignee: string;
}

interface BottleneckSummary {
  id: number;
  name: string;
  blockedTasks: number;
  severity: 'High' | 'Medium' | 'Low';
}

@Component({
  selector: 'app-dashboard',
  imports: [BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  stats: DashboardStat[] = [
    { label: 'Tổng dự án', value: 0 },
    { label: 'Tổng task', value: 0 },
    { label: 'Task nguy cơ trễ hạn', value: 0, type: 'warning' },
    { label: 'Điểm nghẽn', value: 0, type: 'danger' },
  ];

  dashboardMessage = 'Đang tải dữ liệu dashboard từ backend API...';

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

  constructor(
    private taskService: TaskService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.dashboardMessage = 'Đang tải dữ liệu dashboard từ backend API...';

    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => {
        this.applyTasks(tasks);
        this.dashboardMessage = tasks.length
          ? `Đã tải ${tasks.length} task từ backend API.`
          : 'Backend chưa có task nào để thống kê.';
        this.hideMessageLater();
      },
      error: () => {
        this.applyTasks([]);
        this.dashboardMessage = 'Chưa tải được dữ liệu Dashboard. Kiểm tra backend có đang chạy ở port 49261 không.';
        this.cdr.detectChanges();
      },
    });
  }

  private applyTasks(tasks: Task[]): void {
    const projectKeys = tasks.map((task) => task.projectId ?? task.project).filter(Boolean);
    const projectCount = new Set(projectKeys).size;
    const lateRiskTasks = tasks.filter((task) => task.riskScore >= 60);
    const bottleneckTasks = tasks.filter((task) => task.riskLevel === 'High' || task.riskScore >= 80);

    this.stats = [
      { label: 'Tổng dự án', value: projectCount },
      { label: 'Tổng task', value: tasks.length },
      { label: 'Task nguy cơ trễ hạn', value: lateRiskTasks.length, type: 'warning' },
      { label: 'Điểm nghẽn', value: bottleneckTasks.length, type: 'danger' },
    ];

    const statusLabels = this.getStatusLabels(tasks);
    const statusCounts = this.getStatusCounts(tasks);

    this.taskStatusChart = {
      labels: statusLabels,
      datasets: [
        {
          data: statusCounts,
          backgroundColor: ['#94a3b8', '#2563eb', '#f59e0b', '#16a34a', '#dc2626', '#8b5cf6'],
        },
      ],
    };

    this.riskChart = {
      labels: ['Low', 'Medium', 'High'],
      datasets: [
        {
          label: 'Số task',
          data: [
            tasks.filter((task) => this.getRiskBucket(task) === 'Low').length,
            tasks.filter((task) => this.getRiskBucket(task) === 'Medium').length,
            tasks.filter((task) => this.getRiskBucket(task) === 'High').length,
          ],
          backgroundColor: ['#16a34a', '#f59e0b', '#dc2626'],
        },
      ],
    };

    this.highRiskTasks = [...tasks]
      .filter((task) => task.riskScore > 0)
      .sort((a, b) => b.riskScore - a.riskScore)
      .slice(0, 5)
      .map((task) => ({
        id: task.id,
        name: task.name,
        riskScore: task.riskScore,
        deadline: task.deadline,
        assignee: task.assignee || 'Chưa phân công',
      }));

    this.topBottlenecks = [...bottleneckTasks]
      .sort((a, b) => b.riskScore - a.riskScore)
      .slice(0, 5)
      .map((task) => ({
        id: task.id,
        name: task.name,
        blockedTasks: Math.max(1, Math.round(task.riskScore / 20)),
        severity: task.riskScore >= 80 ? 'High' : task.riskScore >= 60 ? 'Medium' : 'Low',
      }));

    this.cdr.detectChanges();
  }

  private getStatusLabels(tasks: Task[]): string[] {
    return [...new Set(tasks.map((task) => task.status || 'Can lam'))].map((status) => getTaskStatusLabel(status));
  }

  private getStatusCounts(tasks: Task[]): number[] {
    const statuses = [...new Set(tasks.map((task) => task.status || 'Can lam'))];
    return statuses.map((status) => tasks.filter((task) => (task.status || 'Can lam') === status).length);
  }

  private getRiskBucket(task: Task): 'Low' | 'Medium' | 'High' {
    if (task.riskLevel === 'High' || task.riskScore >= 70) return 'High';
    if (task.riskLevel === 'Medium' || task.riskScore >= 40) return 'Medium';
    return 'Low';
  }

  private hideMessageLater(): void {
    window.setTimeout(() => {
      this.dashboardMessage = '';
      this.cdr.detectChanges();
    }, 3000);
  }
}
