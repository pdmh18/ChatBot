import { Component } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  imports: [BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
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

  highRiskTasks = [
    {
      name: 'Thiết kế database',
      riskScore: 82,
      deadline: '2026-06-25',
      assignee: 'An',
    },
    {
      name: 'Màn hình Kanban',
      riskScore: 68,
      deadline: '2026-06-30',
      assignee: 'Oanh',
    },
  ];

  topBottlenecks = [
    {
      name: 'Database Schema',
      blockedTasks: 5,
      severity: 'High',
    },
    {
      name: 'API Authentication',
      blockedTasks: 3,
      severity: 'Medium',
    },
  ];
}