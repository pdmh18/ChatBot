import { Component, OnInit } from '@angular/core';
import { AssigneeSuggestion, Bottleneck, LateRisk } from '../../models/ai';
import { Task } from '../../models/task';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-ai-alerts',
  imports: [],
  templateUrl: './ai-alerts.html',
  styleUrl: './ai-alerts.scss',
})
export class AiAlerts implements OnInit {
  lateRisks: LateRisk[] = [];
  bottlenecks: Bottleneck[] = [];
  suggestions: AssigneeSuggestion[] = [];
  selectedBottleneck: Bottleneck | null = null;

  constructor(private taskService: TaskService) {}

  ngOnInit(): void {
    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => this.buildAlerts(tasks),
      error: () => this.buildAlerts([]),
    });
  }

  openBottleneckAlert(item: Bottleneck): void {
    this.selectedBottleneck = item;
  }

  closeBottleneckAlert(): void {
    this.selectedBottleneck = null;
  }

  private buildAlerts(tasks: Task[]): void {
    const riskyTasks = [...tasks].sort((a, b) => b.riskScore - a.riskScore);

    this.lateRisks = riskyTasks.slice(0, 5).map((task) => ({
      id: task.id,
      task: task.name,
      risk: task.riskScore,
      reason: `${task.riskLevel ?? 'Risk'} - Deadline ${task.deadline || 'chưa có'}`,
    }));

    this.bottlenecks = riskyTasks
      .filter((task) => task.riskLevel === 'High' || task.riskScore >= 80)
      .slice(0, 5)
      .map((task) => ({
        id: task.id,
        task: task.name,
        blockedTasks: Math.max(1, Math.round(task.riskScore / 20)),
      }));

    this.suggestions = tasks
      .filter((task) => task.assignee && task.assignee !== 'Chưa phân công')
      .slice(0, 5)
      .map((task) => ({
        id: task.id,
        task: task.name,
        developer: task.assignee,
        score: Math.max(60, 100 - task.riskScore),
      }));
  }
}
