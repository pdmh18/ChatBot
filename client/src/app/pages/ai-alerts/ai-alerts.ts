import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AssigneeSuggestion, BottleneckResult, LateRisk } from '../../models/ai';
import { Task } from '../../models/task';
import { AiService } from '../../services/ai';
import { TaskService } from '../../services/task';

@Component({
  selector: 'app-ai-alerts',
  imports: [],
  templateUrl: './ai-alerts.html',
  styleUrl: './ai-alerts.scss',
})
export class AiAlerts implements OnInit {
  lateRisks: LateRisk[] = [];
  bottleneckResults: BottleneckResult[] = [];
  suggestions: AssigneeSuggestion[] = [];
  selectedBottleneck: BottleneckResult | null = null;
  aiMessage = 'Đang phân tích AI từ backend...';

  constructor(
    private taskService: TaskService,
    private aiService: AiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (tasks) => this.buildAlerts(tasks),
      error: () => this.buildAlerts([]),
    });

    this.loadBottlenecks();
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

  private buildAlerts(tasks: Task[]): void {
    const riskyTasks = [...tasks].sort((a, b) => b.riskScore - a.riskScore);

    this.lateRisks = riskyTasks
      .filter((task) => task.riskScore > 0)
      .slice(0, 5)
      .map((task) => ({
        id: task.id,
        task: task.name,
        risk: task.riskScore,
        reason: `${task.riskLevel ?? 'Risk'} - Deadline ${task.deadline || 'chưa có'}`,
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

    this.cdr.detectChanges();
  }

  private loadBottlenecks(): void {
    this.aiMessage = 'Đang gọi API AI phân tích điểm nghẽn...';

    this.aiService.analyzeBottlenecks(10).subscribe({
      next: (items) => {
        this.bottleneckResults = items;
        this.aiMessage = items.length ? '' : 'AI chưa phát hiện điểm nghẽn nào.';
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.bottleneckResults = [];
        this.aiMessage =
          error?.error?.message ||
          'Chưa gọi được API AI điểm nghẽn. Kiểm tra backend và AI server rồi thử lại.';
        this.cdr.detectChanges();
      },
    });
  }
}

