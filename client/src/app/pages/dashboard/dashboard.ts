import { Component } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

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
export class Dashboard {
  stats: DashboardStat[] = [
    { label: 'Tổng dự án', value: 4 },
    { label: 'Tổng task', value: 28 },
    { label: 'Task nguy cơ trễ hạn', value: 7, type: 'warning' },
    { label: 'Điểm nghẽn', value: 3, type: 'danger' },
  ];

  taskStatusChart: ChartConfiguration<'doughnut'>['data'] = {
    labels: ['Todo', 'In Progress', 'Review', 'Done'],
    datasets: [
      {
        data: [8, 10, 4, 6],
        backgroundColor: ['#94a3b8', '#2563eb', '#f59e0b', '#16a34a'],
      },
    ],
  };

  riskChart: ChartConfiguration<'bar'>['data'] = {
    labels: ['Low', 'Medium', 'High'],
    datasets: [
      {
        label: 'Số task',
        data: [12, 9, 7],
        backgroundColor: ['#16a34a', '#f59e0b', '#dc2626'],
      },
    ],
  };

  highRiskTasks: HighRiskTask[] = [
    {
      id: 1,
      name: 'Thiết kế database',
      riskScore: 82,
      deadline: '2026-06-25',
      assignee: 'An',
    },
    {
      id: 2,
      name: 'Màn hình Kanban',
      riskScore: 68,
      deadline: '2026-06-30',
      assignee: 'Oanh',
    },
  ];

  topBottlenecks: BottleneckSummary[] = [
    {
      id: 1,
      name: 'Database Schema',
      blockedTasks: 5,
      severity: 'High',
    },
    {
      id: 2,
      name: 'API Authentication',
      blockedTasks: 3,
      severity: 'Medium',
    },
  ];
}
